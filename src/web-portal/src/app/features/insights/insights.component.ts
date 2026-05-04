import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { PortalApiService, CustomerInsights } from '../../core/services/portal-api.service';

@Component({
  selector: 'app-insights',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DecimalPipe],
  template: `
    <div class="space-y-4">

      <div class="flex flex-col sm:flex-row sm:items-center gap-3 justify-between">
        <h1 class="text-xl font-bold text-gray-900">Spending Insights</h1>

        <!-- Date filter -->
        <div class="flex items-center gap-2 text-sm">
          <input type="date" [(ngModel)]="from"
            class="border border-gray-200 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-purple-300" />
          <span class="text-gray-400">to</span>
          <input type="date" [(ngModel)]="to"
            class="border border-gray-200 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-purple-300" />
          <button (click)="load()"
            class="bg-purple-600 hover:bg-purple-700 text-white text-sm font-medium px-4 py-1.5 rounded-lg transition-colors">
            Apply
          </button>
        </div>
      </div>

      @if (loading()) {
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          @for (_ of [1,2]; track $index) {
            <div class="bg-white rounded-xl border border-gray-200 p-5 animate-pulse h-28"></div>
          }
        </div>
      } @else if (!insights()) {
        <div class="bg-white rounded-xl border border-gray-200 flex flex-col items-center justify-center py-20 text-center">
          <i class="pi pi-chart-line text-4xl text-gray-200 mb-4"></i>
          <p class="text-gray-400">No data available for the selected period.</p>
        </div>
      } @else {
        <!-- Summary cards -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div class="bg-white rounded-xl border border-gray-200 p-5">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Total Spend</p>
            <p class="text-3xl font-bold text-gray-900 mt-2">₹{{ insights()!.totalSpend | number:'1.2-2' }}</p>
          </div>
          <div class="bg-white rounded-xl border border-gray-200 p-5">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Total Invoices</p>
            <p class="text-3xl font-bold text-gray-900 mt-2">{{ insights()!.totalInvoices }}</p>
          </div>
        </div>

        <!-- Spend by shop -->
        @if (insights()!.byShop.length > 0) {
          <div class="bg-white rounded-xl border border-gray-200 overflow-hidden">
            <div class="px-5 py-4 border-b border-gray-100">
              <h2 class="font-semibold text-gray-800 text-sm">Spend by Shop</h2>
            </div>
            <div class="divide-y divide-gray-100">
              @for (shop of insights()!.byShop; track shop.shopId) {
                <div class="px-5 py-4">
                  <div class="flex items-center justify-between mb-2">
                    <div>
                      <p class="text-sm font-semibold text-gray-800">{{ shop.shopName }}</p>
                      <p class="text-xs text-gray-400 mt-0.5">{{ shop.invoices }} invoice{{ shop.invoices !== 1 ? 's' : '' }}</p>
                    </div>
                    <p class="text-sm font-bold text-gray-900">₹{{ shop.spend | number:'1.2-2' }}</p>
                  </div>
                  <!-- Bar -->
                  @if (insights()!.totalSpend > 0) {
                    <div class="h-1.5 bg-gray-100 rounded-full overflow-hidden">
                      <div class="h-full bg-purple-500 rounded-full transition-all"
                        [style.width.%]="(shop.spend / insights()!.totalSpend) * 100">
                      </div>
                    </div>
                  }
                </div>
              }
            </div>
          </div>
        }
      }
    </div>
  `
})
export class InsightsComponent implements OnInit {
  private readonly api = inject(PortalApiService);

  readonly insights = signal<CustomerInsights | null>(null);
  readonly loading = signal(true);

  from = '';
  to = '';

  async ngOnInit() { await this.load(); }

  async load() {
    this.loading.set(true);
    try {
      const r = await this.api.getInsights(this.from || undefined, this.to || undefined);
      if (r.isSuccess) this.insights.set(r.value);
    } finally {
      this.loading.set(false);
    }
  }
}
