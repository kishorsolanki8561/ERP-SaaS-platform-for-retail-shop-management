import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { TextareaModule } from 'primeng/textarea';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppRoutePaths } from '../../../shared/messages/app-routes';
import { Permissions } from '../../../shared/messages/app-permissions';

interface QuotationRow {
  id: number;
  quotationNumber: string;
  customerId: number;
  customerNameSnapshot: string;
  status: string;
  quotationDate: string;
  validUntil: string;
  grandTotal: number;
}

interface QuotationLineForm {
  productId: number;
  productNameSnapshot: string;
  productUnitId: number;
  unitCodeSnapshot: string;
  conversionFactor: number;
  quantityInBilledUnit: number;
  unitPrice: number;
  discountAmount: number;
  gstRate: number;
}

const STATUS_BADGE: Record<string, string> = {
  Draft:     'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
  Sent:      'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-blue-100 text-blue-700',
  Accepted:  'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700',
  Rejected:  'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-red-100 text-red-600',
  Converted: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-indigo-100 text-indigo-700',
  Expired:   'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-100 text-amber-700',
  '*':       'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
};

@Component({
  selector: 'app-quotations',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, CalendarModule, TextareaModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.sales.quotationsTitle"
        [subtitle]="labels.sales.quotationsSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <!-- Table card -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">

        <!-- Toolbar -->
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800 gap-4">
          <div class="relative flex-1 max-w-xs">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm pointer-events-none"></i>
            <input pInputText [(ngModel)]="searchQuery" (ngModelChange)="onSearch($event)"
                   [placeholder]="labels.shared.search" class="!pl-9 !h-9 !rounded-lg !text-sm !w-full" />
          </div>
          @if (allRows().length > 0) {
            <span class="text-xs text-slate-400 dark:text-slate-500 hidden sm:block">
              {{ rows().length | number }} records
            </span>
          }
        </div>

        <!-- Table -->
        <p-table
          [value]="rows()"
          [loading]="loading()"
          [paginator]="true"
          [rows]="20"
          [rowsPerPageOptions]="[10, 25, 50]"
          styleClass="p-datatable-sm"
          [tableStyle]="{ 'min-width': '100%' }"
        >
          <ng-template pTemplate="header">
            <tr>
              <th style="width: 140px">Quotation #</th>
              <th>Customer</th>
              <th style="width: 130px">Date</th>
              <th style="width: 130px">Valid Until</th>
              <th style="width: 110px">Status</th>
              <th style="width: 130px" class="text-right">Amount</th>
              <th style="width: 160px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm font-semibold text-slate-800 dark:text-slate-200">{{ row.quotationNumber }}</td>
              <td class="text-slate-800 dark:text-slate-200">{{ row.customerNameSnapshot }}</td>
              <td class="tabular-nums text-slate-500">{{ row.quotationDate | date:'dd MMM yyyy' }}</td>
              <td class="tabular-nums text-slate-500">{{ row.validUntil | date:'dd MMM yyyy' }}</td>
              <td><span [class]="statusClass(row.status)">{{ row.status }}</span></td>
              <td class="text-right tabular-nums font-semibold text-slate-800 dark:text-slate-200">
                ₹ {{ row.grandTotal | number:'1.2-2' }}
              </td>
              <td class="text-right">
                <div class="flex items-center justify-end gap-0.5">
                  @if (row.status === 'Draft') {
                    <button pButton icon="pi pi-send" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                            pTooltip="Send" tooltipPosition="left"
                            (click)="sendQuotation(row)" [disabled]="actionId() === row.id"></button>
                    <button pButton icon="pi pi-times" class="p-button-sm p-button-text p-button-rounded p-button-danger"
                            pTooltip="Reject" tooltipPosition="left"
                            (click)="rejectQuotation(row)" [disabled]="actionId() === row.id"></button>
                  }
                  @if (row.status === 'Sent') {
                    <button pButton icon="pi pi-arrow-right-arrow-left" class="p-button-sm p-button-text p-button-rounded p-button-success"
                            pTooltip="Convert to SO" tooltipPosition="left"
                            (click)="convertToSO(row)" [disabled]="actionId() === row.id"></button>
                    <button pButton icon="pi pi-times" class="p-button-sm p-button-text p-button-rounded p-button-danger"
                            pTooltip="Reject" tooltipPosition="left"
                            (click)="rejectQuotation(row)" [disabled]="actionId() === row.id"></button>
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
                    <i class="pi pi-file-edit text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No quotations yet</p>
                  <p class="text-xs text-slate-400 dark:text-slate-600">Create your first quotation to get started.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New Quotation dialog -->
    <p-dialog
      [(visible)]="dialogVisible"
      [header]="labels.sales.newQuotation"
      [modal]="true"
      [style]="{ width: '680px' }"
      [draggable]="false"
    >
      <form class="space-y-4 pt-2">
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.crm.displayName" [required]="true">
            <input pInputText [(ngModel)]="form.customerNameSnapshot" name="customerName" class="w-full"
                   placeholder="Customer name" />
          </app-form-field>
          <app-form-field label="Customer ID" [required]="true">
            <p-inputNumber [(ngModel)]="form.customerId" name="customerId" [min]="1"
                          styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.sales.validUntil" [required]="true">
            <p-calendar [(ngModel)]="form.validUntil" name="validUntil" dateFormat="dd/mm/yy"
                       styleClass="w-full" inputStyleClass="w-full" [showIcon]="true" />
          </app-form-field>
          <app-form-field [label]="labels.common.notes">
            <input pInputText [(ngModel)]="form.notes" name="notes" class="w-full" placeholder="Optional notes" />
          </app-form-field>
        </div>

        <!-- Line items -->
        <div>
          <div class="flex items-center justify-between mb-2">
            <span class="text-sm font-semibold text-slate-700 dark:text-slate-300">Line Items</span>
            <button pButton icon="pi pi-plus" label="Add Line" size="small" severity="secondary"
                    [outlined]="true" type="button" (click)="addLine()"></button>
          </div>
          @for (line of form.lines; track $index; let i = $index) {
            <div class="grid grid-cols-12 gap-2 mb-2 items-end p-3 bg-slate-50 dark:bg-slate-800 rounded-lg">
              <div class="col-span-1">
                <label class="text-xs text-slate-500 mb-1 block">Prod ID</label>
                <p-inputNumber [(ngModel)]="line.productId" [name]="'pid'+i" [min]="1"
                              styleClass="w-full" inputStyleClass="w-full text-xs" />
              </div>
              <div class="col-span-3">
                <label class="text-xs text-slate-500 mb-1 block">Product Name</label>
                <input pInputText [(ngModel)]="line.productNameSnapshot" [name]="'pname'+i"
                       class="w-full text-xs" placeholder="Name" />
              </div>
              <div class="col-span-1">
                <label class="text-xs text-slate-500 mb-1 block">Unit ID</label>
                <p-inputNumber [(ngModel)]="line.productUnitId" [name]="'uid'+i" [min]="1"
                              styleClass="w-full" inputStyleClass="w-full text-xs" />
              </div>
              <div class="col-span-1">
                <label class="text-xs text-slate-500 mb-1 block">Unit</label>
                <input pInputText [(ngModel)]="line.unitCodeSnapshot" [name]="'ucode'+i"
                       class="w-full text-xs" placeholder="PCS" />
              </div>
              <div class="col-span-1">
                <label class="text-xs text-slate-500 mb-1 block">Qty</label>
                <p-inputNumber [(ngModel)]="line.quantityInBilledUnit" [name]="'qty'+i" [min]="0.001"
                              [minFractionDigits]="0" [maxFractionDigits]="3"
                              styleClass="w-full" inputStyleClass="w-full text-xs" />
              </div>
              <div class="col-span-2">
                <label class="text-xs text-slate-500 mb-1 block">Unit Price</label>
                <p-inputNumber [(ngModel)]="line.unitPrice" [name]="'price'+i" [min]="0"
                              mode="decimal" [minFractionDigits]="2" [maxFractionDigits]="2"
                              styleClass="w-full" inputStyleClass="w-full text-xs" />
              </div>
              <div class="col-span-1">
                <label class="text-xs text-slate-500 mb-1 block">Disc</label>
                <p-inputNumber [(ngModel)]="line.discountAmount" [name]="'disc'+i" [min]="0"
                              mode="decimal" [minFractionDigits]="2"
                              styleClass="w-full" inputStyleClass="w-full text-xs" />
              </div>
              <div class="col-span-1">
                <label class="text-xs text-slate-500 mb-1 block">GST%</label>
                <p-inputNumber [(ngModel)]="line.gstRate" [name]="'gst'+i" [min]="0" [max]="28"
                              styleClass="w-full" inputStyleClass="w-full text-xs" />
              </div>
              <div class="col-span-1 flex justify-end">
                <button pButton icon="pi pi-trash" size="small" severity="danger" [text]="true" [rounded]="true"
                        type="button" (click)="removeLine(i)"></button>
              </div>
            </div>
          }
          @if (form.lines.length === 0) {
            <p class="text-xs text-slate-400 text-center py-4">No lines added. Click "Add Line" above.</p>
          }
        </div>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.sales.newQuotation" [loading]="saving()"
                  [disabled]="!canSave()"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>
  `
})
export class QuotationsComponent implements OnInit {
  private readonly http  = inject(HttpClient);
  private readonly toast = inject(MessageService);

  protected readonly labels = AppLabels;
  protected readonly Perms  = Permissions;

  protected readonly loading   = signal(false);
  protected readonly saving    = signal(false);
  protected readonly actionId  = signal<number | null>(null);
  protected readonly allRows   = signal<QuotationRow[]>([]);

  protected searchQuery = '';
  protected dialogVisible = false;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.quotationNumber.toLowerCase().includes(q) ||
      r.customerNameSnapshot.toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.sales.newQuotation, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.quotations.create },
  ];

  protected form = this.emptyForm();

  protected readonly canSave = computed(() =>
    !!this.form.customerId && !!this.form.customerNameSnapshot && this.form.lines.length > 0
  );

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void {
    this.searchQuery = q;
  }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.sales.newQuotation) {
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected statusClass(status: string): string {
    return STATUS_BADGE[status] ?? STATUS_BADGE['*'];
  }

  protected addLine(): void {
    this.form.lines.push({
      productId: 0, productNameSnapshot: '',
      productUnitId: 0, unitCodeSnapshot: 'PCS', conversionFactor: 1,
      quantityInBilledUnit: 1, unitPrice: 0, discountAmount: 0, gstRate: 0,
    });
  }

  protected removeLine(i: number): void {
    this.form.lines.splice(i, 1);
  }

  protected async save(): Promise<void> {
    if (!this.canSave()) return;
    this.saving.set(true);
    try {
      const validUntil = this.form.validUntil instanceof Date
        ? this.form.validUntil.toISOString()
        : new Date(this.form.validUntil || Date.now() + 7 * 86400000).toISOString();
      await firstValueFrom(this.http.post(ApiEndpoints.quotations.create, {
        customerId:           this.form.customerId,
        customerNameSnapshot: this.form.customerNameSnapshot,
        validUntil,
        notes: this.form.notes || null,
        lines: this.form.lines.map(l => ({
          ...l,
          conversionFactor: l.conversionFactor || 1,
        })),
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled by error interceptor */ }
    finally { this.saving.set(false); }
  }

  protected async sendQuotation(row: QuotationRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.patch(ApiEndpoints.quotations.send(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async convertToSO(row: QuotationRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.quotations.convert(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async rejectQuotation(row: QuotationRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.patch(ApiEndpoints.quotations.reject(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<QuotationRow[]>(ApiEndpoints.quotations.list));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return {
      customerId: 0,
      customerNameSnapshot: '',
      validUntil: null as Date | null,
      notes: '',
      lines: [] as QuotationLineForm[],
    };
  }
}
