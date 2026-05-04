import { ChangeDetectionStrategy, Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { InputSwitchModule } from 'primeng/inputswitch';
import { FormsModule } from '@angular/forms';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { catchError, of } from 'rxjs';

interface ModuleAccessDto {
  featureCode: string;
  label: string;
  icon: string;
  isInPlan: boolean;
  isEffectivelyEnabled: boolean;
  hasOverride: boolean;
  overrideValue: boolean | null;
}

const TIER_ORDER: Record<string, number> = {
  'Module.Dashboard': 0, 'Module.Billing': 1, 'Module.Inventory': 2,
  'Module.CRM': 3, 'Module.Reports': 4,
  'Module.Accounting': 10, 'Module.HR': 11, 'Module.Purchasing': 12,
  'Module.Warranty': 13, 'Module.Pricing': 14, 'Module.Transport': 15,
  'Module.Quotations': 16, 'Module.Payment': 17, 'Module.Sync': 18,
  'Module.Wallet': 19, 'Module.Hardware': 20, 'Module.CustomerPortal': 21,
  'Module.Marketplace': 30, 'Module.ApiAccess': 31, 'Module.ServiceJobs': 32,
  'Module.Verticals': 33, 'Module.OnPrem': 34,
};

function tierOf(code: string): 'Core' | 'Business' | 'Enterprise' {
  const idx = TIER_ORDER[code] ?? 99;
  if (idx < 10) return 'Core';
  if (idx < 30) return 'Business';
  return 'Enterprise';
}

@Component({
  selector: 'app-module-access',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ButtonModule, CardModule, TagModule, TooltipModule, InputSwitchModule, FormsModule],
  template: `
    <div class="p-4 max-w-5xl">
      <h2 class="text-xl font-semibold mb-1">Module Access</h2>
      <p class="text-gray-500 text-sm mb-6">
        Control which modules are visible to your staff. Modules included in your plan can be enabled or disabled.
        Modules outside your plan require an upgrade.
      </p>

      @if (saving()) {
        <div class="text-sm text-blue-600 mb-3">Saving...</div>
      }
      @if (error()) {
        <div class="text-sm text-red-600 mb-3">{{ error() }}</div>
      }

      @for (tier of ['Core', 'Business', 'Enterprise']; track tier) {
        <div class="mb-8">
          <h3 class="text-base font-semibold text-gray-700 mb-3 flex items-center gap-2">
            <span class="w-2 h-2 rounded-full inline-block"
              [class]="tier === 'Core' ? 'bg-green-500' : tier === 'Business' ? 'bg-blue-500' : 'bg-purple-500'">
            </span>
            {{ tier }} Plan Modules
          </h3>
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            @for (mod of modulesForTier(tier); track mod.featureCode) {
              <div class="border rounded-lg p-4 flex flex-col gap-2"
                [class]="mod.isInPlan ? 'bg-white border-gray-200' : 'bg-gray-50 border-gray-100 opacity-70'">
                <div class="flex items-start justify-between">
                  <div class="flex items-center gap-2">
                    <i [class]="mod.icon + ' text-lg text-gray-600'"></i>
                    <span class="font-medium text-gray-800">{{ mod.label }}</span>
                  </div>
                  @if (mod.isInPlan) {
                    <p-inputSwitch
                      [ngModel]="mod.isEffectivelyEnabled"
                      (ngModelChange)="toggle(mod, $event)"
                      [disabled]="saving()"
                      pTooltip="{{ mod.isEffectivelyEnabled ? 'Disable for staff' : 'Enable for staff' }}"
                    />
                  } @else {
                    <i class="pi pi-lock text-gray-400" pTooltip="Not in your plan"></i>
                  }
                </div>
                <div class="flex items-center gap-2 flex-wrap">
                  @if (mod.isInPlan) {
                    <span class="text-xs px-2 py-0.5 rounded-full bg-green-50 text-green-700">Included</span>
                  } @else {
                    <span class="text-xs px-2 py-0.5 rounded-full bg-gray-100 text-gray-500">Upgrade Required</span>
                  }
                  @if (mod.hasOverride) {
                    <span class="text-xs px-2 py-0.5 rounded-full bg-amber-50 text-amber-700">Overridden</span>
                  }
                </div>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class ModuleAccessComponent {
  private http = inject(HttpClient);

  modules = signal<ModuleAccessDto[]>([]);
  saving = signal(false);
  error = signal<string | null>(null);

  constructor() {
    this.http.get<ModuleAccessDto[]>(ApiEndpoints.shopAccess.modules)
      .pipe(catchError(() => of([])))
      .subscribe(data => {
        const sorted = [...data].sort((a, b) =>
          (TIER_ORDER[a.featureCode] ?? 99) - (TIER_ORDER[b.featureCode] ?? 99));
        this.modules.set(sorted);
      });
  }

  modulesForTier(tier: string): ModuleAccessDto[] {
    return this.modules().filter(m => tierOf(m.featureCode) === tier);
  }

  toggle(mod: ModuleAccessDto, isVisible: boolean) {
    this.saving.set(true);
    this.error.set(null);
    this.http.put(ApiEndpoints.shopAccess.module(mod.featureCode), { isVisible })
      .pipe(catchError(err => {
        this.error.set(err?.error?.message ?? 'Failed to update module visibility');
        return of(null);
      }))
      .subscribe(result => {
        this.saving.set(false);
        if (result !== null) {
          this.modules.update(list => list.map(m =>
            m.featureCode === mod.featureCode
              ? { ...m, isEffectivelyEnabled: isVisible, hasOverride: true, overrideValue: isVisible }
              : m
          ));
        }
      });
  }
}
