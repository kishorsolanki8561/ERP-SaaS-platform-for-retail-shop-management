import {
  ChangeDetectionStrategy, Component,
  inject, signal, viewChild
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn, RowAction } from '../../../shared/components/data-table/data-table.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface RegistrationRow extends Record<string, unknown> {
  id: number;
  shopCode: string;
  legalName: string;
  adminEmail: string;
  contactPhone: string | null;
  status: string;
  submittedAtUtc: string;
  reviewedAtUtc: string | null;
  rejectionReason: string | null;
}

@Component({
  selector: 'app-platform-registrations',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, DialogModule, ButtonModule, DropdownModule,
    PageHeaderComponent, DataTableComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        title="Shop Registration Requests"
        subtitle="Review and approve or reject self-service shop registration applications."
        [actions]="[]"
      />

      <!-- Status filter -->
      <div class="flex items-center gap-3">
        <label class="text-sm font-medium text-slate-600 dark:text-slate-400">Status:</label>
        <p-dropdown
          [options]="statusOptions"
          [(ngModel)]="selectedStatus"
          (onChange)="applyFilter()"
          optionLabel="label"
          optionValue="value"
          styleClass="w-44 text-sm"
        />
      </div>

      <app-data-table
        #table
        [columns]="columns"
        [apiUrl]="apiUrl()"
        [rowActions]="rowActions"
        [searchable]="false"
        (rowAction)="onRowAction($event)"
      />
    </div>

    <!-- Approve confirmation dialog -->
    <p-dialog
      [(visible)]="showApprove"
      header="Approve Registration"
      [modal]="true"
      [style]="{ width: '420px' }"
      [closable]="!actionLoading()"
    >
      <p class="text-sm text-slate-600 dark:text-slate-400 mb-1">
        Approve <strong>{{ selected()?.legalName }}</strong>
        (<span class="font-mono text-xs">{{ selected()?.shopCode }}</span>)?
      </p>
      <p class="text-sm text-slate-500 dark:text-slate-500 mt-2">
        This will create the shop and activate the admin account at
        <strong>{{ selected()?.adminEmail }}</strong>.
      </p>
      <ng-template pTemplate="footer">
        <button pButton label="Cancel" [text]="true" severity="secondary"
                (click)="showApprove = false" [disabled]="actionLoading()"></button>
        <button pButton label="Approve" severity="success" icon="pi pi-check"
                (click)="confirmApprove()" [loading]="actionLoading()"></button>
      </ng-template>
    </p-dialog>

    <!-- Reject dialog -->
    <p-dialog
      [(visible)]="showReject"
      header="Reject Registration"
      [modal]="true"
      [style]="{ width: '460px' }"
      [closable]="!actionLoading()"
    >
      <p class="text-sm text-slate-600 dark:text-slate-400 mb-4">
        Reject <strong>{{ selected()?.legalName }}</strong>? Please provide a reason.
      </p>
      <textarea
        [(ngModel)]="rejectReason"
        rows="3"
        placeholder="e.g. Duplicate submission, incomplete details…"
        class="w-full px-4 py-3 rounded-xl text-sm outline-none resize-none
               text-slate-900 dark:text-white placeholder:text-slate-400
               border border-slate-300 dark:border-white/15
               bg-white dark:bg-white/5 focus:border-indigo-400"
      ></textarea>
      @if (actionError()) {
        <p class="text-xs text-red-400 mt-2">{{ actionError() }}</p>
      }
      <ng-template pTemplate="footer">
        <button pButton label="Cancel" [text]="true" severity="secondary"
                (click)="showReject = false" [disabled]="actionLoading()"></button>
        <button pButton label="Reject" severity="danger" icon="pi pi-times"
                (click)="confirmReject()" [loading]="actionLoading()"></button>
      </ng-template>
    </p-dialog>
  `,
})
export class PlatformRegistrationsComponent {
  private readonly http = inject(HttpClient);

  private readonly tableRef = viewChild<DataTableComponent<RegistrationRow>>('table');

  protected readonly selected      = signal<RegistrationRow | null>(null);
  protected readonly actionLoading = signal(false);
  protected readonly actionError   = signal<string | null>(null);

  protected showApprove  = false;
  protected showReject   = false;
  protected rejectReason = '';

  protected selectedStatus: string | null = null;

  protected readonly statusOptions = [
    { label: 'All',      value: null       },
    { label: 'Pending',  value: 'Pending'  },
    { label: 'Approved', value: 'Approved' },
    { label: 'Rejected', value: 'Rejected' },
  ];

  protected readonly apiUrl = signal<string>(ApiEndpoints.platformRegistrations.list);

  protected readonly columns: TableColumn[] = [
    { field: 'shopCode',       header: 'Shop Code',  sortable: true  },
    { field: 'legalName',      header: 'Legal Name', sortable: true  },
    { field: 'adminEmail',     header: 'Email',      sortable: false },
    { field: 'submittedAtUtc', header: 'Submitted',  sortable: true, type: 'date' },
    { field: 'status',         header: 'Status',     sortable: true, type: 'status' },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: 'Approve', icon: 'pi pi-check', severity: 'success' },
    { label: 'Reject',  icon: 'pi pi-times', severity: 'danger'  },
  ];

  protected applyFilter(): void {
    const base = ApiEndpoints.platformRegistrations.list;
    this.apiUrl.set(this.selectedStatus ? `${base}?status=${this.selectedStatus}` : base);
  }

  protected onRowAction(event: { action: string; row: RegistrationRow }): void {
    this.selected.set(event.row);
    this.actionError.set(null);
    if (event.action === 'Approve') {
      this.showApprove = true;
    } else if (event.action === 'Reject') {
      this.rejectReason = '';
      this.showReject = true;
    }
  }

  protected async confirmApprove(): Promise<void> {
    const row = this.selected();
    if (!row) return;
    this.actionLoading.set(true);
    this.actionError.set(null);
    try {
      await firstValueFrom(
        this.http.post(ApiEndpoints.platformRegistrations.approve(row.id), {}),
      );
      this.showApprove = false;
      this.tableRef()?.reload();
    } catch (err: unknown) {
      this.actionError.set(
        (err as { error?: { errors?: string[] } })?.error?.errors?.[0] ?? 'An unexpected error occurred.',
      );
    } finally {
      this.actionLoading.set(false);
    }
  }

  protected async confirmReject(): Promise<void> {
    const row = this.selected();
    if (!row) return;
    if (!this.rejectReason.trim()) {
      this.actionError.set('Rejection reason is required.');
      return;
    }
    this.actionLoading.set(true);
    this.actionError.set(null);
    try {
      await firstValueFrom(
        this.http.post(ApiEndpoints.platformRegistrations.reject(row.id), { reason: this.rejectReason }),
      );
      this.showReject = false;
      this.tableRef()?.reload();
    } catch (err: unknown) {
      this.actionError.set(
        (err as { error?: { errors?: string[] } })?.error?.errors?.[0] ?? 'An unexpected error occurred.',
      );
    } finally {
      this.actionLoading.set(false);
    }
  }
}
