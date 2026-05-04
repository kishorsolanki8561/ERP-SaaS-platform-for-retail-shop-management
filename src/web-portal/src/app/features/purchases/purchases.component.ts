import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe, SlicePipe } from '@angular/common';
import { PortalApiService, PurchaseHistory } from '../../core/services/portal-api.service';

@Component({
  selector: 'app-purchases',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DecimalPipe, SlicePipe],
  template: `
    <div class="space-y-4">

      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-gray-900">Purchase History</h1>
        <span class="text-sm text-gray-400">{{ totalCount() }} invoices</span>
      </div>

      <div class="bg-white rounded-xl border border-gray-200 overflow-hidden">
        @if (loading()) {
          <div class="divide-y divide-gray-100">
            @for (_ of [1,2,3,4,5]; track $index) {
              <div class="px-5 py-4 animate-pulse">
                <div class="h-4 bg-gray-100 rounded w-1/3 mb-2"></div>
                <div class="h-3 bg-gray-100 rounded w-1/4"></div>
              </div>
            }
          </div>
        } @else if (purchases().length === 0) {
          <div class="flex flex-col items-center justify-center py-20 text-center">
            <i class="pi pi-receipt text-4xl text-gray-200 mb-4"></i>
            <p class="text-gray-500 font-medium">No purchases found</p>
            <p class="text-sm text-gray-400 mt-1">Your invoices will appear here once you shop.</p>
          </div>
        } @else {
          <!-- Table header (desktop) -->
          <div class="hidden md:grid grid-cols-[1fr_1fr_auto_auto_auto] gap-4 px-5 py-3 bg-gray-50 border-b border-gray-100 text-xs font-semibold text-gray-500 uppercase tracking-wide">
            <span>Invoice</span>
            <span>Shop</span>
            <span>Date</span>
            <span>Total</span>
            <span>Status</span>
          </div>

          <div class="divide-y divide-gray-100">
            @for (p of purchases(); track p.invoiceId) {
              <a
                [routerLink]="['/purchases', p.invoiceId]"
                class="flex md:grid md:grid-cols-[1fr_1fr_auto_auto_auto] gap-4 items-center px-5 py-4 hover:bg-gray-50 transition-colors">
                <div>
                  <p class="text-sm font-semibold text-gray-800">{{ p.invoiceNumber }}</p>
                  <p class="text-xs text-gray-400 md:hidden mt-0.5">{{ p.shopName }}</p>
                </div>
                <p class="hidden md:block text-sm text-gray-600">{{ p.shopName }}</p>
                <p class="hidden md:block text-sm text-gray-500">{{ p.invoiceDate | slice:0:10 }}</p>
                <p class="text-sm font-semibold text-gray-900 ml-auto md:ml-0">₹{{ p.grandTotal | number:'1.2-2' }}</p>
                <span class="hidden md:inline text-xs px-2 py-0.5 rounded-full font-medium"
                  [class]="p.status === 'Finalized' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'">
                  {{ p.status }}
                </span>
              </a>
            }
          </div>

          <!-- Pagination -->
          @if (totalPages() > 1) {
            <div class="flex items-center justify-between px-5 py-4 border-t border-gray-100">
              <button
                [disabled]="page() === 1"
                (click)="prevPage()"
                class="px-4 py-2 text-sm rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">
                ← Previous
              </button>
              <span class="text-sm text-gray-500">Page {{ page() }} of {{ totalPages() }}</span>
              <button
                [disabled]="page() === totalPages()"
                (click)="nextPage()"
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
export class PurchasesComponent implements OnInit {
  private readonly api = inject(PortalApiService);

  readonly purchases = signal<PurchaseHistory[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly loading = signal(true);
  readonly totalPages = () => Math.ceil(this.totalCount() / this.pageSize) || 1;

  async ngOnInit() { await this.load(); }

  async prevPage() { this.page.update(p => p - 1); await this.load(); }
  async nextPage() { this.page.update(p => p + 1); await this.load(); }

  private async load() {
    this.loading.set(true);
    try {
      const r = await this.api.listPurchases(this.page(), this.pageSize);
      this.purchases.set(r.items ?? []);
      this.totalCount.set(r.totalCount ?? 0);
    } finally {
      this.loading.set(false);
    }
  }
}
