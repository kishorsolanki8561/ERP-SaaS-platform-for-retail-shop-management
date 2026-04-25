import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, computed
} from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';

interface InvoiceDetail {
  id: number;
  invoiceNumber: string;
  invoiceDate: string;
  customerId: number;
  customerName: string;
  status: string;
  subTotal: number;
  totalDiscount: number;
  totalTaxAmount: number;
  grandTotal: number;
  lines: InvoiceLine[];
}

interface InvoiceLine {
  id: number;
  productId: number;
  productName: string;
  unitCode: string;
  qty: number;
  unitPrice: number;
  discountPercent: number;
  taxableAmount: number;
  gstRate: number;
  lineTotal: number;
}

interface AddLineForm {
  productId: number;
  productUnitId: number;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
}

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule, RouterModule, CurrencyPipe,
    TableModule, ButtonModule, TagModule, DialogModule,
    InputTextModule, InputNumberModule,
    PageHeaderComponent, FormFieldComponent, HasPermissionDirective,
  ],
  template: `
    @if (invoice()) {
      <app-page-header
        [title]="invoice()!.invoiceNumber"
        [subtitle]="invoice()!.customerName + ' · ' + (invoice()!.invoiceDate | date:'dd MMM yyyy')"
      />

      <!-- Status + actions bar -->
      <div class="flex items-center gap-3 mb-6">
        <span [class]="statusClass()">{{ invoice()!.status }}</span>

        @if (invoice()!.status === 'Draft') {
          <ng-container *hasPermission="permissions.billing.edit">
            <p-button label="Add Line" icon="pi pi-plus" severity="secondary"
                      size="small" (onClick)="addLineVisible = true" />
            <p-button [label]="labels.billing.finalize" icon="pi pi-check"
                      size="small" [loading]="acting()" (onClick)="finalize()" />
          </ng-container>
          <ng-container *hasPermission="permissions.billing.cancel">
            <p-button [label]="labels.billing.cancelInvoice" icon="pi pi-times"
                      severity="danger" size="small" [outlined]="true"
                      [loading]="acting()" (onClick)="cancelVisible = true" />
          </ng-container>
        }
      </div>

      <!-- Invoice lines table -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden mb-6">
        <p-table [value]="invoice()!.lines" styleClass="p-datatable-sm">
          <ng-template pTemplate="header">
            <tr>
              <th>Product</th>
              <th class="w-20 text-right">Qty</th>
              <th class="w-24 text-right">Unit Price</th>
              <th class="w-20 text-right">Disc %</th>
              <th class="w-24 text-right">Taxable</th>
              <th class="w-20 text-right">GST %</th>
              <th class="w-28 text-right">Total</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-line>
            <tr>
              <td>{{ line.productName }} <span class="text-xs text-slate-400 ml-1">{{ line.unitCode }}</span></td>
              <td class="text-right">{{ line.qty }}</td>
              <td class="text-right">{{ line.unitPrice | currency:'INR':'symbol':'1.2-2' }}</td>
              <td class="text-right">{{ line.discountPercent }}%</td>
              <td class="text-right">{{ line.taxableAmount | currency:'INR':'symbol':'1.2-2' }}</td>
              <td class="text-right">{{ line.gstRate }}%</td>
              <td class="text-right font-medium">{{ line.lineTotal | currency:'INR':'symbol':'1.2-2' }}</td>
            </tr>
          </ng-template>
          <ng-template pTemplate="emptymessage">
            <tr><td colspan="7" class="text-center py-8 text-slate-400">No lines added yet.</td></tr>
          </ng-template>
        </p-table>
      </div>

      <!-- Totals -->
      <div class="flex justify-end">
        <div class="w-72 space-y-2 text-sm">
          <div class="flex justify-between text-slate-500">
            <span>Subtotal</span>
            <span>{{ invoice()!.subTotal | currency:'INR':'symbol':'1.2-2' }}</span>
          </div>
          <div class="flex justify-between text-slate-500">
            <span>Discount</span>
            <span class="text-red-500">-{{ invoice()!.totalDiscount | currency:'INR':'symbol':'1.2-2' }}</span>
          </div>
          <div class="flex justify-between text-slate-500">
            <span>Tax (GST)</span>
            <span>{{ invoice()!.totalTaxAmount | currency:'INR':'symbol':'1.2-2' }}</span>
          </div>
          <div class="flex justify-between font-bold text-base border-t border-slate-200 dark:border-slate-700 pt-2 mt-2">
            <span>Grand Total</span>
            <span>{{ invoice()!.grandTotal | currency:'INR':'symbol':'1.2-2' }}</span>
          </div>
        </div>
      </div>
    } @else if (loadError()) {
      <div class="text-center py-20 text-slate-400">Invoice not found.</div>
    } @else {
      <div class="flex justify-center py-20">
        <i class="pi pi-spin pi-spinner text-2xl text-primary-500"></i>
      </div>
    }

    <!-- Add Line dialog -->
    <p-dialog [(visible)]="addLineVisible" header="Add Line" [modal]="true"
              [style]="{ width: '480px' }">
      <form class="space-y-4 pt-2">
        <app-form-field label="Product ID" [required]="true">
          <input pInputText [(ngModel)]="lineForm.productId" name="productId"
                 type="number" class="w-full" />
        </app-form-field>
        <app-form-field label="Product Unit ID" [required]="true">
          <input pInputText [(ngModel)]="lineForm.productUnitId" name="productUnitId"
                 type="number" class="w-full" />
        </app-form-field>
        <div class="grid grid-cols-3 gap-3">
          <app-form-field label="Qty" [required]="true">
            <p-inputNumber [(ngModel)]="lineForm.quantity" name="quantity"
                           [minFractionDigits]="0" [min]="0.01" styleClass="w-full" />
          </app-form-field>
          <app-form-field label="Unit Price" [required]="true">
            <p-inputNumber [(ngModel)]="lineForm.unitPrice" name="unitPrice"
                           mode="decimal" [minFractionDigits]="2" [min]="0"
                           styleClass="w-full" />
          </app-form-field>
          <app-form-field label="Disc %">
            <p-inputNumber [(ngModel)]="lineForm.discountPercent" name="discountPercent"
                           [minFractionDigits]="0" [min]="0" [max]="100" suffix=" %"
                           styleClass="w-full" />
          </app-form-field>
        </div>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary"
                  [outlined]="true" (onClick)="addLineVisible = false" />
        <p-button [label]="labels.billing.addLine" [loading]="acting()"
                  (onClick)="addLine()" />
      </ng-template>
    </p-dialog>

    <!-- Cancel dialog -->
    <p-dialog [(visible)]="cancelVisible" [header]="labels.billing.cancelInvoice"
              [modal]="true" [style]="{ width: '420px' }">
      <app-form-field [label]="labels.billing.cancelReason" [required]="true">
        <input pInputText [(ngModel)]="cancelReason" name="cancelReason" class="w-full" />
      </app-form-field>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary"
                  [outlined]="true" (onClick)="cancelVisible = false" />
        <p-button [label]="labels.billing.cancelInvoice" severity="danger"
                  [loading]="acting()" (onClick)="cancelInvoice()" />
      </ng-template>
    </p-dialog>
  `
})
export class InvoiceDetailComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(MessageService);

  protected readonly labels = AppLabels;
  protected readonly permissions = Permissions;

  protected readonly invoice = signal<InvoiceDetail | null>(null);
  protected readonly loadError = signal(false);
  protected readonly acting = signal(false);

  protected addLineVisible = false;
  protected cancelVisible = false;
  protected cancelReason = '';
  protected lineForm: AddLineForm = this.emptyLine();

  protected readonly statusClass = computed(() => {
    const s = this.invoice()?.status;
    const base = 'px-3 py-1 rounded-full text-xs font-semibold ';
    if (s === 'Draft')     return base + 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300';
    if (s === 'Finalized') return base + 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400';
    if (s === 'Cancelled') return base + 'bg-red-100 text-red-600 dark:bg-red-950/40 dark:text-red-400';
    return base + 'bg-indigo-100 text-indigo-600';
  });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.loadError.set(true); return; }
    try {
      const data = await firstValueFrom(
        this.http.get<InvoiceDetail>(ApiEndpoints.billing.invoice(id))
      );
      this.invoice.set(data);
    } catch { this.loadError.set(true); }
  }

  protected async finalize(): Promise<void> {
    const id = this.invoice()?.id;
    if (!id) return;
    this.acting.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.billing.finalize(id), {}));
      this.invoice.update(i => i ? { ...i, status: 'Finalized' } : i);
      this.toast.add({ severity: 'success', summary: 'Finalized', detail: 'Invoice finalized.' });
    } catch { /* handled */ }
    finally { this.acting.set(false); }
  }

  protected async addLine(): Promise<void> {
    const id = this.invoice()?.id;
    if (!id || !this.lineForm.productId || !this.lineForm.quantity || !this.lineForm.unitPrice) return;
    this.acting.set(true);
    try {
      await firstValueFrom(
        this.http.post(ApiEndpoints.billing.invoiceLines(id), this.lineForm)
      );
      // Reload full invoice to get updated totals
      const updated = await firstValueFrom(
        this.http.get<InvoiceDetail>(ApiEndpoints.billing.invoice(id))
      );
      this.invoice.set(updated);
      this.addLineVisible = false;
      this.lineForm = this.emptyLine();
    } catch { /* handled */ }
    finally { this.acting.set(false); }
  }

  protected async cancelInvoice(): Promise<void> {
    const id = this.invoice()?.id;
    if (!id || !this.cancelReason) return;
    this.acting.set(true);
    try {
      await firstValueFrom(
        this.http.post(ApiEndpoints.billing.cancel(id), { reason: this.cancelReason })
      );
      this.invoice.update(i => i ? { ...i, status: 'Cancelled' } : i);
      this.cancelVisible = false;
      this.toast.add({ severity: 'info', summary: 'Cancelled', detail: 'Invoice cancelled.' });
    } catch { /* handled */ }
    finally { this.acting.set(false); }
  }

  private emptyLine(): AddLineForm {
    return { productId: 0, productUnitId: 0, quantity: 1, unitPrice: 0, discountPercent: 0 };
  }
}
