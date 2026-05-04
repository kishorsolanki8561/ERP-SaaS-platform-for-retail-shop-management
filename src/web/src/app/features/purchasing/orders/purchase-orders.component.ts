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
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AuditLogComponent } from '../../../shared/components/audit-log/audit-log.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface PurchaseOrderRow {
  id: number;
  poNumber: string;
  supplierId: number;
  supplierNameSnapshot: string;
  status: string;
  orderDate: string;
  expectedDeliveryDate?: string;
  grandTotal: number;
}

interface POLineForm {
  productId: number;
  productNameSnapshot: string;
  productUnitId: number;
  unitCodeSnapshot: string;
  conversionFactor: number;
  quantityOrdered: number;
  unitPrice: number;
  discountAmount: number;
  gstRate: number;
}

const STATUS_BADGE: Record<string, string> = {
  Draft:      'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
  Sent:       'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-blue-100 text-blue-700',
  PartiallyReceived: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-100 text-amber-700',
  Received:   'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700',
  Cancelled:  'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-red-100 text-red-600',
  '*':        'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
};

@Component({
  selector: 'app-purchase-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, CalendarModule,
    PageHeaderComponent, FormFieldComponent, AuditLogComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.purchasing.ordersTitle"
        [subtitle]="labels.purchasing.ordersSubtitle"
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
              <th style="width: 130px">PO #</th>
              <th>Supplier</th>
              <th style="width: 130px">Order Date</th>
              <th style="width: 110px">Status</th>
              <th style="width: 130px" class="text-right">Amount</th>
              <th style="width: 120px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm font-semibold text-slate-800 dark:text-slate-200">{{ row.poNumber }}</td>
              <td class="text-slate-800 dark:text-slate-200">{{ row.supplierNameSnapshot }}</td>
              <td class="tabular-nums text-slate-500">{{ row.orderDate | date:'dd MMM yyyy' }}</td>
              <td><span [class]="statusClass(row.status)">{{ row.status }}</span></td>
              <td class="text-right tabular-nums font-semibold text-slate-800 dark:text-slate-200">
                ₹ {{ row.grandTotal | number:'1.2-2' }}
              </td>
              <td class="text-right">
                <div class="flex items-center justify-end gap-0.5">
                  @if (row.status === 'Draft') {
                    <button pButton icon="pi pi-send" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                            pTooltip="Send PO" tooltipPosition="left"
                            (click)="sendPO(row)" [disabled]="actionId() === row.id"></button>
                    <button pButton icon="pi pi-times" class="p-button-sm p-button-text p-button-rounded p-button-danger"
                            pTooltip="Cancel" tooltipPosition="left"
                            (click)="cancelPO(row)" [disabled]="actionId() === row.id"></button>
                  }
                  <button pButton icon="pi pi-history" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                          pTooltip="Audit Log" tooltipPosition="left" (click)="openAuditLog(row.id)"></button>
                </div>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="6">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-shopping-cart text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No purchase orders yet</p>
                  <p class="text-xs text-slate-400 dark:text-slate-600">Create a purchase order to replenish inventory.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New PO dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.purchasing.newPurchaseOrder"
              [modal]="true" [style]="{ width: '680px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <div class="grid grid-cols-2 gap-4">
          <app-form-field label="Supplier ID" [required]="true">
            <p-inputNumber [(ngModel)]="form.supplierId" name="supplierId" [min]="1"
                          styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
          <app-form-field label="Supplier Name" [required]="true">
            <input pInputText [(ngModel)]="form.supplierNameSnapshot" name="supplierName"
                   class="w-full" placeholder="Supplier name" />
          </app-form-field>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.purchasing.expectedDate">
            <p-calendar [(ngModel)]="form.expectedDeliveryDate" name="expectedDate" dateFormat="dd/mm/yy"
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
                <label class="text-xs text-slate-500 mb-1 block">Product</label>
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
                <p-inputNumber [(ngModel)]="line.quantityOrdered" [name]="'qty'+i" [min]="0.001"
                              [minFractionDigits]="0" [maxFractionDigits]="3"
                              styleClass="w-full" inputStyleClass="w-full text-xs" />
              </div>
              <div class="col-span-2">
                <label class="text-xs text-slate-500 mb-1 block">Price</label>
                <p-inputNumber [(ngModel)]="line.unitPrice" [name]="'price'+i" [min]="0"
                              mode="decimal" [minFractionDigits]="2"
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
            <p class="text-xs text-slate-400 text-center py-4">No lines. Click "Add Line" to begin.</p>
          }
        </div>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.purchasing.newPurchaseOrder" [loading]="saving()"
                  [disabled]="!form.supplierId || !form.supplierNameSnapshot || form.lines.length === 0"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>

    <app-audit-log #auditPanel entityType="PurchaseOrder" [entityId]="auditEntityId()" />
  `
})
export class PurchaseOrdersComponent implements OnInit {
  private readonly http = inject(HttpClient);

  @ViewChild('auditPanel') auditPanel!: AuditLogComponent;
  protected readonly auditEntityId = signal<string | number | null>(null);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly actionId = signal<number | null>(null);
  protected readonly allRows  = signal<PurchaseOrderRow[]>([]);

  protected searchQuery   = '';
  protected dialogVisible = false;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.poNumber.toLowerCase().includes(q) ||
      r.supplierNameSnapshot.toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.purchasing.newPurchaseOrder, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.purchasing.createPurchaseOrder },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected openAuditLog(id: number): void {
    this.auditEntityId.set(id);
    this.auditPanel.open();
  }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.purchasing.newPurchaseOrder) {
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
      quantityOrdered: 1, unitPrice: 0, discountAmount: 0, gstRate: 0,
    });
  }

  protected removeLine(i: number): void { this.form.lines.splice(i, 1); }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.purchasing.purchaseOrders, {
        supplierId:           this.form.supplierId,
        supplierNameSnapshot: this.form.supplierNameSnapshot,
        expectedDeliveryDate: this.form.expectedDeliveryDate?.toISOString() ?? null,
        notes:                this.form.notes || null,
        lines:                this.form.lines.map(l => ({ ...l, conversionFactor: l.conversionFactor || 1 })),
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  protected async sendPO(row: PurchaseOrderRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.purchasing.sendPO(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async cancelPO(row: PurchaseOrderRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.purchasing.cancelPO(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      // PurchasingController has no dedicated ListPurchaseOrders endpoint — using supplier-scoped view
      // For now load an empty list; controller only has create/send/cancel endpoints
      this.allRows.set([]);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return {
      supplierId: 0,
      supplierNameSnapshot: '',
      expectedDeliveryDate: null as Date | null,
      notes: '',
      lines: [] as POLineForm[],
    };
  }
}
