import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CommonModule, DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface UsageMeterDto {
  meterCode: string;
  used: number;
  quota: number;
  hardCapEnforced: boolean;
  periodStartUtc: string;
  periodEndUtc: string;
}

interface UsageForecastDto {
  meterCode: string;
  currentUsed: number;
  quota: number;
  projectedByMonthEnd: number;
  willExceed: boolean;
}

const METER_LABELS: Record<string, string> = {
  invoices:     'Monthly Invoices',
  products:     'Total Products',
  active_users: 'Active Users',
  sms:          'SMS per Month',
  email:        'Emails per Month',
  storage_mb:   'Storage (MB)',
};

const METER_ICONS: Record<string, string> = {
  invoices:     'pi pi-file-edit',
  products:     'pi pi-box',
  active_users: 'pi pi-users',
  sms:          'pi pi-mobile',
  email:        'pi pi-envelope',
  storage_mb:   'pi pi-database',
};

@Component({
  selector: 'app-usage',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, DatePipe, ButtonModule, TooltipModule, PageHeaderComponent],
  template: `
    <div class="p-6 space-y-6 max-w-5xl mx-auto">
      <app-page-header
        title="Usage & Quotas"
        subtitle="Monitor your plan limits and consumption across all meters."
        [actions]="[]"
        (actionClick)="noop($event)"
      />

      @if (loading()) {
        <div class="flex items-center justify-center py-20">
          <i class="pi pi-spin pi-spinner text-3xl text-slate-400"></i>
        </div>
      } @else {

        <!-- Quota warning banner -->
        @if (hasHardCapWarning()) {
          <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl p-4 flex items-center gap-3">
            <i class="pi pi-exclamation-circle text-red-500 text-xl"></i>
            <div>
              <p class="font-semibold text-red-700 dark:text-red-400">Hard limit reached</p>
              <p class="text-sm text-red-600 dark:text-red-300">
                One or more limits are at capacity. Upgrade your plan to continue using these features.
              </p>
            </div>
            <button class="ml-auto px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm font-medium rounded-lg transition-colors">
              Upgrade Plan
            </button>
          </div>
        }

        <!-- Meter cards -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          @for (meter of meters(); track meter.meterCode) {
            <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-700 p-5">
              <div class="flex items-center gap-3 mb-4">
                <div [class]="'w-10 h-10 rounded-xl flex items-center justify-center ' + meterIconBg(meter)">
                  <i [class]="meterIcon(meter.meterCode) + ' text-lg'"></i>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="font-semibold text-slate-800 dark:text-slate-100 truncate">
                    {{ meterLabel(meter.meterCode) }}
                  </p>
                  <p class="text-xs text-slate-500 dark:text-slate-400">
                    {{ meter.hardCapEnforced ? 'Hard limit' : 'Soft limit' }}
                  </p>
                </div>
                <div class="text-right">
                  <span [class]="'text-xs font-bold px-2 py-0.5 rounded-full ' + meterBadgeClass(meter)">
                    {{ meterStatusLabel(meter) }}
                  </span>
                </div>
              </div>

              <!-- Progress bar -->
              <div class="mb-2">
                <div class="flex justify-between text-sm mb-1">
                  <span class="font-medium text-slate-700 dark:text-slate-300">
                    {{ meter.used | number }} used
                  </span>
                  <span class="text-slate-500 dark:text-slate-400">
                    {{ meter.quota > 0 ? (meter.quota | number) : '∞' }} limit
                  </span>
                </div>
                <div class="h-2.5 bg-slate-100 dark:bg-slate-700 rounded-full overflow-hidden">
                  <div
                    [style.width.%]="meterPercent(meter)"
                    [class]="'h-full rounded-full transition-all duration-500 ' + meterBarColor(meter)">
                  </div>
                </div>
              </div>

              <!-- Forecast row -->
              @if (forecast(meter.meterCode); as fc) {
                @if (fc.willExceed) {
                  <p class="text-xs text-amber-600 dark:text-amber-400 mt-2">
                    <i class="pi pi-arrow-up mr-1"></i>
                    Projected: {{ fc.projectedByMonthEnd | number }} by month end
                  </p>
                }
              }
            </div>
          }
        </div>

        <!-- Refresh button -->
        <div class="flex justify-end">
          <p-button
            label="Refresh"
            icon="pi pi-refresh"
            severity="secondary"
            [loading]="loading()"
            (onClick)="load()">
          </p-button>
        </div>

      }
    </div>
  `,
})
export class UsageComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly loading  = signal(true);
  protected readonly meters   = signal<UsageMeterDto[]>([]);
  protected readonly forecasts = signal<UsageForecastDto[]>([]);

  protected readonly hasHardCapWarning = computed(() =>
    this.meters().some(m => m.hardCapEnforced && m.quota > 0 && m.used >= m.quota));

  ngOnInit(): void { this.load(); }

  protected async load(): Promise<void> {
    this.loading.set(true);
    try {
      const [current, fc] = await Promise.all([
        firstValueFrom(this.http.get<UsageMeterDto[]>(ApiEndpoints.usage.current)),
        firstValueFrom(this.http.get<UsageForecastDto[]>(ApiEndpoints.usage.forecast)),
      ]);
      this.meters.set(current ?? []);
      this.forecasts.set(fc ?? []);
    } finally {
      this.loading.set(false);
    }
  }

  protected meterLabel(code: string): string { return METER_LABELS[code] ?? code; }
  protected meterIcon(code: string): string  { return METER_ICONS[code] ?? 'pi pi-chart-bar'; }

  protected meterPercent(m: UsageMeterDto): number {
    if (m.quota <= 0) return 0;
    return Math.min(100, Math.round((m.used / m.quota) * 100));
  }

  protected meterBarColor(m: UsageMeterDto): string {
    const pct = this.meterPercent(m);
    if (pct >= 100) return 'bg-red-500';
    if (pct >= 80)  return 'bg-amber-500';
    return 'bg-emerald-500';
  }

  protected meterIconBg(m: UsageMeterDto): string {
    const pct = this.meterPercent(m);
    if (pct >= 100) return 'bg-red-100 dark:bg-red-900/30 text-red-600';
    if (pct >= 80)  return 'bg-amber-100 dark:bg-amber-900/30 text-amber-600';
    return 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600';
  }

  protected meterBadgeClass(m: UsageMeterDto): string {
    const pct = this.meterPercent(m);
    if (pct >= 100) return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400';
    if (pct >= 80)  return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400';
    return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400';
  }

  protected meterStatusLabel(m: UsageMeterDto): string {
    const pct = this.meterPercent(m);
    if (pct >= 100) return 'At Limit';
    if (pct >= 80)  return 'Warning';
    return `${pct}%`;
  }

  protected forecast(meterCode: string): UsageForecastDto | undefined {
    return this.forecasts().find(f => f.meterCode === meterCode);
  }

  protected noop(_: unknown): void {}
}
