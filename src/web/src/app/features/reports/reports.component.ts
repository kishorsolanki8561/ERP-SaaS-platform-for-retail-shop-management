import {
  ChangeDetectionStrategy, Component,
  inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CalendarModule } from 'primeng/calendar';
import { TableModule } from 'primeng/table';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../shared/messages/app-messages';
import { ApiEndpoints } from '../../shared/messages/app-api';

interface ReportType {
  key: string;
  label: string;
  endpoint: string;
  needsDates: boolean;
}

const REPORT_TYPES: ReportType[] = [
  { key: 'TrialBalance',  label: AppLabels.reports.trialBalance, endpoint: ApiEndpoints.reports.trialBalance,  needsDates: false },
  { key: 'ProfitLoss',    label: AppLabels.reports.profitLoss,   endpoint: ApiEndpoints.reports.profitLoss,    needsDates: true  },
  { key: 'BalanceSheet',  label: AppLabels.reports.balanceSheet, endpoint: ApiEndpoints.reports.balanceSheet,  needsDates: false },
  { key: 'DayBook',       label: AppLabels.reports.dayBook,      endpoint: ApiEndpoints.reports.dayBook,       needsDates: true  },
  { key: 'CashBook',      label: AppLabels.reports.cashBook,     endpoint: ApiEndpoints.reports.cashBook,      needsDates: true  },
  { key: 'GSTR1B2B',      label: AppLabels.reports.gstr1,        endpoint: ApiEndpoints.reports.gstr1B2b,      needsDates: true  },
  { key: 'GSTR3B',                  label: AppLabels.reports.gstr3b,                   endpoint: ApiEndpoints.reports.gstr3b,                    needsDates: true  },
  { key: 'PaymentSummary',          label: AppLabels.reports.paymentSummary,           endpoint: ApiEndpoints.reports.paymentSummary,            needsDates: true  },
  { key: 'FailedPayments',          label: AppLabels.reports.failedPayments,           endpoint: ApiEndpoints.reports.failedPayments,            needsDates: true  },
  { key: 'SettlementGap',           label: AppLabels.reports.settlementGap,            endpoint: ApiEndpoints.reports.settlementGap,             needsDates: true  },
  { key: 'ReconciliationExceptions',label: AppLabels.reports.reconciliationExceptions, endpoint: ApiEndpoints.reports.reconciliationExceptions,  needsDates: true  },
];

