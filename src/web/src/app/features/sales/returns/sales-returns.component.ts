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
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface SalesReturnRow {
  id: number;
  returnNumber: string;
  invoiceId: number;
  customerNameSnapshot: string;
  returnDate: string;
  totalAmount: number;
  status: string;
  reason: string;
}

interface ReturnLine {
  invoiceLineId: number | null;
  quantity: number;
  reason: string;
}

const STATUS_BADGE: Record<string, string> = {
  Pending:  'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-100 text-amber-700',
  Approved: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700',
  Rejected: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-red-100 text-red-600',
  Cancelled:'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-500',
  '*':      'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
};

@Component({
  selector: 'app-sales-returns',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        title="Sales Returns"
        subtitle="Process returns and issue credit notes."
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800 gap-4">
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
              <th style="width: 150px">Return #</th>
              <th style="width: 100px">Invoice ID</th>
              <th>Customer</th>
              <th style="width: 130px">Date</th>
              <th style="width: 130px" class="text-right">Amount</th>
              <th style="width: 110px">Status</th>
              <th style="width: 120px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm font-semibold text-slate-700 dark:text-slate-300">{{ row.returnNumber }}</td>
              <td class="font-mono text-sm text-slate-500">#{{ row.invoiceId }}</td>
              <td class="text-slate-800 dark:text-slate-200">{{ row.customerNameSnapshot }}</td>
              <td class="tabular-nums text-slate-500">{{ row.returnDate | date:'dd MMM yyyy' }}</td>
              <td class="text-right tabular-nums font-semibold text-slate-800 dark:text-slate-200">
                ₹ {{ row.totalAmount | number:'1.2-2' }}
              </td>
              <td><span [class]="statusClass(row.status)">{{ row.status }}</span></td>
              <td class="text-right">
                <div class="flex items-center justify-end gap-0.5">
                  @if (row.status === 'Pending') {
                    <button pButton icon="pi pi-check" class="p-button-sm p-button-text p-button-rounded p-button-success"
                            pTooltip="Approve" tooltipPosition="left"
                            (click)="approve(row)" [disabled]="actionId() === row.id"></button>
                    <button pButton icon="pi pi-times" class="p-button-sm p-button-text p-button-rounded p-button-danger"
                            pTooltip="Cancel" tooltipPosition="left"
                            (click)="cancel(row)" [disabled]="actionId() === row.id"></button>
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
                    <i class="pi pi-arrow-circle-left text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No sales returns</p>
                  <p class="text-xs text-slate-400">Customer return requests will appear here.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New Return dialog -->
    <p-dialog [(visible)]="dialogVisible" header="New Sales Return"
              [modal]="true" [style]="{ width: '560px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field label="Invoice ID" [required]="true">
          <p-inputNumber [(ngModel)]="form.invoiceId" name="invoiceId" [min]="1"
                        styleClass="w-full" inputStyleClass="w-full" />
        </app-form-field>
        <app-form-field label="Return Reason" [required]="true">
          <input pInputText [(ngModel)]="form.reason" name="reason" class="w-full"
                 placeholder="Reason for return" />
        </app-form-field>

        <!-- Return lines -->
        <div class="space-y-2">
          <div class="flex items-center justify-between">
            <span class="text-sm font-semibold text-slate-700 dark:text-slate-300">Return Lines</span>
            <button type="button" (click)="addLine()"
                    class="text-xs text-indigo-600 hover:text-indigo-700 font-medium flex items-center gap-1">
              <i class="pi pi-plus text-[10px]"></i> Add Line
            </button>
          </div>
          <div class="rounded-lg border border-slate-200 dark:border-slate-700 overflow-hidden">
            <table class="w-full text-xs">
              <thead>
                <tr class="bg-slate-50 dark:bg-slate-800">
                  <th class="text-left px-3 py-2 text-slate-500 font-medium">Invoice Line ID</th>
                  <th class="text-right px-3 py-2 text-slate-500 font-medium w-24">Qty</th>
                  <th class="w-8"></th>
                </tr>
              </thead>
              <tbody>
                @for (line of form.lines; track $index) {
                  <tr class="border-t border-slate-100 dark:border-slate-700">
                    <td class="px-2 py-1">
                      <p-inputNumber [(ngModel)]="line.invoiceLineId" [name]="'lineId_' + $index"
                                    [min]="1" styleClass="w-full" inputStyleClass="w-full !h-7 !text-xs" />
                    </td>
                    <td class="px-2 py-1">
                      <p-inputNumber [(ngModel)]="line.quantity" [name]="'qty_' + $index"
                                    [min]="1" styleClass="w-full" inputStyleClass="w-full !h-7 !text-xs text-right" />
                    </td>
                    <td class="px-1">
                      @if (form.lines.length > 1) {
                        <button type="button" (click)="removeLine($index)"
                                class="p-1 text-red-400 hover:text-red-600">
                          <i class="pi pi-times text-[10px]"></i>
                        </button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button label="Create Return" [loading]="saving()"
                  [disabled]="!form.invoiceId || !form.reason"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>
  `
})
export class SalesReturnsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly actionId = signal<number | null>(null);
  protected readonly allRows  = signal<SalesReturnRow[]>([]);

  protected searchQuery   = '';
  protected dialogVisible = false;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.returnNumber.toLowerCase().includes(q) ||
      r.customerNameSnapshot.toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: 'New Return', icon: 'pi pi-plus', severity: 'primary', permission: Permissions.salesReturns.create },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected statusClass(status: string): string {
    return STATUS_BADGE[status] ?? STATUS_BADGE['*'];
  }

  protected onHeaderAction(action: string): void {
    if (action === 'New Return') {
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected addLine(): void {
    this.form.lines.push({ invoiceLineId: null, quantity: 1, reason: '' });
  }

  protected removeLine(i: number): void {
    this.form.lines.splice(i, 1);
  }

  protected async approve(row: SalesReturnRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.salesReturns.approve(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async cancel(row: SalesReturnRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.salesReturns.reject(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.salesReturns.create, {
        invoiceId: this.form.invoiceId,
        reason:    this.form.reason,
        lines:     this.form.lines.filter(l => l.invoiceLineId).map(l => ({
          invoiceLineId: l.invoiceLineId,
          quantity:      l.quantity,
        })),
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<SalesReturnRow[]>(ApiEndpoints.salesReturns.list));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return {
      invoiceId: null as number | null,
      reason: '',
      lines: [{ invoiceLineId: null as number | null, quantity: 1, reason: '' }] as ReturnLine[],
    };
  }
}
