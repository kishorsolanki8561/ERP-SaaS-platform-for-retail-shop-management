import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface PayrollRow {
  id: number;
  employeeId: number;
  employeeName: string;
  year: number;
  month: number;
  grossSalary: number;
  totalDeductions: number;
  netPayable: number;
  status: string;
}

const STATUS_BADGE: Record<string, string> = {
  Draft:    'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
  Approved: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700',
  Paid:     'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-green-100 text-green-700',
  '*':      'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
};

const MONTHS = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

@Component({
  selector: 'app-payroll',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.hr.payrollTitle"
        [subtitle]="labels.hr.payrollSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <!-- Month/Year filter -->
      <div class="flex items-center gap-3">
        <div class="flex items-center gap-2">
          <label class="text-sm text-slate-600 dark:text-slate-400">Month:</label>
          <select [(ngModel)]="filterMonth" (ngModelChange)="load()"
                  class="text-sm border border-slate-200 dark:border-slate-700 rounded-lg px-3 py-1.5
                         bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200">
            @for (m of monthOptions; track m.value) {
              <option [value]="m.value">{{ m.label }}</option>
            }
          </select>
        </div>
        <div class="flex items-center gap-2">
          <label class="text-sm text-slate-600 dark:text-slate-400">Year:</label>
          <select [(ngModel)]="filterYear" (ngModelChange)="load()"
                  class="text-sm border border-slate-200 dark:border-slate-700 rounded-lg px-3 py-1.5
                         bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200">
            @for (y of years; track y) {
              <option [value]="y">{{ y }}</option>
            }
          </select>
        </div>
        <p-button icon="pi pi-refresh" size="small" severity="secondary" [outlined]="true"
                  label="Refresh" (onClick)="load()" />
      </div>

      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <p-table [value]="rows()" [loading]="loading()" [paginator]="true" [rows]="20"
                 [rowsPerPageOptions]="[10, 25, 50]" styleClass="p-datatable-sm"
                 [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th>Employee</th>
              <th style="width: 100px">Period</th>
              <th style="width: 130px" class="text-right">Gross</th>
              <th style="width: 130px" class="text-right">Deductions</th>
              <th style="width: 130px" class="text-right">Net Payable</th>
              <th style="width: 100px">Status</th>
              <th style="width: 140px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ row.employeeName }}</td>
              <td class="text-slate-600">{{ monthName(row.month) }} {{ row.year }}</td>
              <td class="text-right tabular-nums text-slate-700">₹ {{ row.grossSalary | number:'1.2-2' }}</td>
              <td class="text-right tabular-nums text-red-600">₹ {{ row.totalDeductions | number:'1.2-2' }}</td>
              <td class="text-right tabular-nums font-semibold text-slate-800 dark:text-slate-200">₹ {{ row.netPayable | number:'1.2-2' }}</td>
              <td><span [class]="statusClass(row.status)">{{ row.status }}</span></td>
              <td class="text-right">
                <div class="flex items-center justify-end gap-0.5">
                  @if (row.status === 'Draft') {
                    <button pButton icon="pi pi-check" class="p-button-sm p-button-text p-button-rounded p-button-success"
                            pTooltip="Approve" tooltipPosition="left"
                            (click)="approve(row)" [disabled]="actionId() === row.id"></button>
                  }
                  @if (row.status === 'Approved') {
                    <button pButton icon="pi pi-wallet" class="p-button-sm p-button-text p-button-rounded p-button-info"
                            pTooltip="Pay" tooltipPosition="left"
                            (click)="pay(row)" [disabled]="actionId() === row.id"></button>
                  }
                </div>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="7">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-chart-bar text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No payroll for this period</p>
                  <p class="text-xs text-slate-400">Generate payroll using the button above.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- Generate payroll confirm dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.hr.generatePayroll"
              [modal]="true" [style]="{ width: '400px' }" [draggable]="false">
      <div class="pt-2 pb-4 space-y-3">
        <p class="text-sm text-slate-600 dark:text-slate-400">
          Generate payroll for <strong>{{ monthName(filterMonth) }} {{ filterYear }}</strong> for all active employees?
        </p>
        <p class="text-xs text-amber-700 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20 px-3 py-2 rounded-lg">
          This will calculate salaries based on attendance records for the selected period.
        </p>
      </div>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.hr.generatePayroll" [loading]="saving()" (onClick)="generate()" />
      </ng-template>
    </p-dialog>
  `
})
export class PayrollComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly actionId = signal<number | null>(null);
  protected readonly allRows  = signal<PayrollRow[]>([]);

  protected dialogVisible = false;
  protected filterMonth = new Date().getMonth() + 1;
  protected filterYear  = new Date().getFullYear();

  protected readonly monthOptions = [
    { value: 1, label: 'January' }, { value: 2, label: 'February' },
    { value: 3, label: 'March' }, { value: 4, label: 'April' },
    { value: 5, label: 'May' }, { value: 6, label: 'June' },
    { value: 7, label: 'July' }, { value: 8, label: 'August' },
    { value: 9, label: 'September' }, { value: 10, label: 'October' },
    { value: 11, label: 'November' }, { value: 12, label: 'December' },
  ];

  protected readonly years = Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - i);

  protected readonly rows = computed(() => this.allRows());

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.hr.generatePayroll, icon: 'pi pi-cog', severity: 'primary', permission: Permissions.hr.payroll },
  ];

  ngOnInit(): void { this.load(); }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.hr.generatePayroll) this.dialogVisible = true;
  }

  protected monthName(m: number): string { return MONTHS[m - 1] ?? ''; }

  protected statusClass(status: string): string {
    return STATUS_BADGE[status] ?? STATUS_BADGE['*'];
  }

  protected async generate(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.hr.generatePayroll, {
        year: this.filterYear, month: this.filterMonth,
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  protected async approve(row: PayrollRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.hr.approvePayroll(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async pay(row: PayrollRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.hr.payPayroll(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(
        this.http.get<PayrollRow[]>(ApiEndpoints.hr.payroll, {
          params: { year: String(this.filterYear), month: String(this.filterMonth) }
        })
      );
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }
}
