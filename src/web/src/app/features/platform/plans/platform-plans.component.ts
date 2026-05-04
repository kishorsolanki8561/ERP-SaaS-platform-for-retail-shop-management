import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { TagModule } from 'primeng/tag';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface PlatformSubscriptionPlanDto {
  id: number;
  code: string;
  label: string;
  monthlyPrice: number;
  annualPrice: number;
  maxUsers: number;
  maxProducts: number;
  maxInvoicesPerMonth: number;
  storageMb: number;
  smsQuotaPerMonth: number;
  emailQuotaPerMonth: number;
  isActive: boolean;
  features: string[];
}

interface PlanFormModel {
  code: string;
  label: string;
  monthlyPrice: number;
  annualPrice: number;
  maxUsers: number;
  maxProducts: number;
  maxInvoicesPerMonth: number;
  storageQuotaMb: number;
  smsQuotaPerMonth: number;
  emailQuotaPerMonth: number;
  isActive: boolean;
  features: string[];
}

const KNOWN_FEATURES = [
  'Billing.BarcodePos', 'Billing.EInvoice', 'Billing.EWayBill',
  'Inventory.MultiUnit', 'Inventory.BatchTracking', 'Inventory.MultiWarehouse',
  'Purchasing.BillPayments', 'CRM.CustomerPortal',
  'Marketplace.Amazon', 'Marketplace.Flipkart',
  'Reports.Advanced', 'Hr.Payroll',
  'ApiAccess.Keys', 'ApiAccess.Webhooks',
];

