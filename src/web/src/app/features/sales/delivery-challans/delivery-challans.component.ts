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
import { CalendarModule } from 'primeng/calendar';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface DeliveryChallanRow {
  id: number;
  dcNumber: string;
  salesOrderId: number;
  status: string;
  challanDate: string;
}

const STATUS_BADGE: Record<string, string> = {
  Draft:      'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
  Dispatched: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-indigo-100 text-indigo-700',
  Delivered:  'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700',
  '*':        'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
};

@Component({
  selector: 'app-delivery-challans',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, CalendarModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.sales.deliveryChallansTitle"
        [subtitle]="labels.sales.deliveryChallansSubtitle"
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
              <th style="width: 140px">DC #</th>
              <th style="width: 130px">Sales Order ID</th>
              <th style="width: 130px">Date</th>
              <th style="width: 110px">Status</th>
              <th style="width: 140px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm font-semibold text-slate-800 dark:text-slate-200">{{ row.dcNumber }}</td>
              <td class="tabular-nums text-slate-600">SO-{{ row.salesOrderId }}</td>
              <td class="tabular-nums text-slate-500">{{ row.challanDate | date:'dd MMM yyyy' }}</td>
              <td><span [class]="statusClass(row.status)">{{ row.status }}</span></td>
              <td class="text-right">
                <div class="flex items-center justify-end gap-0.5">
                  @if (row.status === 'Draft') {
                    <button pButton icon="pi pi-truck" class="p-button-sm p-button-text p-button-rounded p-button-info"
                            pTooltip="Dispatch" tooltipPosition="left"
                            (click)="dispatch(row)" [disabled]="actionId() === row.id"></button>
                  }
                  @if (row.status === 'Dispatched') {
                    <button pButton icon="pi pi-check-circle" class="p-button-sm p-button-text p-button-rounded p-button-success"
                            pTooltip="Mark Delivered" tooltipPosition="left"
                            (click)="markDelivered(row)" [disabled]="actionId() === row.id"></button>
                  }
                </div>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="5">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-truck text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No delivery challans yet</p>
                  <p class="text-xs text-slate-400 dark:text-slate-600">Create challans from confirmed sales orders.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New DC dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.sales.newDeliveryChallan"
              [modal]="true" [style]="{ width: '560px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <div class="grid grid-cols-2 gap-4">
          <app-form-field label="Sales Order ID" [required]="true">
            <p-inputNumber [(ngModel)]="form.salesOrderId" name="salesOrderId" [min]="1"
                          styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
          <app-form-field [label]="labels.sales.challanDate" [required]="true">
            <p-calendar [(ngModel)]="form.challanDate" name="challanDate" dateFormat="dd/mm/yy"
                       styleClass="w-full" inputStyleClass="w-full" [showIcon]="true" />
          </app-form-field>
        </div>
        <app-form-field [label]="labels.sales.deliveryAddress">
          <input pInputText [(ngModel)]="form.deliveryAddress" name="deliveryAddress"
                 class="w-full" placeholder="Delivery address" />
        </app-form-field>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.sales.transporterName">
            <input pInputText [(ngModel)]="form.transporterName" name="transporterName"
                   class="w-full" placeholder="Transporter name" />
          </app-form-field>
          <app-form-field [label]="labels.sales.vehicleNumber">
            <input pInputText [(ngModel)]="form.vehicleNumber" name="vehicleNumber"
                   class="w-full" placeholder="Vehicle number" />
          </app-form-field>
        </div>
        <app-form-field [label]="labels.common.notes">
          <input pInputText [(ngModel)]="form.notes" name="notes" class="w-full" placeholder="Optional notes" />
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.sales.newDeliveryChallan" [loading]="saving()"
                  [disabled]="!form.salesOrderId || !form.challanDate"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>
  `
})
export class DeliveryChallansComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly actionId = signal<number | null>(null);
  protected readonly allRows  = signal<DeliveryChallanRow[]>([]);

  protected searchQuery   = '';
  protected dialogVisible = false;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r => r.dcNumber.toLowerCase().includes(q));
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.sales.newDeliveryChallan, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.quotations.create },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.sales.newDeliveryChallan) {
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected statusClass(status: string): string {
    return STATUS_BADGE[status] ?? STATUS_BADGE['*'];
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.quotations.deliveryChallans, {
        salesOrderId:    this.form.salesOrderId,
        challanDate:     this.form.challanDate?.toISOString() ?? new Date().toISOString(),
        deliveryAddress: this.form.deliveryAddress || null,
        transporterName: this.form.transporterName || null,
        vehicleNumber:   this.form.vehicleNumber || null,
        notes:           this.form.notes || null,
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  protected async dispatch(row: DeliveryChallanRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.patch(ApiEndpoints.quotations.dispatch(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async markDelivered(row: DeliveryChallanRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.patch(ApiEndpoints.quotations.markDelivered(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<DeliveryChallanRow[]>(ApiEndpoints.quotations.deliveryChallans));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return {
      salesOrderId: 0,
      challanDate: null as Date | null,
      deliveryAddress: '',
      transporterName: '',
      vehicleNumber: '',
      notes: '',
    };
  }
}
