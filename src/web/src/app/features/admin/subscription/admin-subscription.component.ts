import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface SubscriptionPlanDto {
  id: number;
  code: string;
  label: string;
  monthlyPrice: number;
  annualPrice: number;
  maxUsers: number;
  maxProducts: number;
  maxInvoicesPerMonth: number;
  storageQuotaMb: number;
  features: string[];
}

interface CurrentSubscriptionDto {
  subscriptionId: number;
  planCode: string;
  planLabel: string;
  monthlyPrice: number;
  annualPrice: number;
  billingCycle: string;
  startsAtUtc: string;
  endsAtUtc: string | null;
  isActive: boolean;
  maxUsers: number;
  maxProducts: number;
  maxInvoicesPerMonth: number;
  features: string[];
}

@Component({
  selector: 'app-admin-subscription',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService],
  imports: [
    CommonModule,
    ButtonModule, BadgeModule, ConfirmDialogModule,
    PageHeaderComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-5xl mx-auto">
      <app-page-header
        [title]="labels.subscription.title"
        [subtitle]="labels.subscription.subtitle"
        [actions]="[]"
      />

      @if (loading()) {
        <div class="flex items-center justify-center py-20">
          <i class="pi pi-spin pi-spinner text-3xl text-slate-400"></i>
        </div>
      } @else {

        <!-- Current plan banner -->
        @if (current()) {
          <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm p-6">
            <div class="flex items-start justify-between gap-4 flex-wrap">
              <div>
                <p class="text-xs font-semibold uppercase tracking-widest text-slate-400 mb-1">{{ labels.subscription.currentPlan }}</p>
                <h2 class="text-2xl font-bold text-slate-800 dark:text-slate-100">{{ current()!.planLabel }}</h2>
                <p class="text-sm text-slate-500 mt-1">
                  {{ current()!.billingCycle === 'Annual' ? 'Annual billing' : 'Monthly billing' }}
                  &nbsp;·&nbsp;
                  Active since {{ current()!.startsAtUtc | date:'dd MMM yyyy' }}
                </p>
              </div>
              <div class="text-right">
                <p class="text-3xl font-bold text-slate-800 dark:text-slate-100">
                  {{ current()!.billingCycle === 'Annual'
                    ? ('₹' + current()!.annualPrice)
                    : (current()!.monthlyPrice === 0 ? 'Free' : ('₹' + current()!.monthlyPrice)) }}
                </p>
                <p class="text-xs text-slate-400">
                  {{ current()!.billingCycle === 'Annual' ? labels.subscription.perYear : labels.subscription.perMonth }}
                </p>
              </div>
            </div>

            <!-- Limits bar -->
            <div class="mt-5 grid grid-cols-2 sm:grid-cols-4 gap-4 pt-4 border-t border-slate-100 dark:border-slate-800">
              <div>
                <p class="text-xs text-slate-400">{{ labels.subscription.users }}</p>
                <p class="text-sm font-semibold text-slate-700 dark:text-slate-300">{{ current()!.maxUsers }}</p>
              </div>
              <div>
                <p class="text-xs text-slate-400">{{ labels.subscription.products }}</p>
                <p class="text-sm font-semibold text-slate-700 dark:text-slate-300">{{ current()!.maxProducts }}</p>
              </div>
              <div>
                <p class="text-xs text-slate-400">{{ labels.subscription.invoicesPerMonth }}</p>
                <p class="text-sm font-semibold text-slate-700 dark:text-slate-300">{{ current()!.maxInvoicesPerMonth }}</p>
              </div>
              <div>
                <p class="text-xs text-slate-400">{{ labels.subscription.storageMb }}</p>
                <p class="text-sm font-semibold text-slate-700 dark:text-slate-300">{{ current()!.maxInvoicesPerMonth }}</p>
              </div>
            </div>
          </div>
        } @else {
          <div class="bg-amber-50 dark:bg-amber-900/20 rounded-2xl border border-amber-200 dark:border-amber-700 p-5 text-sm text-amber-700 dark:text-amber-300">
            No active subscription found. Choose a plan below.
          </div>
        }

        <!-- Billing cycle toggle -->
        <div class="flex items-center gap-2">
          <span class="text-sm font-medium text-slate-600 dark:text-slate-400">{{ labels.subscription.billingCycle }}:</span>
          <div class="flex gap-1 bg-slate-100 dark:bg-slate-800 rounded-lg p-1">
            <button (click)="billingCycle.set('Monthly')"
                    [class]="billingCycle() === 'Monthly'
                      ? 'px-4 py-1.5 text-sm font-semibold rounded-md bg-white dark:bg-slate-700 shadow-sm text-slate-800 dark:text-slate-100 transition'
                      : 'px-4 py-1.5 text-sm text-slate-500 rounded-md hover:text-slate-700 transition'">
              {{ labels.subscription.monthly }}
            </button>
            <button (click)="billingCycle.set('Annual')"
                    [class]="billingCycle() === 'Annual'
                      ? 'px-4 py-1.5 text-sm font-semibold rounded-md bg-white dark:bg-slate-700 shadow-sm text-slate-800 dark:text-slate-100 transition'
                      : 'px-4 py-1.5 text-sm text-slate-500 rounded-md hover:text-slate-700 transition'">
              {{ labels.subscription.annual }}
            </button>
          </div>
        </div>

        <!-- Plan cards -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          @for (plan of plans(); track plan.id) {
            <div [class]="isCurrentPlan(plan)
              ? 'relative bg-white dark:bg-slate-900 rounded-2xl border-2 border-indigo-500 shadow-md p-6 flex flex-col'
              : 'relative bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm p-6 flex flex-col'">

              @if (isCurrentPlan(plan)) {
                <span class="absolute -top-3 left-4 bg-indigo-500 text-white text-xs font-semibold px-3 py-0.5 rounded-full">
                  Current
                </span>
              }

              <h3 class="text-lg font-bold text-slate-800 dark:text-slate-100">{{ plan.label }}</h3>

              <div class="mt-2 mb-4">
                <span class="text-3xl font-extrabold text-slate-800 dark:text-slate-100">
                  {{ billingCycle() === 'Annual' && plan.annualPrice > 0
                    ? '₹' + plan.annualPrice
                    : (plan.monthlyPrice === 0 ? 'Free' : '₹' + plan.monthlyPrice) }}
                </span>
                @if (plan.monthlyPrice > 0) {
                  <span class="text-sm text-slate-400 ml-1">
                    {{ billingCycle() === 'Annual' ? labels.subscription.perYear : labels.subscription.perMonth }}
                  </span>
                }
              </div>

              <ul class="space-y-2 text-sm text-slate-600 dark:text-slate-400 flex-1 mb-5">
                <li class="flex items-center gap-2">
                  <i class="pi pi-users text-indigo-400 text-xs"></i>
                  {{ plan.maxUsers }} {{ labels.subscription.users }}
                </li>
                <li class="flex items-center gap-2">
                  <i class="pi pi-box text-indigo-400 text-xs"></i>
                  {{ plan.maxProducts }} {{ labels.subscription.products }}
                </li>
                <li class="flex items-center gap-2">
                  <i class="pi pi-file text-indigo-400 text-xs"></i>
                  {{ plan.maxInvoicesPerMonth }} {{ labels.subscription.invoicesPerMonth }}
                </li>
                <li class="flex items-center gap-2">
                  <i class="pi pi-database text-indigo-400 text-xs"></i>
                  {{ plan.storageQuotaMb }} {{ labels.subscription.storageMb }}
                </li>
                @for (feat of plan.features; track feat) {
                  <li class="flex items-center gap-2">
                    <i class="pi pi-check-circle text-emerald-400 text-xs"></i>
                    {{ feat }}
                  </li>
                }
              </ul>

              @if (!isCurrentPlan(plan)) {
                <p-button
                  [label]="isUpgrade(plan) ? labels.subscription.upgradeNow : labels.subscription.downgrade"
                  [severity]="isUpgrade(plan) ? 'primary' : 'secondary'"
                  [outlined]="!isUpgrade(plan)"
                  styleClass="w-full"
                  [loading]="changingTo() === plan.code"
                  (onClick)="confirmChange(plan)" />
              } @else {
                <button disabled
                        class="w-full py-2 text-sm font-medium rounded-lg bg-slate-50 dark:bg-slate-800 text-slate-400 cursor-default">
                  Current Plan
                </button>
              }
            </div>
          }
        </div>

        <!-- Cancel subscription -->
        @if (canCancel()) {
          <div class="flex justify-end">
            <button (click)="confirmCancel()"
                    class="text-sm text-red-500 hover:text-red-700 underline underline-offset-2 transition">
              {{ labels.subscription.cancelPlan }}
            </button>
          </div>
        }

      }
    </div>

    <p-confirmDialog />
  `
})
export class AdminSubscriptionComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly confirm = inject(ConfirmationService);

  protected readonly labels      = AppLabels;
  protected readonly loading     = signal(true);
  protected readonly changingTo  = signal<string | null>(null);
  protected readonly plans       = signal<SubscriptionPlanDto[]>([]);
  protected readonly current     = signal<CurrentSubscriptionDto | null>(null);
  protected readonly billingCycle = signal<'Monthly' | 'Annual'>('Monthly');

  protected readonly canCancel = computed(() => {
    const c = this.current();
    return c !== null && c.planCode !== 'Starter';
  });

  ngOnInit(): void { this.load(); }

  protected isCurrentPlan(plan: SubscriptionPlanDto): boolean {
    const c = this.current();
    return c !== null && c.planCode === plan.code && c.billingCycle === this.billingCycle();
  }

  protected isUpgrade(plan: SubscriptionPlanDto): boolean {
    const c = this.current();
    if (!c) return true;
    const currentPlan = this.plans().find(p => p.code === c.planCode);
    return (plan.monthlyPrice ?? 0) > (currentPlan?.monthlyPrice ?? 0);
  }

  protected confirmChange(plan: SubscriptionPlanDto): void {
    this.confirm.confirm({
      header: this.labels.subscription.confirmChange,
      message: `Switch to ${plan.label} (${this.billingCycle()})?`,
      acceptLabel: this.labels.subscription.changePlan,
      rejectLabel: 'Cancel',
      accept: () => this.changePlan(plan.code),
    });
  }

  protected confirmCancel(): void {
    this.confirm.confirm({
      header: this.labels.subscription.confirmCancel,
      message: this.labels.subscription.cancelWarning,
      acceptLabel: 'Yes, Cancel',
      acceptButtonStyleClass: 'p-button-danger',
      rejectLabel: 'Keep Plan',
      accept: () => this.cancelPlan(),
    });
  }

  private async changePlan(planCode: string): Promise<void> {
    this.changingTo.set(planCode);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.subscriptions.changePlan, {
        planCode,
        billingCycle: this.billingCycle(),
      }));
      await this.load();
    } catch { /* handled by interceptor */ }
    finally { this.changingTo.set(null); }
  }

  private async cancelPlan(): Promise<void> {
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.subscriptions.cancel, {}));
      await this.load();
    } catch { /* handled */ }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const [plans, current] = await Promise.all([
        firstValueFrom(this.http.get<SubscriptionPlanDto[]>(ApiEndpoints.subscriptions.plans)),
        firstValueFrom(this.http.get<CurrentSubscriptionDto>(ApiEndpoints.subscriptions.current)).catch(() => null),
      ]);
      this.plans.set(plans ?? []);
      this.current.set(current);
      if (current) {
        this.billingCycle.set(current.billingCycle === 'Annual' ? 'Annual' : 'Monthly');
      }
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }
}
