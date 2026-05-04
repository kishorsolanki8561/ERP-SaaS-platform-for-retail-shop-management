import {
  ChangeDetectionStrategy, Component,
  inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SidebarModule } from 'primeng/sidebar';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { DropdownModule } from 'primeng/dropdown';
import { TextareaModule } from 'primeng/textarea';
import { DialogModule } from 'primeng/dialog';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn, RowAction } from '../../../shared/components/data-table/data-table.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface LeadSummaryDto {
  id: number;
  name: string;
  email: string;
  phone: string;
  businessName?: string;
  verticalCode: string;
  source: string;
  status: string;
  assignedUserId?: number;
  assignedUserName?: string;
  lastContactedAtUtc?: string;
  createdAtUtc: string;
}

interface LeadDetailDto extends LeadSummaryDto {
  message?: string;
  notes?: string;
  cityCode: string;
  stateCode: string;
  shopsCount?: number;
  utmSource?: string;
  utmCampaign?: string;
  convertedShopId?: number;
}

const STATUS_COLORS: Record<string, string> = {
  New:       'bg-sky-100 text-sky-700',
  Contacted: 'bg-blue-100 text-blue-700',
  Qualified: 'bg-violet-100 text-violet-700',
  Converted: 'bg-emerald-100 text-emerald-700',
  Lost:      'bg-red-100 text-red-600',
};

