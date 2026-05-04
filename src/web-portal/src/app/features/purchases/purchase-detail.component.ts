import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DecimalPipe, SlicePipe } from '@angular/common';
import { PortalApiService, PurchaseDetail } from '../../core/services/portal-api.service';

@Component({
  selector: 'app-purchase-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DecimalPipe, SlicePipe],
  template: `
    <div class="space-y-4">

      <a routerLink="/purchases" class="inline-flex items-center gap-1.5 text-sm text-gray-500 hover:text-gray-700">
        <i class="pi pi-arrow-left text-xs"></i> Back to purchases
      </a>

      @if (loading()) {
        <div class="bg-white rounded-xl border border-gray-200 p-6 animate-pulse space-y-4">
          <div class="h-6 bg-gray-100 rounded w-1/4"></div>
          <div class="h-4 bg-gray-100 rounded w-1/3"></div>
        </div>
      } @else if (!detail()) {
        <div class="bg-white rounded-xl border border-gray-200 p-10 text-center">
          <i class="pi pi-exclamation-circle text-3xl text-gray-300 mb-3 block"></i>
          <p class="text-gray-500">Invoice not found.</p>
        </div>
      } @else {
        <div class="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <!-- Header -->
          <div class="px-6 py-5 border-b border-gray-100 bg-gray-50">
            <div class="flex items-start justify-between">
              <div>
                <h1 class="text-lg font-bold text-gray-900">{{ detail()!.invoiceNumber }}</h1>
                <p class="text-sm text-gray-500 mt-0.5">{{ detail()!.shopName }} · {{ detail()!.invoiceDate | slice:0:10 }}</p>
              </div>
              <div class="text-right">
                <p class="text-xl font-bold text-gray-900">₹{{ detail()!.grandTotal | number:'1.2-2' }}</p>
                @if (detail()!.subTotal !== detail()!.grandTotal) {
                  <p class="text-xs text-gray-400 mt-0.5">Sub-total ₹{{ detail()!.subTotal | number:'1.2-2' }}</p>
                }
              </div>
            </div>
          </div>

          <!-- Lines -->
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-gray-50 border-b border-gray-100">
                <tr>
                  <th class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Product</th>
                  <th class="text-right px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Qty</th>
                  <th class="text-right px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Unit Price</th>
                  <th class="text-right px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide">Total</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                @for (line of detail()!.lines; track $index) {
                  <tr>
                    <td class="px-6 py-3.5 text-gray-800 font-medium">
                      {{ line.productName }}
                      <span class="text-xs text-gray-400 ml-1">/ {{ line.unitCode }}</span>
                    </td>
                    <td class="px-4 py-3.5 text-right text-gray-600">{{ line.qty }}</td>
                    <td class="px-4 py-3.5 text-right text-gray-600">₹{{ line.unitPrice | number:'1.2-2' }}</td>
                    <td class="px-6 py-3.5 text-right font-semibold text-gray-900">₹{{ line.lineTotal | number:'1.2-2' }}</td>
                  </tr>
                }
              </tbody>
              <tfoot class="border-t border-gray-200">
                <tr>
                  <td colspan="3" class="px-6 py-4 text-right text-sm font-semibold text-gray-700">Grand Total</td>
                  <td class="px-6 py-4 text-right text-base font-bold text-gray-900">₹{{ detail()!.grandTotal | number:'1.2-2' }}</td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      }
    </div>
  `
})
export class PurchaseDetailComponent implements OnInit {
  private readonly api = inject(PortalApiService);
  private readonly route = inject(ActivatedRoute);

  readonly detail = signal<PurchaseDetail | null>(null);
  readonly loading = signal(true);

  async ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    try {
      const r = await this.api.getPurchase(id);
      if (r.isSuccess) this.detail.set(r.value);
    } finally {
      this.loading.set(false);
    }
  }
}