@Component({
  selector: 'app-reports',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    ButtonModule, CalendarModule, TableModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.reports.title"
        [subtitle]="labels.reports.subtitle"
        [actions]="[]"
        (actionClick)="noop()"
      />

      <!-- Report selector + filters -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 shadow-sm">
        <div class="flex flex-wrap items-end gap-4">
          <!-- Report type tabs -->
          <div class="flex flex-wrap gap-2 flex-1">
            @for (r of reportTypes; track r.key) {
              <button
                (click)="selectReport(r)"
                [class]="r.key === selectedReport?.key
                  ? 'px-3 py-1.5 rounded-lg text-sm font-semibold bg-indigo-600 text-white shadow-sm'
                  : 'px-3 py-1.5 rounded-lg text-sm font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'">
                {{ r.label }}
              </button>
            }
          </div>

          <!-- Date filters (only when needed) -->
          @if (selectedReport?.needsDates) {
            <div class="flex items-end gap-3">
              <div class="w-36">
                <label class="block text-xs text-slate-500 mb-1">{{ labels.reports.fromDate }}</label>
                <p-calendar [(ngModel)]="fromDate" dateFormat="dd/mm/yy"
                            styleClass="w-full" inputStyleClass="w-full !h-9 !text-sm" />
              </div>
              <div class="w-36">
                <label class="block text-xs text-slate-500 mb-1">{{ labels.reports.toDate }}</label>
                <p-calendar [(ngModel)]="toDate" dateFormat="dd/mm/yy"
                            styleClass="w-full" inputStyleClass="w-full !h-9 !text-sm" />
              </div>
            </div>
          }

          <p-button [label]="labels.reports.runReport" icon="pi pi-play" [loading]="loading()"
                    [disabled]="!selectedReport"
                    (onClick)="runReport()" />
        </div>
      </div>

      <!-- Results table -->
      @if (columns().length > 0) {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
          <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800">
            <span class="text-sm font-semibold text-slate-700 dark:text-slate-300">
              {{ selectedReport?.label }}
              @if (fromDate && selectedReport?.needsDates) {
                <span class="font-normal text-slate-400 ml-2 text-xs">
                  {{ fromDate | date:'dd MMM yyyy' }} – {{ toDate | date:'dd MMM yyyy' }}
                </span>
              }
            </span>
            <div class="flex items-center gap-2">
              <p-button [label]="labels.reports.exportExcel" icon="pi pi-file-excel"
                        size="small" severity="secondary" [outlined]="true"
                        (onClick)="exportReport('Excel')" />
            </div>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-slate-50 dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700">
                  @for (col of columns(); track col) {
                    <th class="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide
                               first:pl-5 last:pr-5"
                        [class.text-right]="isNumericColumn(col)">
                      {{ col }}
                    </th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (row of reportData(); track $index) {
                  <tr class="border-b border-slate-100 dark:border-slate-800 hover:bg-slate-50/50 dark:hover:bg-slate-800/50">
                    @for (col of columns(); track col) {
                      <td class="px-4 py-2.5 text-slate-700 dark:text-slate-300 first:pl-5 last:pr-5"
                          [class.text-right]="isNumericColumn(col)"
                          [class.tabular-nums]="isNumericColumn(col)"
                          [class.font-semibold]="col === 'AccountName' || col === 'account'">
                        {{ formatCell(row[col]) }}
                      </td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>

          @if (reportData().length === 0 && !loading()) {
            <div class="flex flex-col items-center justify-center py-16 gap-3">
              <i class="pi pi-chart-bar text-3xl text-slate-300 dark:text-slate-600"></i>
              <p class="text-sm text-slate-400">No data for the selected period.</p>
            </div>
          }
        </div>
      } @else if (!loading() && ranOnce()) {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-12 shadow-sm text-center">
          <i class="pi pi-chart-bar text-4xl text-slate-300 dark:text-slate-600 block mb-3"></i>
          <p class="text-sm text-slate-500">No data returned for this report.</p>
        </div>
      } @else if (!ranOnce()) {
        <div class="bg-slate-50 dark:bg-slate-800/50 rounded-2xl border border-dashed border-slate-300 dark:border-slate-700 p-12 text-center">
          <i class="pi pi-chart-bar text-4xl text-slate-300 dark:text-slate-600 block mb-3"></i>
          <p class="text-sm text-slate-400">Select a report type and click Run.</p>
        </div>
      }
    </div>
  `
})
export class ReportsComponent {
  private readonly http = inject(HttpClient);

  protected readonly labels     = AppLabels;
  protected readonly loading    = signal(false);
  protected readonly columns    = signal<string[]>([]);
  protected readonly reportData = signal<Record<string, unknown>[]>([]);
  protected readonly ranOnce    = signal(false);

  protected readonly reportTypes = REPORT_TYPES;
  protected selectedReport: ReportType | null = REPORT_TYPES[0];
  protected fromDate: Date | null = null;
  protected toDate: Date | null   = null;

  private readonly numericHints = ['amount', 'balance', 'debit', 'credit', 'tax', 'total', 'net', 'gross', 'taxable'];

  protected noop(): void {}

  protected selectReport(r: ReportType): void {
    this.selectedReport = r;
    this.columns.set([]);
    this.reportData.set([]);
    this.ranOnce.set(false);
  }

  protected isNumericColumn(col: string): boolean {
    return this.numericHints.some(h => col.toLowerCase().includes(h));
  }

  protected formatCell(value: unknown): string {
    if (value == null) return '—';
    if (typeof value === 'number') return value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    if (typeof value === 'string' && value.match(/^\d{4}-\d{2}-\d{2}/)) {
      return new Date(value).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
    }
    return String(value);
  }

  protected async runReport(): Promise<void> {
    if (!this.selectedReport) return;
    this.loading.set(true);
    this.ranOnce.set(true);
    try {
      const params: Record<string, string> = {};
      if (this.selectedReport.needsDates) {
        if (this.fromDate) params['fromDate'] = this.fromDate.toISOString().split('T')[0];
        if (this.toDate)   params['toDate']   = this.toDate.toISOString().split('T')[0];
      }
      const qs = new URLSearchParams(params).toString();
      const url = qs ? `${this.selectedReport.endpoint}?${qs}` : this.selectedReport.endpoint;
      const data = await firstValueFrom(this.http.get<Record<string, unknown>[]>(url));
      const rows = data ?? [];
      this.reportData.set(rows);
      this.columns.set(rows.length > 0 ? Object.keys(rows[0]) : []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  protected async exportReport(format: string): Promise<void> {
    if (!this.selectedReport) return;
    try {
      const params: Record<string, string> = { format };
      if (this.selectedReport.needsDates) {
        if (this.fromDate) params['fromDate'] = this.fromDate.toISOString().split('T')[0];
        if (this.toDate)   params['toDate']   = this.toDate.toISOString().split('T')[0];
      }
      const qs = new URLSearchParams(params).toString();
      const url = `${ApiEndpoints.reports.export(this.selectedReport.key)}?${qs}`;
      await firstValueFrom(this.http.get(url, { responseType: 'blob' }));
    } catch { /* handled */ }
  }
}
