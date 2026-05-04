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

interface WarrantyRow {
  id: number;
  serialNumber: string;
  productNameSnapshot: string;
  customerNameSnapshot: string;
  registrationDate: string;
  warrantyExpiryDate: string;
  isActive: boolean;
}

@Component({
  selector: 'app-warranty-registrations',
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
        [title]="labels.warranty.registrationsTitle"
        [subtitle]="labels.warranty.registrationsSubtitle"
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
              <th>Serial #</th>
              <th>Product</th>
              <th>Customer</th>
              <th style="width: 130px">Registered</th>
              <th style="width: 130px">Expiry</th>
              <th style="width: 80px">Active</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm font-semibold text-slate-700 dark:text-slate-300">{{ row.serialNumber }}</td>
              <td class="text-slate-800 dark:text-slate-200">{{ row.productNameSnapshot }}</td>
              <td class="text-slate-600">{{ row.customerNameSnapshot }}</td>
              <td class="tabular-nums text-slate-500">{{ row.registrationDate | date:'dd MMM yyyy' }}</td>
              <td class="tabular-nums" [class.text-red-600]="isExpired(row)" [class.text-slate-500]="!isExpired(row)">
                {{ row.warrantyExpiryDate | date:'dd MMM yyyy' }}
              </td>
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
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="6">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-shield text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No warranty registrations</p>
                  <p class="text-xs text-slate-400">Register products sold under warranty.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New Registration dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.warranty.newRegistration"
              [modal]="true" [style]="{ width: '520px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <div class="grid grid-cols-2 gap-4">
          <app-form-field label="Invoice ID" [required]="true">
            <p-inputNumber [(ngModel)]="form.invoiceId" name="invoiceId" [min]="1"
                          styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
          <app-form-field label="Invoice Line ID" [required]="true">
            <p-inputNumber [(ngModel)]="form.invoiceLineId" name="invoiceLineId" [min]="1"
                          styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.warranty.productSerial" [required]="true">
            <input pInputText [(ngModel)]="form.serialNumber" name="serialNumber"
                   class="w-full" placeholder="e.g. SN123456789" />
          </app-form-field>
          <app-form-field label="Warranty Months" [required]="true">
            <p-inputNumber [(ngModel)]="form.warrantyMonths" name="warrantyMonths" [min]="1" [max]="120"
                          styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
        </div>
        <app-form-field [label]="labels.common.notes">
          <input pInputText [(ngModel)]="form.notes" name="notes" class="w-full" placeholder="Optional notes" />
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.warranty.newRegistration" [loading]="saving()"
                  [disabled]="!form.invoiceId || !form.invoiceLineId || !form.serialNumber || !form.warrantyMonths"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>
  `
})
export class WarrantyRegistrationsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly allRows  = signal<WarrantyRow[]>([]);

  protected searchQuery   = '';
  protected dialogVisible = false;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.serialNumber.toLowerCase().includes(q) ||
      r.productNameSnapshot.toLowerCase().includes(q) ||
      r.customerNameSnapshot.toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.warranty.newRegistration, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.warranty.manage },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.warranty.newRegistration) {
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected isExpired(row: WarrantyRow): boolean {
    return new Date(row.warrantyExpiryDate) < new Date();
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.warranty.createReg, {
        invoiceId:      this.form.invoiceId,
        invoiceLineId:  this.form.invoiceLineId,
        serialNumber:   this.form.serialNumber,
        warrantyMonths: this.form.warrantyMonths,
        notes:          this.form.notes || null,
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<WarrantyRow[]>(ApiEndpoints.warranty.registrations));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return { invoiceId: 0, invoiceLineId: 0, serialNumber: '', warrantyMonths: 12, notes: '' };
  }
}
