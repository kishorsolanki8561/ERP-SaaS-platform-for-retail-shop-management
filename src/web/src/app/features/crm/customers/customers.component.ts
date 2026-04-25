import {
  ChangeDetectionStrategy, Component,
  inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
import { DdlDropdownComponent } from '../../../shared/components/ddl-dropdown/ddl-dropdown.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppConstants } from '../../../shared/messages/app-constants';
import { Permissions } from '../../../shared/messages/app-permissions';

interface CustomerForm {
  displayName: string;
  customerType: string;
  email: string;
  phone: string;
  gstNumber: string;
  creditLimit: number;
  groupId: number | null;
}

@Component({
  selector: 'app-customers',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    DialogModule, InputTextModule, InputNumberModule, ButtonModule,
    PageHeaderComponent, DataTableComponent, FormFieldComponent, DdlDropdownComponent,
  ],
  template: `
    <app-page-header
      [title]="labels.crm.customersTitle"
      [subtitle]="labels.crm.customersSubtitle"
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

    <!-- Create / Edit dialog -->
    <p-dialog
      [(visible)]="dialogVisible"
      [header]="editId() ? labels.crm.editCustomer : labels.crm.newCustomer"
      [modal]="true"
      [style]="{ width: '520px' }"
      [closable]="true"
    >
      <form (ngSubmit)="saveCustomer()" class="space-y-4 pt-2">
        <app-form-field [label]="labels.crm.displayName" [required]="true">
          <input pInputText [(ngModel)]="form.displayName" name="displayName"
                 class="w-full" required />
        </app-form-field>

        <app-form-field [label]="labels.crm.customerType" [required]="true">
          <app-ddl-dropdown [dkey]="ddlKeys.customerType"
                            [(ngModel)]="form.customerType" name="customerType" />
        </app-form-field>

        <div class="grid grid-cols-2 gap-3">
          <app-form-field [label]="labels.crm.phone">
            <input pInputText [(ngModel)]="form.phone" name="phone" class="w-full" />
          </app-form-field>
          <app-form-field [label]="labels.crm.email">
            <input pInputText [(ngModel)]="form.email" name="email"
                   type="email" class="w-full" />
          </app-form-field>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <app-form-field [label]="labels.crm.gstNumber">
            <input pInputText [(ngModel)]="form.gstNumber" name="gstNumber"
                   class="w-full" maxlength="15" />
          </app-form-field>
          <app-form-field [label]="labels.crm.creditLimit">
            <p-inputNumber [(ngModel)]="form.creditLimit" name="creditLimit"
                           mode="decimal" [minFractionDigits]="0" [min]="0"
                           styleClass="w-full" />
          </app-form-field>
        </div>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary"
                  [outlined]="true" (onClick)="dialogVisible = false" />
        <p-button [label]="labels.shared.save" [loading]="saving()"
                  (onClick)="saveCustomer()" />
      </ng-template>
    </p-dialog>
  `
})
export class CustomersComponent {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(MessageService);

  protected readonly labels = AppLabels;
  protected readonly ddlKeys = AppConstants.ddlKeys;
  protected readonly apiUrl = ApiEndpoints.crm.customers;

  protected dialogVisible = false;
  protected readonly editId = signal<number | null>(null);
  protected readonly saving = signal(false);

  protected form: CustomerForm = this.emptyForm();

  protected readonly columns: TableColumn[] = [
    { field: 'customerCode', header: 'Code',  width: '110px' },
    { field: 'displayName',  header: 'Name',  sortable: true },
    { field: 'customerType', header: 'Type',  width: '110px' },
    { field: 'phone',        header: 'Phone', width: '140px' },
    { field: 'email',        header: 'Email', sortable: true },
    { field: 'isActive',     header: 'Active', width: '80px' },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: AppLabels.shared.edit,       icon: 'pi pi-pencil', severity: 'secondary', permission: Permissions.crm.edit },
    { label: AppLabels.shared.deactivate, icon: 'pi pi-ban',    severity: 'danger',    permission: Permissions.crm.edit },
  ];

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.crm.newCustomer, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.crm.create },
  ];

  protected onHeaderAction(action: string): void {
    if (action === this.labels.crm.newCustomer) {
      this.form = this.emptyForm();
      this.editId.set(null);
      this.dialogVisible = true;
    }
  }

  protected onRowAction(event: { action: string; row: Record<string, unknown> }): void {
    const id = event.row['id'] as number;
    if (event.action === this.labels.shared.edit) {
      this.form = {
        displayName:  String(event.row['displayName'] ?? ''),
        customerType: String(event.row['customerType'] ?? ''),
        email:        String(event.row['email'] ?? ''),
        phone:        String(event.row['phone'] ?? ''),
        gstNumber:    String(event.row['gstNumber'] ?? ''),
        creditLimit:  Number(event.row['creditLimit'] ?? 0),
        groupId:      null,
      };
      this.editId.set(id);
      this.dialogVisible = true;
    } else if (event.action === this.labels.shared.deactivate) {
      this.deactivate(id);
    }
  }

  protected async saveCustomer(): Promise<void> {
    if (!this.form.displayName || !this.form.customerType) return;
    this.saving.set(true);
    try {
      const payload = {
        displayName:  this.form.displayName,
        customerType: this.form.customerType,
        email:        this.form.email || null,
        phone:        this.form.phone || null,
        gstNumber:    this.form.gstNumber || null,
        creditLimit:  this.form.creditLimit,
        groupId:      this.form.groupId,
      };
      if (this.editId()) {
        await firstValueFrom(this.http.put(ApiEndpoints.crm.customer(this.editId()!), payload));
      } else {
        await firstValueFrom(this.http.post(ApiEndpoints.crm.customers, payload));
      }
      this.dialogVisible = false;
      this.toast.add({ severity: 'success', summary: 'Success', detail: 'Customer saved.' });
    } catch { /* errorInterceptor shows toast */ }
    finally { this.saving.set(false); }
  }

  private async deactivate(id: number): Promise<void> {
    try {
      await firstValueFrom(this.http.delete(ApiEndpoints.crm.customer(id)));
      this.toast.add({ severity: 'info', summary: 'Done', detail: 'Customer deactivated.' });
    } catch { /* handled */ }
  }

  private emptyForm(): CustomerForm {
    return { displayName: '', customerType: '', email: '', phone: '', gstNumber: '', creditLimit: 0, groupId: null };
  }
}
