import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe, SlicePipe } from '@angular/common';
import { PortalAuthService } from '../../core/auth/portal-auth.service';
import { PortalApiService, CustomerInsights, PurchaseHistory } from '../../core/services/portal-api.service';

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DecimalPipe, SlicePipe],
  template: `
    <div class="space-y-6">

      <!-- Welcome banner -->
      <div class="bg-gradient-to-r from-purple-700 to-purple-900 rounded-2xl p-6 text-white">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 rounded-full bg-white/20 flex items-center justify-center shrink-0">
            <i class="pi pi-user text-xl"></i>
          </div>
          <div>
            <h1 class="text-xl font-bold">Welcome back, {{ firstName() }}!</h1>
            <p class="text-purple-200 text-sm mt-0.5">Here's a snapshot of your activity across all shops.</p>
          </div>
        </div>
      </div>

      <!-- Stats row -->
      @if (insights(); as ins) {
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div class="bg-white rounded-xl border border-gray-200 p-5">
            <p class="text-xs text-gray-500 font-medium uppercase tracking-wide">Total Spend</p>
            <p class="text-2xl font-bold text-gray-900 mt-1">₹{{ ins.totalSpend | number:'1.2-2' }}</p>
            <p class="text-xs text-gray-400 mt-1">across all shops</p>
          </div>
          <div class="bg-white rounded-xl border border-gray-200 p-5">
            <p class="text-xs text-gray-500 font-medium uppercase tracking-wide">Total Invoices</p>
            <p class="text-2xl font-bold text-gray-900 mt-1">{{ ins.totalInvoices }}</p>
            <p class="text-xs text-gray-400 mt-1">all time</p>
          </div>
          <div class="bg-white rounded-xl border border-gray-200 p-5">
            <p class="text-xs text-gray-500 font-medium uppercase tracking-wide">Shops Visited</p>
            <p class="text-2xl font-bold text-gray-900 mt-1">{{ ins.byShop.length }}</p>
            <p class="text-xs text-gray-400 mt-1">linked shops</p>
          </div>
        </div>
      } @else if (insightsLoading()) {
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          @for (_ of [1,2,3]; track $index) {
            <div class="bg-white rounded-xl border border-gray-200 p-5 animate-pulse h-24"></div>
          }
        </div>
      }

      <!-- Recent purchases -->
      <div class="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <div class="flex items-center justify-between px-5 py-4 border-b border-gray-100">
          <h2 class="font-semibold text-gray-800 text-sm">Recent Purchases</h2>
          <a routerLink="/purchases" class="text-xs text-purple-600 hover:underline font-medium">View all →</a>
        </div>

        @if (purchasesLoading()) {
          <div class="divide-y divide-gray-100">
            @for (_ of [1,2,3]; track $index) {
              <div class="px-5 py-4 animate-pulse">
                <div class="h-4 bg-gray-100 rounded w-1/3 mb-2"></div>
                <div class="h-3 bg-gray-100 rounded w-1/4"></div>
              </div>
            }
          </div>
        } @else if (recentPurchases().length === 0) {
          <div class="flex flex-col items-center justify-center py-14 text-center">
            <i class="pi pi-receipt text-3xl text-gray-300 mb-3"></i>
            <p class="text-sm text-gray-400">No purchases yet.</p>
          </div>
        } @else {
          <div class="divide-y divide-gray-100">
            @for (p of recentPurchases(); track p.invoiceId) {
              <a
                [routerLink]="['/purchases', p.invoiceId]"
                class="flex items-center justify-between px-5 py-4 hover:bg-gray-50 transition-colors">
                <div>
                  <p class="text-sm font-medium text-gray-800">{{ p.invoiceNumber }}</p>
                  <p class="text-xs text-gray-400 mt-0.5">{{ p.shopName }} · {{ p.invoiceDate | slice:0:10 }}</p>
                </div>
                <div class="text-right">
                  <p class="text-sm font-semibold text-gray-900">₹{{ p.grandTotal | number:'1.2-2' }}</p>
                  <span class="text-xs px-2 py-0.5 rounded-full font-medium"
                    [class]="p.status === 'Finalized' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'">
                    {{ p.status }}
                  </span>
                </div>
              </a>
            }
          </div>
        }
      </div>

      <!-- Quick links -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
        @for (link of quickLinks; track link.route) {
          <a [routerLink]="link.route"
            class="bg-white rounded-xl border border-gray-200 p-4 flex flex-col items-center gap-2 hover:border-purple-300 hover:bg-purple-50 transition-colors text-center">
            <i class="pi {{ link.icon }} text-xl text-purple-600"></i>
            <span class="text-xs font-medium text-gray-700">{{ link.label }}</span>
          </a>
        }
      </div>
    </div>
  `
})
export class PortalDashboardComponent implements OnInit {
  private readonly auth = inject(PortalAuthService);
  private readonly api = inject(PortalApiService);

  readonly insights = signal<CustomerInsights | null>(null);
  readonly recentPurchases = signal<PurchaseHistory[]>([]);
  readonly insightsLoading = signal(true);
  readonly purchasesLoading = signal(true);

  readonly firstName = () => this.auth.currentUser()?.displayName?.split(' ')[0] ?? 'there';

  readonly quickLinks = [
    { label: 'Purchases',  icon: 'pi-receipt',       route: '/purchases' },
    { label: 'My Orders',  icon: 'pi-shopping-cart',  route: '/orders' },
    { label: 'Inquiries',  icon: 'pi-comments',       route: '/inquiries' },
    { label: 'Profile',    icon: 'pi-user',           route: '/profile' },
  ];

  async ngOnInit() {
    this.loadInsights();
    this.loadPurchases();
  }

  private async loadInsights() {
    try {
      const r = await this.api.getInsights();
      if (r.isSuccess) this.insights.set(r.value);
    } finally {
      this.insightsLoading.set(false);
    }
  }

  private async loadPurchases() {
    try {
      const r = await this.api.listPurchases(1, 5);
      this.recentPurchases.set(r.items ?? []);
    } finally {
      this.purchasesLoading.set(false);
    }
  }
}
