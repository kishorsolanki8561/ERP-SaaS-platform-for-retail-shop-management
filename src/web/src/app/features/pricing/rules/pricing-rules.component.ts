import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, computed, ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { TooltipModule } from 'primeng/tooltip';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AuditLogComponent } from '../../../shared/components/audit-log/audit-log.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface DiscountRuleRow {
  id: number;
  ruleName: string;
  discountPct: number;
  minQuantity?: number;
  validFrom?: string;
  validTo?: string;
  isActive: boolean;
}

@Component({
  selector: 'app-pricing-rules',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, CalendarModule, TooltipModule,
    PageHeaderComponent, FormFieldComponent, AuditLogComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.pricing.rulesTitle"
        [subtitle]="labels.pricing.rulesSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <!-- Discount Rules table -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800 gap-4">
          <h3 class="text-sm font-semibold text-slate-700 dark:text-slate-300">Discount Rules</h3>
          <div class="relative flex-1 max-w-xs">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm pointer-events-none"></i>
            <input pInputText [(ngModel)]="searchQuery" (ngModelChange)="onSearch($event)"
                   [placeholder]="labels.shared.search" class="!pl-9 !h-9 !rounded-lg !text-sm !w-full" />
          </div>
          @if (allRows().length > 0) {
            <span class="text-xs text-slate-400 hidden sm:block">{{ rows().length | number }} records</span>
          }
        </div>

        <p-table [value]="rows()" [loading]="loading()" [paginator]="true" [rows]="20"
                 [rowsPerPageOptions]="[10, 25, 50]" styleClass="p-datatable-sm"
                 [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th>Rule Name</th>
              <th style="width: 120px" class="text-right">Discount %</th>
              <th style="width: 100px" class="text-right">Min Qty</th>
              <th style="width: 130px">Valid From</th>
              <th style="width: 130px">Valid To</th>
              <th style="width: 80px">Active</th>
              <th style="width: 80px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ row.ruleName }}</td>
              <td class="text-right tabular-nums text-slate-700">{{ row.discountPct | number:'1.2-2' }}%</td>
              <td class="text-right tabular-nums text-slate-500">{{ row.minQuantity ?? '—' }}</td>
              <td class="tabular-nums text-slate-500">{{ row.validFrom ? (row.validFrom | date:'dd MMM yyyy') : '—' }}</td>
              <td class="tabular-nums text-slate-500">{{ row.validTo ? (row.validTo | date:'dd MMM yyyy') : '—' }}</td>
              <td>
                @if (row.isActive) {
                  <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-emerald-100">
                    <i class="pi pi-check text-emerald-600" style="font-size: 0.5625rem"></i>
                  </span>
                } @else {
                  <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-slate-100">
                    <i class="pi pi-times text-slate-400" style="font-size: 0.5625rem"></i>
                  </span>
                }
              </td>
              <td class="text-right">
                <button pButton [icon]="row.isActive ? 'pi pi-ban' : 'pi pi-check'"
                        class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        [pTooltip]="row.isActive ? 'Deactivate' : 'Activate'" tooltipPosition="left"
                        (click)="toggle(row)" [disabled]="actionId() === row.id"></button>
                <button pButton icon="pi pi-history" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        pTooltip="Audit Log" tooltipPosition="left" (click)="openAuditLog(row.id)"></button>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="7">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-percentage text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No discount rules</p>
                  <p class="text-xs text-slate-400">Create rules to apply automatic discounts at checkout.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New Discount Rule dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.pricing.newRule"
              [modal]="true" [style]="{ width: '500px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field [label]="labels.pricing.ruleName" [required]="true">
          <input pInputText [(ngModel)]="form.ruleName" name="ruleName"
                 class="w-full" placeholder="e.g. Summer Sale 10%" />
        </app-form-field>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.pricing.discountPct" [required]="true">
            <p-inputNumber [(ngModel)]="form.discountPct" name="discountPct"
                          [min]="0.01" [max]="100" [maxFractionDigits]="2" suffix="%"
                          styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
          <app-form-field [label]="labels.pricing.minQty">
            <p-inputNumber [(ngModel)]="form.minQuantity" name="minQty" [min]="1"
                          styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.pricing.validFrom">
            <p-calendar [(ngModel)]="form.validFrom" name="validFrom" dateFormat="dd/mm/yy"
                        styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
          <app-form-field [label]="labels.pricing.validTo">
            <p-calendar [(ngModel)]="form.validTo" name="validTo" dateFormat="dd/mm/yy"
                        styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
        </div>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.pricing.newRule" [loading]="saving()"
                  [disabled]="!form.ruleName || !form.discountPct"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>

    <app-audit-log #auditPanel entityType="DiscountRule" [entityId]="auditEntityId()" />
  `
})
export class PricingRulesComponent implements OnInit {
  private readonly http = inject(HttpClient);

  @ViewChild('auditPanel') auditPanel!: AuditLogComponent;
  protected readonly auditEntityId = signal<string | number | null>(null);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly actionId = signal<number | null>(null);
  protected readonly allRows  = signal<DiscountRuleRow[]>([]);

  protected searchQuery   = '';
  protected dialogVisible = false;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r => r.ruleName.toLowerCase().includes(q));
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.pricing.newRule, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.pricing.manage },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected openAuditLog(id: number): void {
    this.auditEntityId.set(id);
    this.auditPanel.open();
  }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.pricing.newRule) {
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected async toggle(row: DiscountRuleRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(
        this.http.patch(`${ApiEndpoints.pricing.toggleDiscountRule(row.id)}?isActive=${!row.isActive}`, {})
      );
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.pricing.discountRules, {
        ruleName:    this.form.ruleName,
        discountPct: this.form.discountPct,
        minQuantity: this.form.minQuantity || null,
        validFrom:   this.form.validFrom   || null,
        validTo:     this.form.validTo     || null,
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<DiscountRuleRow[]>(ApiEndpoints.pricing.discountRules));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return {
      ruleName: '', discountPct: 0,
      minQuantity: null as number | null,
      validFrom: null as Date | null,
      validTo:   null as Date | null,
    };
  }
}