@Component({
  selector: 'app-platform-leads',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    SidebarModule, ButtonModule, BadgeModule, DropdownModule, TextareaModule, DialogModule,
    PageHeaderComponent, DataTableComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        title="Leads"
        subtitle="Inbound leads from website, referrals, ads and events."
        [actions]="[]"
      />

      <app-data-table
        [columns]="columns"
        [apiUrl]="apiUrl"
        [rowActions]="rowActions"
        [searchable]="true"
        (rowAction)="onRowAction($event)"
      />
    </div>

    <!-- Lead detail sidebar -->
    <p-sidebar
      [(visible)]="detailVisible"
      position="right"
      [style]="{ width: '520px' }"
      [closeOnEscape]="true"
    >
      <ng-template pTemplate="header">
        <div class="flex items-center gap-2">
          <i class="pi pi-user text-indigo-500 text-lg"></i>
          <span class="font-semibold text-slate-800 dark:text-slate-100">
            {{ selectedLead()?.name ?? 'Lead Detail' }}
          </span>
        </div>
      </ng-template>

      @if (detailLoading()) {
        <div class="flex items-center justify-center py-16">
          <i class="pi pi-spin pi-spinner text-2xl text-slate-400"></i>
        </div>
      } @else if (selectedLead()) {
        <!-- Status badge -->
        <div class="mb-4">
          <span [class]="'inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold ' + statusClass(selectedLead()!.status)">
            {{ selectedLead()!.status }}
          </span>
        </div>

        <!-- Details rows -->
        <div class="space-y-2 text-sm mb-5">
          @for (field of detailFields(); track field.label) {
            <div class="flex justify-between border-b border-slate-100 dark:border-slate-800 pb-2">
              <span class="text-slate-400">{{ field.label }}</span>
              <span class="font-medium text-slate-700 dark:text-slate-300 text-right max-w-[220px] break-words">{{ field.value }}</span>
            </div>
          }
        </div>

        <!-- Message / notes -->
        @if (selectedLead()!.message) {
          <div class="mb-4">
            <p class="text-xs font-medium text-slate-500 mb-1">Message</p>
            <p class="text-sm text-slate-700 dark:text-slate-300 bg-slate-50 dark:bg-slate-800 rounded-lg p-3">
              {{ selectedLead()!.message }}
            </p>
          </div>
        }
        @if (selectedLead()!.notes) {
          <div class="mb-4">
            <p class="text-xs font-medium text-slate-500 mb-1">Notes</p>
            <p class="text-sm text-slate-700 dark:text-slate-300 bg-slate-50 dark:bg-slate-800 rounded-lg p-3">
              {{ selectedLead()!.notes }}
            </p>
          </div>
        }

        <!-- UTM -->
        @if (selectedLead()!.utmSource || selectedLead()!.utmCampaign) {
          <div class="mb-4 bg-slate-50 dark:bg-slate-800 rounded-lg p-3 text-xs">
            <p class="font-medium text-slate-500 mb-1">UTM Parameters</p>
            @if (selectedLead()!.utmSource) {
              <p class="text-slate-600">Source: {{ selectedLead()!.utmSource }}</p>
            }
            @if (selectedLead()!.utmCampaign) {
              <p class="text-slate-600">Campaign: {{ selectedLead()!.utmCampaign }}</p>
            }
          </div>
        }

        <!-- Actions -->
        <div class="flex flex-col gap-2 mt-4">
          <p-button
            label="Update Status"
            icon="pi pi-refresh"
            severity="secondary"
            [outlined]="true"
            styleClass="w-full"
            (onClick)="openStatusDialog()"
          />
          <p-button
            label="Convert to Shop"
            icon="pi pi-building"
            severity="success"
            [outlined]="true"
            styleClass="w-full"
            [disabled]="selectedLead()!.status === 'Converted'"
            (onClick)="convertLead(selectedLead()!.id)"
          />
        </div>
      }
    </p-sidebar>

    <!-- Status update dialog -->
    <p-dialog
      header="Update Lead Status"
      [(visible)]="statusDialogVisible"
      [modal]="true"
      [style]="{ width: '400px' }"
    >
      <div class="space-y-4 pt-2">
        <div>
          <label class="block text-sm font-medium text-slate-600 mb-1">New Status</label>
          <p-dropdown
            [options]="statusOptions"
            [(ngModel)]="newStatus"
            placeholder="Select status"
            styleClass="w-full"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-600 mb-1">Notes (optional)</label>
          <textarea
            pTextarea
            [(ngModel)]="statusNote"
            rows="3"
            class="w-full"
            placeholder="Add a note about this update..."
          ></textarea>
        </div>
      </div>
      <ng-template pTemplate="footer">
        <p-button
          label="Cancel"
          severity="secondary"
          [outlined]="true"
          (onClick)="statusDialogVisible = false"
        />
        <p-button
          label="Update"
          [disabled]="!newStatus"
          (onClick)="submitStatusUpdate()"
        />
      </ng-template>
    </p-dialog>
  `
})
export class PlatformLeadsComponent {
  private readonly http = inject(HttpClient);

  protected readonly apiUrl = ApiEndpoints.leads.list;
  protected detailVisible = false;
  protected statusDialogVisible = false;
  protected newStatus = '';
  protected statusNote = '';

  protected readonly detailLoading = signal(false);
  protected readonly selectedLead  = signal<LeadDetailDto | null>(null);

  protected readonly statusOptions = [
    'New', 'Contacted', 'Qualified', 'Converted', 'Lost',
  ].map(s => ({ label: s, value: s }));

  protected readonly columns: TableColumn[] = [
    { field: 'name',        header: 'Name',     sortable: true },
    { field: 'email',       header: 'Email',    width: '200px' },
    { field: 'phone',       header: 'Phone',    width: '130px' },
    { field: 'verticalCode',header: 'Vertical', width: '120px' },
    { field: 'source',      header: 'Source',   width: '100px' },
    { field: 'status',      header: 'Status',   width: '110px' },
    { field: 'createdAtUtc',header: 'Received', width: '130px', type: 'date', sortable: true },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: 'View Details', icon: 'pi pi-eye',     severity: 'secondary' },
    { label: 'Convert',      icon: 'pi pi-building', severity: 'success' },
  ];

  protected onRowAction(event: { action: string; row: Record<string, unknown> }): void {
    const id = event.row['id'] as number;
    if (event.action === 'View Details') {
      this.loadDetail(id);
    } else if (event.action === 'Convert') {
      this.convertLead(id);
    }
  }

  protected statusClass(status: string): string {
    return STATUS_COLORS[status] ?? 'bg-slate-100 text-slate-600';
  }

  protected detailFields(): { label: string; value: string }[] {
    const l = this.selectedLead();
    if (!l) return [];
    return [
      { label: 'Email',       value: l.email },
      { label: 'Phone',       value: l.phone },
      { label: 'Business',    value: l.businessName ?? '—' },
      { label: 'Vertical',    value: l.verticalCode },
      { label: 'Location',    value: `${l.cityCode}, ${l.stateCode}` },
      { label: 'Source',      value: l.source },
      { label: 'Shops Count', value: l.shopsCount?.toString() ?? '—' },
      { label: 'Assigned To', value: l.assignedUserName ?? 'Unassigned' },
    ].filter(f => !!f.value && f.value !== '—' || true);
  }

  protected openStatusDialog(): void {
    this.newStatus = this.selectedLead()?.status ?? '';
    this.statusNote = '';
    this.statusDialogVisible = true;
  }

  protected async submitStatusUpdate(): Promise<void> {
    const lead = this.selectedLead();
    if (!lead || !this.newStatus) return;
    try {
      await firstValueFrom(
        this.http.patch(ApiEndpoints.leads.lead(lead.id), {
          status: this.newStatus,
          notes: this.statusNote || null,
        })
      );
      this.statusDialogVisible = false;
      this.loadDetail(lead.id);
    } catch { /* handled by interceptor */ }
  }

  protected async convertLead(id: number): Promise<void> {
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.leads.convert(id), {}));
      if (this.selectedLead()?.id === id) {
        this.loadDetail(id);
      }
    } catch { /* handled by interceptor */ }
  }

  private async loadDetail(id: number): Promise<void> {
    this.selectedLead.set(null);
    this.detailVisible = true;
    this.detailLoading.set(true);
    try {
      const detail = await firstValueFrom(
        this.http.get<LeadDetailDto>(ApiEndpoints.leads.lead(id))
      );
      this.selectedLead.set(detail);
    } catch { /* handled by interceptor */ }
    finally { this.detailLoading.set(false); }
  }
}