@Component({
  selector: 'app-platform-plans',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    ButtonModule, DialogModule, InputTextModule, InputNumberModule,
    CheckboxModule, TagModule,
    PageHeaderComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header title="Subscription Plans" subtitle="Manage platform subscription tiers">
        <button pButton icon="pi pi-plus" label="New Plan" class="p-button-sm"
          (click)="openCreate()"></button>
      </app-page-header>

      @if (loading()) {
        <div class="flex justify-center py-20">
          <i class="pi pi-spinner pi-spin text-3xl text-slate-400"></i>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 overflow-hidden">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-700">
                <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Code</th>
                <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Label</th>
                <th class="px-4 py-3 text-right font-semibold text-slate-600 dark:text-slate-400">Monthly (₹)</th>
                <th class="px-4 py-3 text-right font-semibold text-slate-600 dark:text-slate-400">Annual (₹)</th>
                <th class="px-4 py-3 text-right font-semibold text-slate-600 dark:text-slate-400">Users</th>
                <th class="px-4 py-3 text-right font-semibold text-slate-600 dark:text-slate-400">Inv/mo</th>
                <th class="px-4 py-3 text-center font-semibold text-slate-600 dark:text-slate-400">Status</th>
                <th class="px-4 py-3 text-center font-semibold text-slate-600 dark:text-slate-400">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (p of plans(); track p.id) {
                <tr class="border-b border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                  <td class="px-4 py-3 font-mono text-slate-700 dark:text-slate-300 font-medium">{{ p.code }}</td>
                  <td class="px-4 py-3 text-slate-800 dark:text-slate-200">{{ p.label }}</td>
                  <td class="px-4 py-3 text-right text-slate-700 dark:text-slate-300">{{ p.monthlyPrice | number }}</td>
                  <td class="px-4 py-3 text-right text-slate-700 dark:text-slate-300">{{ p.annualPrice | number }}</td>
                  <td class="px-4 py-3 text-right text-slate-500">{{ p.maxUsers }}</td>
                  <td class="px-4 py-3 text-right text-slate-500">{{ p.maxInvoicesPerMonth }}</td>
                  <td class="px-4 py-3 text-center">
                    <p-tag [value]="p.isActive ? 'Active' : 'Inactive'"
                           [severity]="p.isActive ? 'success' : 'secondary'" />
                  </td>
                  <td class="px-4 py-3 text-center">
                    <button pButton severity="secondary" size="small" icon="pi pi-pencil" [outlined]="true"
                      (click)="onAction({ action: 'Edit', row: p })"></button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    <!-- Create/Edit Dialog -->
    <p-dialog [(visible)]="showDialog" [modal]="true"
      [header]="editingId() ? 'Edit Plan' : 'New Plan'"
      [style]="{ width: '640px' }" [closable]="true">
      <div class="space-y-4 py-2">
        <div class="grid grid-cols-2 gap-4">
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Code</label>
            <input pInputText [(ngModel)]="form.code" [disabled]="!!editingId()"
              class="w-full" placeholder="e.g. GROWTH" />
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Label</label>
            <input pInputText [(ngModel)]="form.label" class="w-full" placeholder="e.g. Growth Plan" />
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Monthly Price (₹)</label>
            <p-inputNumber [(ngModel)]="form.monthlyPrice" [min]="0" class="w-full"></p-inputNumber>
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Annual Price (₹)</label>
            <p-inputNumber [(ngModel)]="form.annualPrice" [min]="0" class="w-full"></p-inputNumber>
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Max Users</label>
            <p-inputNumber [(ngModel)]="form.maxUsers" [min]="1" class="w-full"></p-inputNumber>
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Max Products</label>
            <p-inputNumber [(ngModel)]="form.maxProducts" [min]="1" class="w-full"></p-inputNumber>
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Max Invoices/Month</label>
            <p-inputNumber [(ngModel)]="form.maxInvoicesPerMonth" [min]="1" class="w-full"></p-inputNumber>
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Storage (MB)</label>
            <p-inputNumber [(ngModel)]="form.storageQuotaMb" [min]="100" class="w-full"></p-inputNumber>
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">SMS Quota/Month</label>
            <p-inputNumber [(ngModel)]="form.smsQuotaPerMonth" [min]="0" class="w-full"></p-inputNumber>
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Email Quota/Month</label>
            <p-inputNumber [(ngModel)]="form.emailQuotaPerMonth" [min]="0" class="w-full"></p-inputNumber>
          </div>
        </div>

        @if (editingId()) {
          <div class="flex items-center gap-2">
            <p-checkbox [(ngModel)]="form.isActive" [binary]="true" inputId="isActive"></p-checkbox>
            <label for="isActive" class="text-sm font-medium text-gray-700">Active</label>
          </div>
        }

        <div class="space-y-2">
          <label class="text-sm font-medium text-gray-700">Features</label>
          <div class="grid grid-cols-2 gap-2 max-h-48 overflow-y-auto border rounded-lg p-3">
            @for (feat of knownFeatures; track feat) {
              <div class="flex items-center gap-2">
                <p-checkbox
                  [ngModel]="form.features.includes(feat)"
                  (ngModelChange)="toggleFeature(feat, $event)"
                  [binary]="true" [inputId]="feat"></p-checkbox>
                <label [for]="feat" class="text-xs text-gray-600">{{ feat }}</label>
              </div>
            }
          </div>
        </div>
      </div>
      <ng-template pTemplate="footer">
        <button pButton label="Cancel" class="p-button-text" (click)="showDialog = false"></button>
        <button pButton [label]="editingId() ? 'Save Changes' : 'Create Plan'"
          [loading]="saving()" (click)="save()"></button>
      </ng-template>
    </p-dialog>
  `,
})
export class PlatformPlansComponent implements OnInit {
  private http = inject(HttpClient);

  loading = signal(true);
  saving = signal(false);
  plans = signal<PlatformSubscriptionPlanDto[]>([]);
  editingId = signal<number | null>(null);
  showDialog = false;
  knownFeatures = KNOWN_FEATURES;

  form: PlanFormModel = this.emptyForm();

  ngOnInit() { this.load(); }

  async load() {
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.http.get<PlatformSubscriptionPlanDto[]>(ApiEndpoints.platform.plans)
      );
      this.plans.set(result);
    } finally {
      this.loading.set(false);
    }
  }

  openCreate() {
    this.editingId.set(null);
    this.form = this.emptyForm();
    this.showDialog = true;
  }

  onAction(event: { action: string; row: PlatformSubscriptionPlanDto }) {
    if (event.action === 'Edit') {
      this.editingId.set(event.row.id);
      this.form = {
        code: event.row.code,
        label: event.row.label,
        monthlyPrice: event.row.monthlyPrice,
        annualPrice: event.row.annualPrice,
        maxUsers: event.row.maxUsers,
        maxProducts: event.row.maxProducts,
        maxInvoicesPerMonth: event.row.maxInvoicesPerMonth,
        storageQuotaMb: event.row.storageMb,
        smsQuotaPerMonth: event.row.smsQuotaPerMonth,
        emailQuotaPerMonth: event.row.emailQuotaPerMonth,
        isActive: event.row.isActive,
        features: [...event.row.features],
      };
      this.showDialog = true;
    }
  }

  toggleFeature(code: string, checked: boolean) {
    if (checked && !this.form.features.includes(code)) {
      this.form.features = [...this.form.features, code];
    } else if (!checked) {
      this.form.features = this.form.features.filter(f => f !== code);
    }
  }

  async save() {
    this.saving.set(true);
    try {
      if (this.editingId()) {
        await firstValueFrom(
          this.http.put(ApiEndpoints.platform.plan(this.editingId()!), this.form)
        );
      } else {
        await firstValueFrom(
          this.http.post(ApiEndpoints.platform.plans, this.form)
        );
      }
      this.showDialog = false;
      await this.load();
    } finally {
      this.saving.set(false);
    }
  }

  private emptyForm(): PlanFormModel {
    return {
      code: '', label: '',
      monthlyPrice: 0, annualPrice: 0,
      maxUsers: 5, maxProducts: 500, maxInvoicesPerMonth: 100,
      storageQuotaMb: 512, smsQuotaPerMonth: 0, emailQuotaPerMonth: 0,
      isActive: true, features: [],
    };
  }
}
