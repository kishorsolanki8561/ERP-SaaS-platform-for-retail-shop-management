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
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface MarketplaceOrderRow {
  id: number;
  channelName: string;
  channelOrderRef: string;
  customerName: string;
  orderDate: string;
  status: string;
  totalAmount: number;
  invoiceId?: number;
}

const STATUS_BADGE: Record<string, string> = {
  Pending:   'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-100 text-amber-700',
  Confirmed: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-blue-100 text-blue-700',
  Shipped:   'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-indigo-100 text-indigo-700',
  Delivered: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700',
  Cancelled: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-red-100 text-red-600',
  Converted: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
  '*':       'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
};

@Component({
  selector: 'app-marketplace-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule,
    PageHeaderComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.marketplace.ordersTitle"
        [subtitle]="labels.marketplace.ordersSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800 gap-4">
          <div class="relative flex-1 max-w-xs">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm pointer-events-none"></i>
            <input pInputText [(ngModel)]="searchQuery" (ngModelChange)="onSearch($event)"
                   placeholder="Search orders..." class="!pl-9 !h-9 !rounded-lg !text-sm !w-full" />
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
              <th style="width: 100px">Channel</th>
              <th style="width: 150px">Order Ref</th>
              <th>Customer</th>
              <th style="width: 130px">Order Date</th>
              <th style="width: 110px">Status</th>
              <th style="width: 130px" class="text-right">Amount</th>
              <th style="width: 100px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td>
                <span class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold
                             bg-indigo-50 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-300">
                  {{ row.channelName }}
                </span>
              </td>
              <td class="font-mono text-sm text-slate-700 dark:text-slate-300">{{ row.channelOrderRef }}</td>
              <td class="text-slate-800 dark:text-slate-200">{{ row.customerName }}</td>
              <td class="tabular-nums text-slate-500">{{ row.orderDate | date:'dd MMM yyyy' }}</td>
              <td><span [class]="statusClass(row.status)">{{ row.status }}</span></td>
              <td class="text-right tabular-nums font-semibold text-slate-800 dark:text-slate-200">
                ₹ {{ row.totalAmount | number:'1.2-2' }}
              </td>
              <td class="text-right">
                @if (row.status === 'Pending' || row.status === 'Confirmed') {
                  @if (!row.invoiceId) {
                    <button pButton icon="pi pi-file-invoice" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                            pTooltip="Convert to Invoice" tooltipPosition="left"
                            (click)="convertToInvoice(row)" [disabled]="actionId() === row.id"></button>
                  }
                }
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="7">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-shopping-bag text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No marketplace orders</p>
                  <p class="text-xs text-slate-400">Connect a marketplace account and sync to pull orders.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>
  `
})
export class MarketplaceOrdersComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly actionId = signal<number | null>(null);
  protected readonly allRows  = signal<MarketplaceOrderRow[]>([]);

  protected searchQuery = '';

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.channelOrderRef.toLowerCase().includes(q) ||
      r.customerName.toLowerCase().includes(q) ||
      r.channelName.toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.marketplace.syncNow, icon: 'pi pi-sync', severity: 'secondary', permission: Permissions.marketplace.sync },
  ];

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.marketplace.syncNow) this.syncAll();
  }

  protected statusClass(status: string): string {
    return STATUS_BADGE[status] ?? STATUS_BADGE['*'];
  }

  protected async convertToInvoice(row: MarketplaceOrderRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.marketplace.convertOrder(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  private async syncAll(): Promise<void> {
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.marketplace.syncAll, {}));
      await this.load();
    } catch { /* handled */ }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<MarketplaceOrderRow[]>(ApiEndpoints.marketplace.orders));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }
}
