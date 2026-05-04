import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-shop-catalog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="space-y-4">
      <a routerLink="/shops" class="inline-flex items-center gap-1.5 text-sm text-gray-500 hover:text-gray-700">
        <i class="pi pi-arrow-left text-xs"></i> Back to shops
      </a>

      <div class="bg-white rounded-xl border border-gray-200 flex flex-col items-center justify-center py-24 text-center">
        <div class="w-16 h-16 rounded-2xl bg-purple-100 flex items-center justify-center mb-4">
          <i class="pi pi-shopping-bag text-2xl text-purple-600"></i>
        </div>
        <h2 class="text-lg font-semibold text-gray-800">Shop Catalog</h2>
        <p class="text-sm text-gray-400 mt-2 max-w-xs">
          Browse products and place online orders directly from this shop. Coming soon.
        </p>
        <span class="mt-4 text-xs bg-purple-50 text-purple-600 px-3 py-1.5 rounded-full font-medium">Enterprise Feature</span>
      </div>
    </div>
  `
})
export class ShopCatalogComponent {}
