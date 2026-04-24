import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { PortalAuthService } from '../../core/auth/portal-auth.service';

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule],
  template: `
    <div class="min-h-screen bg-slate-50 dark:bg-slate-950">

      <!-- Top bar -->
      <header class="bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 px-6 h-16 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-lg bg-violet-600 flex items-center justify-center font-bold text-white text-sm">SE</div>
          <span class="font-semibold text-slate-900 dark:text-white text-[15px]">ShopEarth Portal</span>
        </div>
        <div class="flex items-center gap-3">
          <span class="text-sm text-slate-500 dark:text-slate-400 hidden sm:block">
            {{ auth.currentUser()?.displayName }}
          </span>
          <p-button
            label="Sign out"
            icon="pi pi-sign-out"
            severity="secondary"
            size="small"
            [outlined]="true"
            (onClick)="auth.logout()" />
        </div>
      </header>

      <!-- Content -->
      <div class="max-w-5xl mx-auto px-6 py-8 space-y-6">

        <!-- Welcome -->
        <div class="bg-gradient-to-r from-violet-950 to-slate-900 rounded-2xl p-6 border border-violet-900/50">
          <div class="flex items-center gap-4">
            <div class="w-12 h-12 rounded-2xl bg-violet-600/30 flex items-center justify-center shrink-0">
              <i class="pi pi-user text-violet-400 text-xl"></i>
            </div>
            <div>
              <h1 class="text-xl font-bold text-white">
                Welcome back, {{ firstName() }}!
              </h1>
              <p class="text-violet-300/70 text-sm mt-0.5">
                Your orders and account details will appear here.
              </p>
            </div>
          </div>
        </div>

        <!-- Placeholder cards -->
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          @for (card of cards; track card.label) {
            <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5">
              <div class="flex items-center gap-3 mb-4">
                <div class="w-9 h-9 rounded-xl flex items-center justify-center" [class]="card.bg">
                  <i [class]="'pi ' + card.icon + ' ' + card.color + ' text-sm'"></i>
                </div>
                <span class="text-sm font-semibold text-slate-700 dark:text-slate-300">{{ card.label }}</span>
              </div>
              <div class="text-2xl font-bold text-slate-900 dark:text-white mb-1">—</div>
              <div class="text-xs text-slate-400 bg-slate-100 dark:bg-slate-800 rounded-full px-2 py-0.5 inline-block">Coming soon</div>
            </div>
          }
        </div>

        <!-- Recent orders placeholder -->
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
          <div class="flex items-center justify-between px-5 py-4 border-b border-slate-100 dark:border-slate-800">
            <h3 class="font-semibold text-slate-900 dark:text-white text-sm">Recent Orders</h3>
            <span class="text-xs text-slate-400 bg-slate-100 dark:bg-slate-800 px-2.5 py-1 rounded-full font-medium">Coming in Phase 1</span>
          </div>
          <div class="flex flex-col items-center justify-center py-16 px-4">
            <div class="w-14 h-14 rounded-2xl bg-violet-50 dark:bg-violet-950/40 flex items-center justify-center mb-4">
              <i class="pi pi-shopping-cart text-2xl text-violet-500"></i>
            </div>
            <p class="text-sm text-slate-500 dark:text-slate-400 text-center max-w-xs">
              Your order history, invoices, and delivery tracking will appear here once Phase 1 launches.
            </p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class PortalDashboardComponent {
  protected readonly auth = inject(PortalAuthService);

  protected firstName(): string {
    return this.auth.currentUser()?.displayName?.split(' ')[0] ?? 'there';
  }

  protected readonly cards = [
    { label: 'Total Orders',    icon: 'pi-shopping-cart', color: 'text-violet-600 dark:text-violet-400', bg: 'bg-violet-50 dark:bg-violet-950/40' },
    { label: 'Pending Invoices', icon: 'pi-file-edit',    color: 'text-sky-600 dark:text-sky-400',     bg: 'bg-sky-50 dark:bg-sky-950/40'          },
    { label: 'Loyalty Points',  icon: 'pi-star-fill',     color: 'text-amber-600 dark:text-amber-400', bg: 'bg-amber-50 dark:bg-amber-950/40'       },
  ];
}
