import {
  ChangeDetectionStrategy, Component, inject, signal, computed,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TextareaModule } from 'primeng/textarea';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface OnPremDeploymentDto {
  id: number;
  deploymentId: string;
  shopLocalEndpoint: string;
  softwareVersion: string;
  mode: string;
  status: string;
  installedAtUtc: string;
  lastReplicationAtUtc: string;
  lastFullReplicationAtUtc: string | null;
}

interface ConflictArchiveDto {
  id: number;
  deploymentId: string;
  entityName: string;
  entityId: number;
  strategy: string;
  outcome: string;
  resolutionNote: string | null;
  resolvedByUserId: number | null;
  resolvedAtUtc: string | null;
}

const STATUS_SEVERITY: Record<string, 'success' | 'danger' | 'warn' | 'secondary'> = {
  Active:      'success',
  Paused:      'secondary',
  Degraded:    'warn',
  Unreachable: 'danger',
};

const OUTCOME_SEVERITY: Record<string, 'info' | 'success' | 'warn' | 'danger'> = {
  Pending:          'warn',
  AutoResolved:     'success',
  ManuallyResolved: 'info',
  Rejected:         'danger',
};

@Component({
  selector: 'app-on-prem',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, DatePipe, FormsModule, ButtonModule, TagModule, TooltipModule,
    ToastModule, DialogModule, DropdownModule, TableModule, ConfirmDialogModule,
    TextareaModule, PageHeaderComponent,
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <p-toast />
    <p-confirmDialog />

    <div class="p-6 space-y-8 max-w-6xl mx-auto">
      <app-page-header
        title="On-Prem Deployments"
        subtitle="Manage self-hosted deployments and monitor replication health."
        icon="pi pi-server">
        <button pButton label="Refresh" icon="pi pi-refresh" severity="secondary"
          (click)="loadAll()" [loading]="loading()"></button>
      </app-page-header>

      <!-- Deployment cards -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        @for (d of deployments(); track d.id) {
          <div class="card p-4 space-y-3 cursor-pointer border-2 transition-colors"
            [class.border-primary]="selectedDeployment()?.id === d.id"
            [class.border-transparent]="selectedDeployment()?.id !== d.id"
            (click)="selectDeployment(d)">
            <div class="flex items-start justify-between">
              <div>
                <div class="font-semibold text-sm">{{ d.deploymentId }}</div>
                <div class="text-xs text-surface-500">{{ d.shopLocalEndpoint }}</div>
              </div>
              <p-tag [value]="d.status"
                [severity]="statusSeverity(d.status)" />
            </div>
            <div class="flex gap-4 text-xs text-surface-500">
              <span><i class="pi pi-code-branch mr-1"></i>{{ d.softwareVersion }}</span>
              <span><i class="pi pi-arrow-right-arrow-left mr-1"></i>{{ d.mode }}</span>
            </div>
            <div class="text-xs text-surface-400">
              Last sync: {{ d.lastReplicationAtUtc | date:'short' }}
            </div>
            <div class="flex gap-2">
              @if (d.status === 'Active') {
                <button pButton label="Pause" icon="pi pi-pause" severity="secondary" size="small"
                  (click)="$event.stopPropagation(); updateStatus(d, 'Paused')"></button>
              }
              @if (d.status === 'Paused') {
                <button pButton label="Resume" icon="pi pi-play" severity="success" size="small"
                  (click)="$event.stopPropagation(); updateStatus(d, 'Active')"></button>
              }
              <button pButton label="View Logs" icon="pi pi-list" severity="info" size="small"
                (click)="$event.stopPropagation(); openLogs(d)"></button>
            </div>
          </div>
        }
        @if (deployments().length === 0 && !loading()) {
          <div class="col-span-2 text-center text-surface-400 py-12">
            <i class="pi pi-server text-4xl block mb-2"></i>
            No on-prem deployments registered yet.
          </div>
        }
      </div>

      <!-- Conflict queue -->
      @if (pendingConflicts().length > 0) {
        <section>
          <h3 class="font-semibold text-lg mb-3">
            <i class="pi pi-exclamation-triangle text-yellow-500 mr-2"></i>
            Pending Conflicts ({{ pendingConflicts().length }})
          </h3>
          <p-table [value]="pendingConflicts()" styleClass="p-datatable-sm">
            <ng-template pTemplate="header">
              <tr>
                <th>Deployment</th>
                <th>Entity</th>
                <th>Strategy</th>
                <th>Created</th>
                <th></th>
              </tr>
            </ng-template>
            <ng-template pTemplate="body" let-c>
              <tr>
                <td class="text-xs font-mono">{{ c.deploymentId }}</td>
                <td>{{ c.entityName }} #{{ c.entityId }}</td>
                <td>{{ c.strategy }}</td>
                <td class="text-xs text-surface-500">{{ c.createdAtUtc | date:'short' }}</td>
                <td>
                  <button pButton label="Resolve" icon="pi pi-check" severity="warn" size="small"
                    (click)="openResolveDialog(c)"></button>
                </td>
              </tr>
            </ng-template>
          </p-table>
        </section>
      }
    </div>

    <!-- Replication logs dialog -->
    <p-dialog [(visible)]="showLogsDialog" header="Replication Logs"
      [modal]="true" [style]="{width: '60rem'}" [closable]="true">
      <p-table [value]="logs()" styleClass="p-datatable-sm p-datatable-striped">
        <ng-template pTemplate="header">
          <tr>
            <th>Direction</th>
            <th>Status</th>
            <th>Rows ✓/⚡/✗</th>
            <th>Bytes</th>
            <th>Started</th>
            <th>Duration</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-log>
          <tr>
            <td>{{ log.direction }}</td>
            <td><p-tag [value]="log.status" [severity]="logStatusSeverity(log.status)" /></td>
            <td class="font-mono text-xs">
              {{ log.rowsTransferred }}/{{ log.rowsConflicted }}/{{ log.rowsFailed }}
            </td>
            <td class="text-xs">{{ (log.payloadBytes / 1024).toFixed(1) }} KB</td>
            <td class="text-xs">{{ log.startedAtUtc | date:'short' }}</td>
            <td class="text-xs">
              @if (log.completedAtUtc) {
                {{ durationMs(log.startedAtUtc, log.completedAtUtc) | number }}ms
              } @else {
                Running…
              }
            </td>
          </tr>
        </ng-template>
      </p-table>
    </p-dialog>

    <!-- Resolve conflict dialog -->
    <p-dialog [(visible)]="showResolveDialog" header="Resolve Conflict"
      [modal]="true" [style]="{width: '36rem'}">
      @if (resolvingConflict()) {
        <div class="space-y-4">
          <div class="text-sm">
            <strong>Entity:</strong> {{ resolvingConflict()!.entityName }} #{{ resolvingConflict()!.entityId }}<br>
            <strong>Strategy:</strong> {{ resolvingConflict()!.strategy }}
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">Outcome</label>
            <p-dropdown [(ngModel)]="resolveOutcome"
              [options]="[{label:'Manually Resolved',value:'ManuallyResolved'},{label:'Rejected',value:'Rejected'}]"
              optionLabel="label" optionValue="value" placeholder="Select outcome" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">Notes</label>
            <textarea pTextarea [(ngModel)]="resolveNote" rows="3" class="w-full"
              placeholder="Explain the resolution..."></textarea>
          </div>
          <div class="flex justify-end gap-2">
            <button pButton label="Cancel" severity="secondary" (click)="showResolveDialog = false"></button>
            <button pButton label="Save" icon="pi pi-check"
              [disabled]="!resolveOutcome" (click)="submitResolve()"></button>
          </div>
        </div>
      }
    </p-dialog>
  `,
})
export class OnPremComponent {
  private http = inject(HttpClient);
  private toast = inject(MessageService);

  readonly loading = signal(false);
  readonly deployments = signal<OnPremDeploymentDto[]>([]);
  readonly selectedDeployment = signal<OnPremDeploymentDto | null>(null);
  readonly conflicts = signal<ConflictArchiveDto[]>([]);
  readonly logs = signal<any[]>([]);

  readonly pendingConflicts = computed(() =>
    this.conflicts().filter(c => c.outcome === 'Pending'));

  showLogsDialog = false;
  showResolveDialog = false;
  resolvingConflict = signal<ConflictArchiveDto | null>(null);
  resolveOutcome = '';
  resolveNote = '';

  constructor() { this.loadAll(); }

  async loadAll(): Promise<void> {
    this.loading.set(true);
    try {
      const [deps, conflictRes] = await Promise.all([
        firstValueFrom(this.http.get<OnPremDeploymentDto[]>(ApiEndpoints.onPrem.deployments)),
        firstValueFrom(this.http.get<{ items: ConflictArchiveDto[] }>(
          ApiEndpoints.onPrem.conflicts + '?outcome=Pending&pageSize=50')),
      ]);
      this.deployments.set(deps);
      this.conflicts.set(conflictRes.items);
    } catch {
      this.toast.add({ severity: 'error', summary: 'Load failed' });
    } finally {
      this.loading.set(false);
    }
  }

  selectDeployment(d: OnPremDeploymentDto): void {
    this.selectedDeployment.set(d);
  }

  async updateStatus(d: OnPremDeploymentDto, status: string): Promise<void> {
    try {
      await firstValueFrom(this.http.patch(ApiEndpoints.onPrem.updateStatus(d.id), { status }));
      this.deployments.update(list =>
        list.map(dep => dep.id === d.id ? { ...dep, status } : dep));
      this.toast.add({ severity: 'success', summary: `Deployment ${status}` });
    } catch {
      this.toast.add({ severity: 'error', summary: 'Update failed' });
    }
  }

  async openLogs(d: OnPremDeploymentDto): Promise<void> {
    try {
      const res = await firstValueFrom(
        this.http.get<{ items: any[] }>(ApiEndpoints.onPrem.logs(d.id)));
      this.logs.set(res.items);
      this.showLogsDialog = true;
    } catch {
      this.toast.add({ severity: 'error', summary: 'Could not load logs' });
    }
  }

  openResolveDialog(c: ConflictArchiveDto): void {
    this.resolvingConflict.set(c);
    this.resolveOutcome = '';
    this.resolveNote = '';
    this.showResolveDialog = true;
  }

  async submitResolve(): Promise<void> {
    const c = this.resolvingConflict();
    if (!c || !this.resolveOutcome) return;
    try {
      await firstValueFrom(this.http.post(
        ApiEndpoints.onPrem.resolveConflict(c.id),
        { outcome: this.resolveOutcome, resolutionNote: this.resolveNote || null }));
      this.conflicts.update(list => list.filter(x => x.id !== c.id));
      this.showResolveDialog = false;
      this.toast.add({ severity: 'success', summary: 'Conflict resolved' });
    } catch {
      this.toast.add({ severity: 'error', summary: 'Resolve failed' });
    }
  }

  statusSeverity(s: string) { return STATUS_SEVERITY[s] ?? 'secondary'; }
  logStatusSeverity(s: string) {
    const m: Record<string, 'success' | 'danger' | 'warn' | 'info'> = {
      Success: 'success', Failed: 'danger', PartialFailure: 'warn', Running: 'info',
    };
    return m[s] ?? 'info';
  }
  durationMs(start: string, end: string): number {
    return Math.round((new Date(end).getTime() - new Date(start).getTime()));
  }
}
