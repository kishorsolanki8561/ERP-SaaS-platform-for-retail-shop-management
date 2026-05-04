import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ApiEndpoints } from '../../shared/messages/app-api';
import { AppRoutePaths } from '../../shared/messages/app-routes';

interface DashboardSummary {
  todaySalesAmount: number;
  todayInvoiceCount: number;
  todaySalesTrend: string;
  todaySalesTrendUp: boolean;
  activeProductCount: number;
  customerCount: number;
}

interface StatCard {
  label:    string;
  value:    string;
  icon:     string;
  iconBg:   string;
  iconColor: string;
  trend?:   string;
  trendUp?: boolean;
}

interface QuickAction {
  label: string;
  sub:   string;
  icon:  string;
  bg:    string;
  color: string;
  route: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <div class="p-5 lg:p-7 space-y-6 max-w-7xl mx-auto">

      <!-- Greeting ─────────────────────────────────────────────── -->
      <div class="flex items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-slate-900 dark:text-white tracking-tight">
            Good {{ greeting() }}, {{ firstName() }}
          </h1>
          <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">
            Here's a snapshot of your shop for today.
          </p>
        </div>
      </div>

      <!-- KPI cards ────────────────────────────────────────────── -->
      <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
        @for (card of stats(); track card.label) {
          <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800
                      p-5 hover:border-slate-300 dark:hover:border-slate-700
                      transition-all duration-200 group">
            <!-- Icon + trend -->
            <div class="flex items-start justify-between mb-4">
              <div class="w-10 h-10 rounded-xl flex items-center justify-center shrink-0"
                   [class]="card.iconBg">
                <i [class]="'pi ' + card.icon + ' text-[15px] ' + card.iconColor"></i>
              </div>
              @if (card.trend) {
                <span class="text-[11px] font-semibold px-2 py-0.5 rounded-full"
                      [class]="card.trendUp
                        ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400'
                        : 'bg-red-50 dark:bg-red-950/40 text-red-600 dark:text-red-400'">
                  {{ card.trendUp ? '▲' : '▼' }} {{ card.trend }}
                </span>
              }
            </div>

            <!-- Value -->
            @if (loading()) {
              <div class="h-7 w-28 rounded-lg bg-slate-100 dark:bg-slate-800 animate-pulse mb-1.5"></div>
            } @else {
              <div class="text-2xl font-bold text-slate-900 dark:text-white mb-1 tabular-nums">
                {{ card.value }}
              </div>
            }
            <div class="text-[13px] text-slate-500 dark:text-slate-400">{{ card.label }}</div>
          </div>
        }
      </div>

      <!-- Main grid ────────────────────────────────────────────── -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-5">

        <!-- Recent invoices (2/3 width) -->
        <div class="lg:col-span-2 bg-white dark:bg-slate-900 rounded-2xl
                    border border-slate-200 dark:border-slate-800 overflow-hidden">
          <div class="flex items-center justify-between px-5 py-4
                      border-b border-slate-100 dark:border-slate-800">
            <h3 class="text-sm font-semibold text-slate-900 dark:text-white">Recent Invoices</h3>
            <button class="text-xs text-indigo-500 hover:text-indigo-600 dark:hover:text-indigo-400
                           font-medium transition-colors"
                    (click)="navigate(paths.billing.invoices)">View all →</button>
          </div>

