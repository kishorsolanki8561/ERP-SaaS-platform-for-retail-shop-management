import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe, SlicePipe } from '@angular/common';
import { PortalApiService, LinkedShop } from '../../core/services/portal-api.service';

@Component({
  selector: 'app-shops',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DecimalPipe, SlicePipe],
  template: `
    <div class="space-y-4">

      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-gray-900">My Shops</h1>
        <span class="text-sm text-gray-400">{{ shops().length }} linked</span>
      </div>

      @if (loading()) {
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (_ of [1,2,3]; track $index) {
            <div class="bg-white rounded-xl border border-gray-200 p-5 animate-pulse h-36"></div>
          }
        </div>
      } @else if (shops().length === 0) {
        <div class="bg-white rounded-xl border border-gray-200 flex flex-col items-center justify-center py-20 text-center">
          <i class="pi pi-shop text-4xl text-gray-200 mb-4"></i>
          <p class="text-gray-500 font-medium">No linked shops</p>
          <p class="text-sm text-gray-400 mt-1">Shops you've purchased from will appear here.</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (shop of shops(); track shop.shopId) {
            <div class="bg-white rounded-xl border border-gray-200 p-5 flex flex-col gap-3">
              <div class="flex items-start gap-3">
                <div class="w-10 h-10 rounded-xl bg-purple-100 flex items-center justify-center shrink-0">
                  <i class="pi pi-shop text-purple-600"></i>
                </div>
                <div class="min-w-0">
                  <p class="font-semibold text-gray-800 truncate">{{ shop.shopName }}</p>
                  <p class="text-xs text-gray-400 mt-0.5">Linked {{ shop.linkedAtUtc | slice:0:10 }}</p>
                </div>
              </div>

              <div class="grid grid-cols-2 gap-2 pt-1 border-t border-gray-100">
                <div>
                  <p class="text-xs text-gray-400">Total Spend</p>
                  <p class="text-sm font-semibold text-gray-800">₹{{ shop.totalSpend | number:'1.2-2' }}</p>
                </div>
                <div class="flex gap-1.5 items-end justify-end">
                  @if (shop.hasWallet) {
                    <span class="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded-full font-medium">Wallet</span>
                  }
                  @if (shop.hasOnlineOrders) {
                    <span class="text-xs bg-green-50 text-green-600 px-2 py-0.5 rounded-full font-medium">Online</span>
                  }
                </div>
              </div>

              @if (shop.hasOnlineOrders) {
                <a
                  [routerLink]="['/shops', shop.shopId, 'catalog']"
                  class="mt-1 flex items-center justify-center gap-1.5 text-xs font-medium text-purple-600 hover:text-purple-700 border border-purple-200 hover:border-purple-300 rounded-lg py-2 transition-colors">
                  <i class="pi pi-shopping-bag text-xs"></i> Browse Catalog
                </a>
              }
            </div>
          }
        </div>
      }
    </div>
  `
})
export class ShopsComponent implements OnInit {
  private readonly api = inject(PortalApiService);

  readonly shops = signal<LinkedShop[]>([]);
  readonly loading = signal(true);

  async ngOnInit() {
    try {
      const r = await this.api.listShops(1, 50);
      this.shops.set(r.items ?? []);
    } finally {
      this.loading.set(false);
    }
  }
}
