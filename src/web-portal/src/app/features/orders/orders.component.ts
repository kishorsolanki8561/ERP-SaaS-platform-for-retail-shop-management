import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe, SlicePipe } from '@angular/common';
import { PortalApiService, OnlineOrderSummary } from '../../core/services/portal-api.service';

@Component({
  selector: 'app-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, SlicePipe],
  template: `
    <div class="space-y-4">

      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-gray-900">My Orders</h1>
        <span class="text-sm text-gray-400">{{ totalCount() }} orders</span>
      </div>

      <div class="bg-white rounded-xl border border-gray-200 overflow-hidden">
        @if (loading()) {
          <div class="divide-y divide-gray-100">
            @for (_ of [1,2,3]; track $index) {
              <div class="px-5 py-4 animate-pulse">
                <div class="h-4 bg-gray-100 rounded w-1/3 mb-2"></div>
                <div class="h-3 bg-gray-100 rounded w-1/5"></div>
              </div>
            }
          </div>
        } @else if (orders().length === 0) {
          <div class="flex flex-col items-center justify-center py-20 text-center">
            <i class="pi pi-shopping-cart text-4xl text-gray-200 mb-4"></i>
            <p class="text-gray-500 font-medium">No orders yet</p>
            <p class="text-sm text-gray-400 mt-1">Online orders you place will appear here.</p>
          </div>
        } @else {
          <div class="hidden md:grid grid-cols-[1fr_auto_auto_auto] gap-4 px-5 py-3 bg-gray-50 border-b border-gray-100 text-xs font-semibold text-gray-500 uppercase tracking-wide">
            <span>Order</span>
            <span>Date</span>
            <span>Total</span>
            <span>Status</span>
          </div>

          <div class="divide-y divide-gray-100">
            @for (o of orders(); track o.id) {
              <div class="flex md:grid md:grid-cols-[1fr_auto_auto_auto] gap-4 items-center px-5 py-4">
                <div>
                  <p class="text-sm font-semibold text-gray-800">{{ o.orderNumber }}</p>
                  <p class="text-xs text-gray-400 md:hidden mt-0.5">{{ o.createdAtUtc | slice:0:10 }}</p>
                </div>
                <p class="hidden md:block text-sm text-gray-500">{{ o.createdAtUtc | slice:0:10 }}</p>
                <p class="text-sm font-semibold text-gray-900 ml-auto md:ml-0">₹{{ o.grandTotal | number:'1.2-2' }}</p>
                <span class="hidden md:inline text-xs px-2 py-0.5 rounded-full font-medium"
                  [class]="statusClass(o.status)">
                  {{ o.status }}
                </span>
              </div>
            }
          </div>

          @if (totalPages() > 1) {
            <div class="flex items-center justify-between px-5 py-4 border-t border-gray-100">
              <button [disabled]="page() === 1" (click)="prevPage()"
                class="px-4 py-2 text-sm rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">
                ← Previous
              </button>
              <span class="text-sm text-gray-500">Page {{ page() }} of {{ totalPages() }}</span>
              <button [disabled]="page() === totalPages()" (click)="nextPage()"
                class="px-4 py-2 text-sm rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">
                Next →
              </button>
            </div>
          }
        }
      </div>
    </div>
  `
})
export class OrdersComponent implements OnInit {
  private readonly api = inject(PortalApiService);

  readonly orders = signal<OnlineOrderSummary[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly loading = signal(true);
  readonly totalPages = () => Math.ceil(this.totalCount() / this.pageSize) || 1;

  async ngOnInit() { await this.load(); }
  async prevPage() { this.page.update(p => p - 1); await this.load(); }
  async nextPage() { this.page.update(p => p + 1); await this.load(); }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending:    'bg-yellow-100 text-yellow-700',
      Accepted:   'bg-blue-100 text-blue-700',
      Dispatched: 'bg-indigo-100 text-indigo-700',
      Delivered:  'bg-green-100 text-green-700',
      Rejected:   'bg-red-100 text-red-700',
      Cancelled:  'bg-gray-100 text-gray-500',
    };
    return map[status] ?? 'bg-gray-100 text-gray-500';
  }

  private async load() {
    this.loading.set(true);
    try {
      const r = await this.api.listOrders(this.page(), this.pageSize);
      this.orders.set(r.items ?? []);
      this.totalCount.set(r.totalCount ?? 0);
    } finally {
      this.loading.set(false);
    }
  }
}
