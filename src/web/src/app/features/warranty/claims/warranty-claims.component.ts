import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface ClaimRow {
  id: number;
  serialNumber: string;
  productNameSnapshot: string;
  customerNameSnapshot: string;
  claimDate: string;
  reason: string;
  status: string;
  resolutionNotes?: string;
}

const STATUS_BADGE: Record<string, string> = {
  Open:     'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-100 text-amber-700',
  Resolved: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700',
  Rejected: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-red-100 text-red-600',
  '*':      'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
};

@Component({
  selector: 'app-warranty-claims',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule, InputTextModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.warranty.claimsTitle"
        [subtitle]="labels.warranty.claimsSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800 gap-4">
          <div class="relative flex-1 max-w-xs">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm pointer-events-none"></i>
            <input pInputText [(ngModel)]="searchQuery" (ngModelChange)="onSearch($event)"
                   [placeholder]="labels.shared.search" class="!pl-9 !h-9 !rounded-lg !text-sm !w-full" />
          </div>
          @if (allRows().length > 0) {
            <span class="text-xs text-slate-400 hidden sm:block">{{ rows().length | number }} records</span>
          }
        </div>

        <p-table [value]="rows()" [loading]="loading()" [paginator]="true" [rows]="20"
                 [rowsPerPageOptions]="[10, 25, 50]" styleClass="p-datatable-sm"
                 [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th style="width: 130px">Serial #</th>
              <th>Product</th>
              <th>Customer</th>
              <th style="width: 130px">Date</th>
              <th>Reason</th>
              <th style="width: 100px">Status</th>
              <th style="width: 120px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm font-semibold text-slate-700 dark:text-slate-300">{{ row.serialNumber }}</td>
              <td class="text-slate-800 dark:text-slate-200">{{ row.productNameSnapshot }}</td>
              <td class="text-slate-600">{{ row.customerNameSnapshot }}</td>
              <td class="tabular-nums text-slate-500">{{ row.claimDate | date:'dd MMM yyyy' }}</td>
              <td class="text-slate-600 max-w-xs truncate">{{ row.reason }}</td>
              <td><span [class]="statusClass(row.status)">{{ row.status }}</span></td>
              <td class="text-right">
                @if (row.status === 'Open') {
                  <button pButton icon="pi pi-check" class="p-button-sm p-button-text p-button-rounded p-button-success"
                          pTooltip="Resolve" tooltipPosition="left"
                          (click)="openResolve(row)" [disabled]="actionId() === row.id"></button>
                }
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="7">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-inbox text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No warranty claims</p>
                  <p class="text-xs text-slate-400">Claims raised by customers will appear here.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New Claim dialog -->
    <p-dialog [(visible)]="newDialogVisible" [header]="labels.warranty.newClaim"
              [modal]="true" [style]="{ width: '480px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field label="Serial Number" [required]="true">
          <input pInputText [(ngModel)]="newForm.serialNumber" name="serialNumber"
                 class="w-full" placeholder="e.g. SN123456789" />
        </app-form-field>
        <app-form-field [label]="labels.warranty.claimReason" [required]="true">
          <input pInputText [(ngModel)]="newForm.reason" name="reason"
                 class="w-full" placeholder="Describe the issue" />
        </app-form-field>
        <app-form-field [label]="labels.common.notes">
          <input pInputText [(ngModel)]="newForm.notes" name="notes" class="w-full" />
        </app-form-field>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="newDialogVisible = false" />
        <p-button [label]="labels.warranty.newClaim" [loading]="saving()"
                  [disabled]="!newForm.serialNumber || !newForm.reason"
                  (onClick)="createClaim()" />
      </ng-template>
    </p-dialog>

    <!-- Resolve dialog -->
    <p-dialog [(visible)]="resolveDialogVisible" header="Resolve Claim"
              [modal]="true" [style]="{ width: '420px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field label="Resolution Notes" [required]="true">
          <input pInputText [(ngModel)]="resolveForm.resolutionNotes" name="notes"
                 class="w-full" placeholder="Describe resolution action taken" />
        </app-form-field>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="resolveDialogVisible = false" />
        <p-button label="Mark Resolved" severity="success" [loading]="saving()"
                  [disabled]="!resolveForm.resolutionNotes"
                  (onClick)="resolveClaim()" />
      </ng-template>
    </p-dialog>
  `
})
export class WarrantyClaimsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly actionId = signal<number | null>(null);
  protected readonly allRows  = signal<ClaimRow[]>([]);

  protected searchQuery        = '';
  protected newDialogVisible   = false;
  protected resolveDialogVisible = false;
  private selectedClaimId: number | null = null;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.serialNumber.toLowerCase().includes(q) ||
      r.productNameSnapshot.toLowerCase().includes(q) ||
      r.customerNameSnapshot.toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.warranty.newClaim, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.warranty.manageClaims },
  ];

  protected newForm     = this.emptyNew();
  protected resolveForm = { resolutionNotes: '' };

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected statusClass(status: string): string {
    return STATUS_BADGE[status] ?? STATUS_BADGE['*'];
  }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.warranty.newClaim) {
      this.newForm = this.emptyNew();
      this.newDialogVisible = true;
    }
  }

  protected openResolve(row: ClaimRow): void {
    this.selectedClaimId = row.id;
    this.resolveForm = { resolutionNotes: '' };
    this.resolveDialogVisible = true;
  }

  protected async createClaim(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.warranty.createClaim, {
        serialNumber: this.newForm.serialNumber,
        reason:       this.newForm.reason,
        notes:        this.newForm.notes || null,
      }));
      this.newDialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  protected async resolveClaim(): Promise<void> {
    if (!this.selectedClaimId) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.patch(ApiEndpoints.warranty.resolveClaim(this.selectedClaimId), {
        resolutionNotes: this.resolveForm.resolutionNotes,
        status: 'Resolved',
      }));
      this.resolveDialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<ClaimRow[]>(ApiEndpoints.warranty.claims));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyNew() {
    return { serialNumber: '', reason: '', notes: '' };
  }
}