          <!-- Placeholder rows -->
          <div class="divide-y divide-slate-100 dark:divide-slate-800">
            @for (row of [1,2,3,4,5]; track row) {
              <div class="flex items-center justify-between px-5 py-3.5 gap-4">
                <div class="flex items-center gap-3 min-w-0">
                  <div class="w-8 h-8 rounded-lg bg-indigo-50 dark:bg-indigo-950/40
                               flex items-center justify-center shrink-0">
                    <i class="pi pi-file-edit text-indigo-400 text-xs"></i>
                  </div>
                  <div class="min-w-0">
                    @if (loading()) {
                      <div class="h-3.5 w-32 rounded bg-slate-100 dark:bg-slate-800 animate-pulse mb-1.5"></div>
                      <div class="h-3 w-20 rounded bg-slate-100 dark:bg-slate-800 animate-pulse"></div>
                    } @else {
                      <div class="text-sm font-medium text-slate-800 dark:text-slate-200 truncate">—</div>
                      <div class="text-xs text-slate-400 dark:text-slate-500">No recent invoices</div>
                    }
                  </div>
                </div>
                @if (!loading()) {
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px]
                               font-semibold bg-slate-100 text-slate-500
                               dark:bg-slate-800 dark:text-slate-400 shrink-0">
                    —
                  </span>
                }
              </div>
            }
          </div>
        </div>

        <!-- Quick actions (1/3 width) -->
        <div class="bg-white dark:bg-slate-900 rounded-2xl
                    border border-slate-200 dark:border-slate-800 overflow-hidden">
          <div class="px-5 py-4 border-b border-slate-100 dark:border-slate-800">
            <h3 class="text-sm font-semibold text-slate-900 dark:text-white">Quick Actions</h3>
          </div>
          <div class="p-3 space-y-1">
            @for (action of quickActions; track action.label) {
              <button
                class="w-full flex items-center gap-3 px-3 py-3 rounded-xl text-left
                       hover:bg-slate-50 dark:hover:bg-slate-800
                       transition-colors cursor-pointer group"
                (click)="navigate(action.route)">
                <div class="w-9 h-9 rounded-lg flex items-center justify-center shrink-0"
                     [class]="action.bg">
                  <i [class]="'pi ' + action.icon + ' text-sm ' + action.color"></i>
                </div>
                <div class="min-w-0">
                  <div class="text-[13px] font-semibold text-slate-700 dark:text-slate-300">
                    {{ action.label }}
                  </div>
                  <div class="text-[11px] text-slate-400 dark:text-slate-500">{{ action.sub }}</div>
                </div>
                <i class="pi pi-chevron-right text-[10px] text-slate-300 dark:text-slate-600
                          group-hover:text-slate-400 ml-auto shrink-0"></i>
              </button>
            }
          </div>
        </div>
      </div>

    </div>
  `
})
export class DashboardComponent implements OnInit {
  protected readonly auth    = inject(AuthService);
  private   readonly http    = inject(HttpClient);
  private   readonly router  = inject(Router);
  protected readonly paths   = AppRoutePaths;

  protected readonly loading = signal(true);
  protected readonly stats   = signal<StatCard[]>(this.blankStats());

  protected greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'morning';
    if (h < 17) return 'afternoon';
    return 'evening';
  }

  protected firstName(): string {
    return this.auth.currentUser()?.displayName?.split(' ')[0] ?? 'there';
  }

  async ngOnInit(): Promise<void> {
    try {
      const s = await firstValueFrom(
        this.http.get<DashboardSummary>(ApiEndpoints.dashboard.summary)
      );
      this.stats.set([
        {
          label:     'Total Sales Today',
          value:     `₹ ${s.todaySalesAmount.toLocaleString('en-IN', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`,
          icon:      'pi-indian-rupee',
          iconColor: 'text-indigo-600 dark:text-indigo-400',
          iconBg:    'bg-indigo-50 dark:bg-indigo-950/40',
          trend:     s.todaySalesTrend,
          trendUp:   s.todaySalesTrendUp,
        },
        {
          label:     'Invoices Today',
          value:     String(s.todayInvoiceCount),
          icon:      'pi-file-edit',
          iconColor: 'text-violet-600 dark:text-violet-400',
          iconBg:    'bg-violet-50 dark:bg-violet-950/40',
        },
        {
          label:     'Active Products',
          value:     String(s.activeProductCount),
          icon:      'pi-box',
          iconColor: 'text-sky-600 dark:text-sky-400',
          iconBg:    'bg-sky-50 dark:bg-sky-950/40',
        },
        {
          label:     'Customers',
          value:     String(s.customerCount),
          icon:      'pi-users',
          iconColor: 'text-emerald-600 dark:text-emerald-400',
          iconBg:    'bg-emerald-50 dark:bg-emerald-950/40',
        },
      ]);
    } catch { /* errorInterceptor shows toast */ }
    finally { this.loading.set(false); }
  }

  private blankStats(): StatCard[] {
    return [
      { label: 'Total Sales Today', value: '₹ —', icon: 'pi-indian-rupee', iconColor: 'text-indigo-600 dark:text-indigo-400',  iconBg: 'bg-indigo-50 dark:bg-indigo-950/40'  },
      { label: 'Invoices Today',    value: '—',    icon: 'pi-file-edit',    iconColor: 'text-violet-600 dark:text-violet-400',  iconBg: 'bg-violet-50 dark:bg-violet-950/40'  },
      { label: 'Active Products',   value: '—',    icon: 'pi-box',          iconColor: 'text-sky-600 dark:text-sky-400',         iconBg: 'bg-sky-50 dark:bg-sky-950/40'         },
      { label: 'Customers',         value: '—',    icon: 'pi-users',        iconColor: 'text-emerald-600 dark:text-emerald-400', iconBg: 'bg-emerald-50 dark:bg-emerald-950/40' },
    ];
  }

  protected navigate(route: string): void { this.router.navigate([route]); }

  protected readonly quickActions: QuickAction[] = [
    { label: 'New Invoice',   sub: 'Create a sale invoice',  icon: 'pi-plus',      color: 'text-indigo-600',  bg: 'bg-indigo-50 dark:bg-indigo-950/50',   route: AppRoutePaths.billing.invoices    },
    { label: 'Add Product',   sub: 'Manage your catalog',    icon: 'pi-box',       color: 'text-sky-600',     bg: 'bg-sky-50 dark:bg-sky-950/50',          route: AppRoutePaths.inventory.products  },
    { label: 'Add Customer',  sub: 'Register a new buyer',   icon: 'pi-user-plus', color: 'text-emerald-600', bg: 'bg-emerald-50 dark:bg-emerald-950/50',  route: AppRoutePaths.crm.customers       },
    { label: 'Wallet',        sub: 'Customer credit ledger', icon: 'pi-wallet',    color: 'text-violet-600',  bg: 'bg-violet-50 dark:bg-violet-950/50',    route: AppRoutePaths.wallet.balances     },
  ];
}
