import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { TagModule } from 'primeng/tag';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface UpcomingRenewalDto {
  shopId: number;
  shopName: string;
  planLabel: string;
  amount: number;
  renewsAtUtc: string;
}

interface SubscriptionDashboardDto {
  mrr: number;
  arr: number;
  activeShops: number;
  trialShops: number;
  expiredShops: number;
  churnRate: number;
  upcomingRenewals: UpcomingRenewalDto[];
}

@Component({
  selector: 'app-platform-subscription-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ButtonModule, BadgeModule, TagModule, PageHeaderComponent],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        title="Subscription Dashboard"
        subtitle="MRR, churn and upcoming renewals">
        <button pButton icon="pi pi-refresh" label="Refresh" class="p-button-outlined p-button-sm"
          (click)="load()"></button>
      </app-page-header>

      @if (loading()) {
        <div class="flex justify-center py-12"><i class="pi pi-spin pi-spinner text-4xl text-primary-400"></i></div>
      } @else if (data()) {
        <!-- KPI cards -->
        <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
          <div class="bg-white rounded-xl border p-4 text-center">
            <div class="text-xs text-gray-500 mb-1">MRR</div>
            <div class="text-2xl font-bold text-green-600">₹{{ data()!.mrr | number:'1.0-0' }}</div>
          </div>
          <div class="bg-white rounded-xl border p-4 text-center">
            <div class="text-xs text-gray-500 mb-1">ARR</div>
            <div class="text-2xl font-bold text-green-700">₹{{ data()!.arr | number:'1.0-0' }}</div>
          </div>
          <div class="bg-white rounded-xl border p-4 text-center">
            <div class="text-xs text-gray-500 mb-1">Active</div>
            <div class="text-2xl font-bold text-blue-600">{{ data()!.activeShops }}</div>
          </div>
          <div class="bg-white rounded-xl border p-4 text-center">
            <div class="text-xs text-gray-500 mb-1">Trial</div>
            <div class="text-2xl font-bold text-amber-500">{{ data()!.trialShops }}</div>
          </div>
          <div class="bg-white rounded-xl border p-4 text-center">
            <div class="text-xs text-gray-500 mb-1">Expired</div>
            <div class="text-2xl font-bold text-red-500">{{ data()!.expiredShops }}</div>
          </div>
          <div class="bg-white rounded-xl border p-4 text-center">
            <div class="text-xs text-gray-500 mb-1">Churn</div>
            <div class="text-2xl font-bold" [class]="data()!.churnRate > 5 ? 'text-red-600' : 'text-gray-700'">
              {{ data()!.churnRate | number:'1.1-1' }}%
            </div>
          </div>
        </div>

        <!-- Upcoming renewals -->
        <div class="bg-white rounded-xl border">
          <div class="px-6 py-4 border-b">
            <h3 class="font-semibold text-gray-700">Renewals in Next 7 Days ({{ data()!.upcomingRenewals.length }})</h3>
          </div>
          @if (data()!.upcomingRenewals.length === 0) {
            <div class="p-8 text-center text-gray-400">No renewals due in the next 7 days</div>
          } @else {
            <table class="w-full text-sm">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-4 py-3 text-left text-gray-500 font-medium">Shop</th>
                  <th class="px-4 py-3 text-left text-gray-500 font-medium">Plan</th>
                  <th class="px-4 py-3 text-right text-gray-500 font-medium">Amount</th>
                  <th class="px-4 py-3 text-right text-gray-500 font-medium">Renews At</th>
                </tr>
              </thead>
              <tbody>
                @for (r of data()!.upcomingRenewals; track r.shopId) {
                  <tr class="border-t hover:bg-gray-50">
                    <td class="px-4 py-3 font-medium">{{ r.shopName }}</td>
                    <td class="px-4 py-3">
                      <p-tag [value]="r.planLabel" severity="info" styleClass="text-xs"></p-tag>
                    </td>
                    <td class="px-4 py-3 text-right font-medium text-green-600">₹{{ r.amount | number:'1.0-0' }}</td>
                    <td class="px-4 py-3 text-right text-gray-500">{{ r.renewsAtUtc | date:'dd MMM yyyy' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </div>
      }
    </div>
  `,
})
export class PlatformSubscriptionDashboardComponent implements OnInit {
  private http = inject(HttpClient);

  loading = signal(true);
  data = signal<SubscriptionDashboardDto | null>(null);

  ngOnInit() { this.load(); }

  async load() {
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.http.get<SubscriptionDashboardDto>(ApiEndpoints.platform.subscriptionDashboard)
      );
      this.data.set(result);
    } finally {
      this.loading.set(false);
    }
  }
}
