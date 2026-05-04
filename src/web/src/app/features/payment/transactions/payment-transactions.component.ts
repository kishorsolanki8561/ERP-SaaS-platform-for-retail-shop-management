import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { CalendarModule } from 'primeng/calendar';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface PaymentTransactionDto {
  id: number;
  gatewayCode: string;
  gatewayTxnId: string;
  ourReferenceNumber: string;
  purpose: string;
  amount: number;
  currency: string;
  status: string;
  paymentMethod: string | null;
  failureCode: string | null;
  failureMessage: string | null;
  initiatedAtUtc: string;
  completedAtUtc: string | null;
  paymentUrl: string | null;
  refundGatewayTxnId: string | null;
}

interface PagedResult {
  items: PaymentTransactionDto[];
  totalCount: number;
}

const STATUS_OPTIONS = [
  { label: 'All statuses', value: null },
  { label: 'Initiated',          value: 'Initiated' },
  { label: 'Pending',            value: 'Pending' },
  { label: 'Success',            value: 'Success' },
  { label: 'Failed',             value: 'Failed' },
  { label: 'Cancelled',          value: 'Cancelled' },
  { label: 'Refunded',           value: 'Refunded' },
  { label: 'Partially Refunded', value: 'PartiallyRefunded' },
];

const GATEWAY_OPTIONS = [
  { label: 'All gateways', value: null },
  { label: 'Simulated', value: 'Simulated' },
  { label: 'Razorpay',  value: 'Razorpay' },
  { label: 'Stripe',    value: 'Stripe' },
  { label: 'PhonePe',   value: 'PhonePe' },
  { label: 'Paytm',     value: 'Paytm' },
];

