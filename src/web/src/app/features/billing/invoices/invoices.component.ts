import {
  ChangeDetectionStrategy, Component,
  inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn, RowAction } from '../../../shared/components/data-table/data-table.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppRoutePaths } from '../../../shared/messages/app-routes';
import { Permissions } from '../../../shared/messages/app-permissions';

@Component({
  selector: 'app-invoices',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    DialogModule, InputTextModule, InputNumberModule, ButtonModule,
    PageHeaderComponent, DataTableComponent, FormFieldComponent,
  ],
  template: `
    <app-page-header
      [title]="labels.billing.invoicesTitle"
      [subtitle]="labels.billing.invoicesSubtitle"
      [actions]="headerActions"
      (actionClick)="onHeaderAction($event)"
    />

    <app-data-table
      [columns]="columns"
      [apiUrl]="apiUrl"
      [rowActions]="rowActions"
      [searchable]="true"
      (rowAction)="onRowAction($event)"
    />

    <!-- New Invoice dialog — collects minimal info to create a Draft -->
    <p-dialog
      [(visible)]="dialogVisible"
      [header]="labels.billing.newInvoice"
      [modal]="true"
      [style]="{ width: '480px' }"
    >
      <form class="space-y-4 pt-2">
        <app-form-field [label]="labels.billing.customer" [required]="true">
          <input pInputText [(ngModel)]="draftForm.customerId" name="customerId"
                 type="number" class="w-full" placeholder="Customer ID" />
        </app-form-field>
        <app-form-field [label]="labels.billing.warehouseId" [required]="true">
          <input pInputText [(ngModel)]="draftForm.warehouseId" name="warehouseId"
                 type="number" class="w-full" placeholder="Warehouse ID" />
        </app-form-field>
        <app-form-field [label]="labels.billing.notes">
          <input pInputText [(ngModel)]="draftForm.notes" name="notes" class="w-full" />
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary"
                  [outlined]="true" (onClick)="dialogVisible = false" />
        <p-button [label]="labels.billing.newInvoice" [loading]="creating()"
                  (onClick)="createDraft()" />
      </ng-template>
    </p-dialog>
  `
})
export class InvoicesComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly toast = inject(MessageService);

  protected readonly labels = AppLabels;
  protected readonly apiUrl = ApiEndpoints.billing.invoices;

  protected dialogVisible = false;
  protected readonly creating = signal(false);
  protected draftForm = { customerId: 0, warehouseId: 0, notes: '' };

  protected readonly columns: TableColumn[] = [
    { field: 'invoiceNumber',       header: 'Invoice #',  width: '130px', sortable: true },
    { field: 'invoiceDate',         header: 'Date',       width: '120px', sortable: true },
    { field: 'customerNameSnapshot', header: 'Customer',  sortable: true },
    { field: 'status',              header: 'Status',     width: '110px' },
    { field: 'grandTotal',          header: 'Amount',     width: '120px', sortable: true },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: 'View',   icon: 'pi pi-eye',    severity: 'secondary' },
    { label: 'Cancel', icon: 'pi pi-times',  severity: 'danger', permission: Permissions.billing.cancel },
  ];

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.billing.newInvoice, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.billing.create },
  ];

  protected onHeaderAction(action: string): void {
    if (action === this.labels.billing.newInvoice) {
      this.draftForm = { customerId: 0, warehouseId: 0, notes: '' };
      this.dialogVisible = true;
    }
  }

  protected onRowAction(event: { action: string; row: Record<string, unknown> }): void {
    const id = event.row['id'] as number;
    if (event.action === 'View') {
      this.router.navigate([AppRoutePaths.billing.invoiceDetail(id)]);
    }
  }

  protected async createDraft(): Promise<void> {
    if (!this.draftForm.customerId || !this.draftForm.warehouseId) return;
    this.creating.set(true);
    try {
      const result = await firstValueFrom(
        this.http.post<{ value: number }>(ApiEndpoints.billing.invoices, {
          invoiceDate:  new Date().toISOString(),
          customerId:   this.draftForm.customerId,
          warehouseId:  this.draftForm.warehouseId,
          shopId:       0,
          notes:        this.draftForm.notes || null,
        })
      );
      this.dialogVisible = false;
      if (result?.value) {
        this.router.navigate([AppRoutePaths.billing.invoiceDetail(result.value)]);
      }
    } catch { /* handled */ }
    finally { this.creating.set(false); }
  }
}
