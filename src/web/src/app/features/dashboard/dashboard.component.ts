import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

interface StatCard {
  label: string;
  value: string;
  icon: string;
  color: string;
  bg: string;
  trend?: string;
  trendUp?: boolean;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">

      <!-- Greeting -->
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-white">
          Good {{ greeting() }}, {{ firstName() }} 👋
        </h1>
        <p class="text-slate-500 dark:text-slate-400 text-sm mt-1">
          Here's what's happening with your shop today.
        </p>
      </div>

      <!-- KPI cards -->
      <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        @for (card of stats; track card.label) {
          <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 hover:shadow-md hover:shadow-slate-200/60 dark:hover:shadow-slate-900/60 transition-all duration-200">
            <div class="flex items-start justify-between mb-4">
              <div class="p-2.5 rounded-xl" [class]="card.bg">
                <i [class]="'pi ' + card.icon + ' text-base ' + card.color"></i>
              </div>
              @if (card.trend) {
                <span class="text-xs font-medium px-2 py-0.5 rounded-full"
                      [class]="card.trendUp
                        ? 'text-emerald-700 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/40'
                        : 'text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-950/40'">
                  {{ card.trendUp ? '▲' : '▼' }} {{ card.trend }}
                </span>
              }
            </div>
            <div class="text-2xl font-bold text-slate-900 dark:text-white mb-1">{{ card.value }}</div>
            <div class="text-sm text-slate-500 dark:text-slate-400">{{ card.label }}</div>
          </div>
        }
      </div>

      <!-- Placeholder panels -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">

        <!-- Recent activity -->
        <div class="lg:col-span-2 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
          <div class="flex items-center justify-between px-5 py-4 border-b border-slate-100 dark:border-slate-800">
            <h3 class="font-semibold text-slate-900 dark:text-white text-sm">Recent Invoices</h3>
            <span class="text-xs text-slate-400 font-medium bg-slate-100 dark:bg-slate-800 px-2.5 py-1 rounded-full">Coming in Phase 1</span>
          </div>
          <div class="flex flex-col items-center justify-center py-16 px-4">
            <div class="w-14 h-14 rounded-2xl bg-indigo-50 dark:bg-indigo-950/40 flex items-center justify-center mb-4">
              <i class="pi pi-receipt text-2xl text-indigo-500"></i>
            </div>
            <p class="text-sm text-slate-500 dark:text-slate-400 text-center max-w-xs">
              Invoice management will be available in Phase 1. It'll include GST billing, e-way bills, and payment tracking.
            </p>
          </div>
        </div>

        <!-- Quick actions -->
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
          <div class="flex items-center justify-between px-5 py-4 border-b border-slate-100 dark:border-slate-800">
            <h3 class="font-semibold text-slate-900 dark:text-white text-sm">Quick Actions</h3>
          </div>
          <div class="p-4 space-y-2">
            @for (action of quickActions; track action.label) {
              <button class="w-full flex items-center gap-3 px-3.5 py-3 rounded-xl text-left
                             hover:bg-slate-50 dark:hover:bg-slate-800
                             transition-colors group"
                      disabled>
                <div class="w-9 h-9 rounded-lg flex items-center justify-center shrink-0" [class]="action.bg">
                  <i [class]="'pi ' + action.icon + ' text-sm ' + action.color"></i>
                </div>
                <div>
                  <div class="text-sm font-medium text-slate-700 dark:text-slate-300">{{ action.label }}</div>
                  <div class="text-xs text-slate-400">Phase 1</div>
                </div>
              </button>
            }
          </div>
        </div>
      </div>

      <!-- Phase roadmap banner -->
      <div class="bg-gradient-to-r from-indigo-950 to-slate-900 rounded-2xl p-5 border border-indigo-900/50">
        <div class="flex items-start gap-4">
          <div class="w-10 h-10 rounded-xl bg-indigo-600/30 flex items-center justify-center shrink-0">
            <i class="pi pi-bolt text-indigo-400"></i>
          </div>
          <div>
            <h3 class="text-white font-semibold text-sm mb-1">Phase 1 modules coming soon</h3>
            <p class="text-indigo-300/70 text-xs leading-relaxed">
              CRM · Inventory · Billing · Wallet · Notifications · Dashboard analytics
            </p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class DashboardComponent {
  protected readonly auth = inject(AuthService);

  protected greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'morning';
    if (h < 17) return 'afternoon';
    return 'evening';
  }

  protected firstName(): string {
    return this.auth.currentUser()?.displayName?.split(' ')[0] ?? 'there';
  }

  protected readonly stats: StatCard[] = [
    { label: 'Total Sales Today',  value: '₹ —',  icon: 'pi-indian-rupee', color: 'text-indigo-600 dark:text-indigo-400', bg: 'bg-indigo-50 dark:bg-indigo-950/40', trend: '—' , trendUp: true  },
    { label: 'Invoices Today',     value: '—',    icon: 'pi-file-edit',    color: 'text-violet-600 dark:text-violet-400', bg: 'bg-violet-50 dark:bg-violet-950/40', trend: '—' , trendUp: true  },
    { label: 'Active Products',    value: '—',    icon: 'pi-box',          color: 'text-sky-600 dark:text-sky-400',    bg: 'bg-sky-50 dark:bg-sky-950/40'                                         },
    { label: 'Customers',          value: '—',    icon: 'pi-users',        color: 'text-emerald-600 dark:text-emerald-400', bg: 'bg-emerald-50 dark:bg-emerald-950/40'                            },
  ];

  protected readonly quickActions = [
    { label: 'New Invoice',    icon: 'pi-plus',       color: 'text-indigo-600', bg: 'bg-indigo-50 dark:bg-indigo-950/50'  },
    { label: 'Add Product',   icon: 'pi-box',        color: 'text-sky-600',    bg: 'bg-sky-50 dark:bg-sky-950/50'         },
    { label: 'Add Customer',  icon: 'pi-user-plus',  color: 'text-emerald-600',bg: 'bg-emerald-50 dark:bg-emerald-950/50' },
    { label: 'Stock Entry',   icon: 'pi-database',   color: 'text-violet-600', bg: 'bg-violet-50 dark:bg-violet-950/50'   },
  ];
}