@Component({
  selector: 'app-payment-transactions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, DatePipe, FormsModule, ButtonModule, TagModule, DialogModule,
    DropdownModule, CalendarModule, InputNumberModule, TextareaModule,
    PaginatorModule, ToastModule, ConfirmDialogModule, PageHeaderComponent,
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <p-toast />
    <p-confirmDialog />

    <div class="p-6 space-y-5 max-w-7xl mx-auto">
      <app-page-header
        title="Payment Transactions"
        subtitle="Track all payment attempts, confirmations, and refunds."
        [actions]="[]"
        (actionClick)="void(0)"
      />

      <!-- Filters -->
      <div class="flex flex-wrap gap-3">
        <p-dropdown
          [options]="STATUS_OPTIONS"
          [(ngModel)]="filterStatus"
          optionLabel="label"
          optionValue="value"
          placeholder="Status"
          styleClass="w-44"
          (onChange)="applyFilter()" />
        <p-dropdown
          [options]="GATEWAY_OPTIONS"
          [(ngModel)]="filterGateway"
          optionLabel="label"
          optionValue="value"
          placeholder="Gateway"
          styleClass="w-36"
          (onChange)="applyFilter()" />
        <p-calendar
          [(ngModel)]="filterFrom"
          placeholder="From date"
          dateFormat="dd/mm/yy"
          styleClass="w-36"
          (onSelect)="applyFilter()" />
        <p-calendar
          [(ngModel)]="filterTo"
          placeholder="To date"
          dateFormat="dd/mm/yy"
          styleClass="w-36"
          (onSelect)="applyFilter()" />
        <button pButton label="Reset" icon="pi pi-times" severity="secondary"
          [outlined]="true" size="small" (click)="resetFilter()"></button>
      </div>

      @if (loading()) {
        <div class="flex justify-center py-20">
          <i class="pi pi-spinner pi-spin text-3xl text-slate-400"></i>
        </div>
      } @else if (items().length === 0) {
        <div class="flex flex-col items-center justify-center py-20 text-slate-400 space-y-3">
          <i class="pi pi-credit-card text-5xl"></i>
          <p class="text-lg font-medium">No transactions found</p>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-700">
                  <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Reference</th>
                  <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Gateway</th>
                  <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Purpose</th>
                  <th class="px-4 py-3 text-right font-semibold text-slate-600 dark:text-slate-400">Amount</th>
                  <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Status</th>
                  <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Date</th>
                  <th class="px-4 py-3"></th>
                </tr>
              </thead>
              <tbody>
                @for (txn of items(); track txn.id) {
                  <tr class="border-b border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                    <td class="px-4 py-3">
                      <div class="font-medium text-slate-800 dark:text-slate-200">{{ txn.ourReferenceNumber }}</div>
                      <div class="text-xs font-mono text-slate-400 truncate max-w-[140px]">{{ txn.gatewayTxnId }}</div>
                    </td>
                    <td class="px-4 py-3 text-slate-700 dark:text-slate-300">{{ txn.gatewayCode }}</td>
                    <td class="px-4 py-3 text-slate-600 dark:text-slate-400">{{ txn.purpose }}</td>
                    <td class="px-4 py-3 text-right font-semibold text-slate-800 dark:text-slate-200">
                      ₹{{ txn.amount | number:'1.2-2' }}
                    </td>
                    <td class="px-4 py-3">
                      <p-tag [value]="txn.status" [severity]="statusSeverity(txn.status)" />
                    </td>
                    <td class="px-4 py-3 text-slate-500 whitespace-nowrap">
                      {{ txn.initiatedAtUtc | date:'dd MMM, HH:mm' }}
                    </td>
                    <td class="px-4 py-3">
                      @if (txn.status === 'Success') {
                        <button pButton label="Refund" icon="pi pi-replay"
                          severity="warn" [outlined]="true" size="small"
                          (click)="openRefund(txn)"></button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          @if (totalCount() > pageSize) {
            <div class="px-4 py-3 border-t border-slate-200 dark:border-slate-700">
              <p-paginator
                [rows]="pageSize"
                [totalRecords]="totalCount()"
                [first]="(page() - 1) * pageSize"
                (onPageChange)="onPage($event)" />
            </div>
          }
        </div>

        <p class="text-sm text-slate-500">
          Showing {{ items().length }} of <strong>{{ totalCount() }}</strong> transactions
        </p>
      }
    </div>

    <!-- Refund dialog -->
    <p-dialog
      [(visible)]="refundVisible"
      header="Issue Refund"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      styleClass="w-full max-w-sm"
    >
      @if (refundTxn()) {
        <div class="space-y-4 py-2">
          <p class="text-sm text-slate-600 dark:text-slate-400">
            Refunding payment <strong>{{ refundTxn()!.ourReferenceNumber }}</strong>
            (Max: <strong>₹{{ refundTxn()!.amount | number:'1.2-2' }}</strong>)
          </p>
          <div class="space-y-1.5">
            <label class="text-sm font-medium text-slate-700 dark:text-slate-300">Refund amount (₹)</label>
            <p-inputNumber
              [(ngModel)]="refundAmount"
              [min]="0.01"
              [max]="refundTxn()!.amount"
              mode="currency"
              currency="INR"
              locale="en-IN"
              styleClass="w-full" />
          </div>
          <div class="space-y-1.5">
            <label class="text-sm font-medium text-slate-700 dark:text-slate-300">Reason</label>
            <textarea pTextarea [(ngModel)]="refundReason"
              rows="2" class="w-full" placeholder="Customer request..."></textarea>
          </div>
        </div>
      }

      <ng-template pTemplate="footer">
        <div class="flex gap-2 justify-end">
          <button pButton label="Cancel" severity="secondary" [outlined]="true"
            (click)="refundVisible = false"></button>
          <button pButton label="Confirm Refund" icon="pi pi-check" severity="warn"
            [loading]="refunding()"
            [disabled]="!refundAmount || refundAmount <= 0"
            (click)="submitRefund()"></button>
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class PaymentTransactionsComponent implements OnInit {
  private readonly http    = inject(HttpClient);
  private readonly toast   = inject(MessageService);

  protected readonly STATUS_OPTIONS  = STATUS_OPTIONS;
  protected readonly GATEWAY_OPTIONS = GATEWAY_OPTIONS;

  protected readonly loading    = signal(true);
  protected readonly refunding  = signal(false);
  protected readonly items      = signal<PaymentTransactionDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page       = signal(1);
  protected readonly pageSize   = 20;

  protected filterStatus:  string | null = null;
  protected filterGateway: string | null = null;
  protected filterFrom:    Date | null   = null;
  protected filterTo:      Date | null   = null;

  protected refundVisible = false;
  protected refundTxn     = signal<PaymentTransactionDto | null>(null);
  protected refundAmount  = 0;
  protected refundReason  = '';

  ngOnInit(): void { this.load(); }

  protected applyFilter(): void {
    this.page.set(1);
    this.load();
  }

  protected resetFilter(): void {
    this.filterStatus = null;
    this.filterGateway = null;
    this.filterFrom = null;
    this.filterTo = null;
    this.applyFilter();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      let params = new HttpParams()
        .set('page', this.page())
        .set('pageSize', this.pageSize);
      if (this.filterStatus)  params = params.set('status',  this.filterStatus);
      if (this.filterGateway) params = params.set('gateway', this.filterGateway);
      if (this.filterFrom)    params = params.set('from', this.filterFrom.toISOString());
      if (this.filterTo)      params = params.set('to',   this.filterTo.toISOString());

      const result = await firstValueFrom(
        this.http.get<PagedResult>(ApiEndpoints.payment.transactions, { params })
      );
      this.items.set(result.items);
      this.totalCount.set(result.totalCount);
    } finally {
      this.loading.set(false);
    }
  }

  protected onPage(e: PaginatorState): void {
    this.page.set((e.page ?? 0) + 1);
    this.load();
  }

  protected openRefund(txn: PaymentTransactionDto): void {
    this.refundTxn.set(txn);
    this.refundAmount = txn.amount;
    this.refundReason = '';
    this.refundVisible = true;
  }

  protected async submitRefund(): Promise<void> {
    const txn = this.refundTxn();
    if (!txn) return;
    this.refunding.set(true);
    try {
      await firstValueFrom(
        this.http.post(ApiEndpoints.payment.refund(txn.id), {
          refundAmount: this.refundAmount,
          reason: this.refundReason,
        })
      );
      this.toast.add({ severity: 'success', summary: 'Refund initiated', life: 3000 });
      this.refundVisible = false;
      await this.load();
    } catch {
      this.toast.add({ severity: 'error', summary: 'Refund failed', life: 4000 });
    } finally {
      this.refunding.set(false);
    }
  }

  protected statusSeverity(status: string): 'success' | 'danger' | 'warn' | 'info' | 'secondary' {
    switch (status) {
      case 'Success':           return 'success';
      case 'Failed':            return 'danger';
      case 'Refunded':          return 'warn';
      case 'PartiallyRefunded': return 'warn';
      case 'Cancelled':         return 'secondary';
      default:                  return 'info';
    }
  }

  protected void(_: unknown): void {}
}
