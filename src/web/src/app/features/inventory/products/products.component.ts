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

interface ProductForm {
  name: string;
  categoryCode: string;
  hsnSacCode: string;
  gstRate: number;
  baseUnitCode: string;
  salePrice: number;
  purchasePrice: number;
  mrpPrice: number | null;
  minStockLevel: number;
  description: string;
}

@Component({
  selector: 'app-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    DialogModule, InputTextModule, InputNumberModule, ButtonModule,
    PageHeaderComponent, DataTableComponent, FormFieldComponent, DdlDropdownComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.inventory.productsTitle"
        [subtitle]="labels.inventory.productsSubtitle"
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
    </div>

    <!-- Create / Edit dialog -->
    <p-dialog
      [(visible)]="dialogVisible"
      [header]="editId() ? labels.inventory.editProduct : labels.inventory.newProduct"
      [modal]="true"
      [style]="{ width: '620px' }"
      [closable]="true"
    >
      <form (ngSubmit)="saveProduct()" class="space-y-4 pt-2">
        <app-form-field [label]="labels.inventory.productName" [required]="true">
          <input pInputText [(ngModel)]="form.name" name="name" class="w-full" required />
        </app-form-field>

        <div class="grid grid-cols-2 gap-3">
          <app-form-field [label]="labels.inventory.category" [required]="true">
            <app-ddl-dropdown [dkey]="ddlKeys.productCategory"
                              [(ngModel)]="form.categoryCode" name="categoryCode" />
          </app-form-field>
          <app-form-field [label]="labels.inventory.hsnCode">
            <input pInputText [(ngModel)]="form.hsnSacCode" name="hsnSacCode"
                   class="w-full" maxlength="8" />
          </app-form-field>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <app-form-field [label]="labels.inventory.gstRate">
            <p-inputNumber [(ngModel)]="form.gstRate" name="gstRate"
                           [min]="0" [max]="28" [minFractionDigits]="0"
                           suffix=" %" styleClass="w-full" />
          </app-form-field>
          <app-form-field [label]="labels.inventory.baseUnit" [required]="true">
            <input pInputText [(ngModel)]="form.baseUnitCode" name="baseUnitCode"
                   class="w-full" placeholder="PCS / KG / MTR" />
          </app-form-field>
        </div>

        <div class="grid grid-cols-3 gap-3">
          <app-form-field [label]="labels.inventory.salePrice" [required]="true">
            <p-inputNumber [(ngModel)]="form.salePrice" name="salePrice"
                           mode="decimal" [minFractionDigits]="2" [min]="0"
                           styleClass="w-full" />
          </app-form-field>
          <app-form-field [label]="labels.inventory.purchasePrice">
            <p-inputNumber [(ngModel)]="form.purchasePrice" name="purchasePrice"
                           mode="decimal" [minFractionDigits]="2" [min]="0"
                           styleClass="w-full" />
          </app-form-field>
          <app-form-field [label]="labels.inventory.mrpPrice">
            <p-inputNumber [(ngModel)]="form.mrpPrice" name="mrpPrice"
                           mode="decimal" [minFractionDigits]="2" [min]="0"
                           styleClass="w-full" />
          </app-form-field>
        </div>

        <app-form-field [label]="labels.inventory.minStock">
          <p-inputNumber [(ngModel)]="form.minStockLevel" name="minStockLevel"
                         [min]="0" [minFractionDigits]="0" styleClass="w-full" />
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary"
                  [outlined]="true" (onClick)="dialogVisible = false" />
        <p-button [label]="labels.shared.save" [loading]="saving()"
                  (onClick)="saveProduct()" />
      </ng-template>
    </p-dialog>
  `
})
export class ProductsComponent {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(MessageService);

  protected readonly labels = AppLabels;
  protected readonly ddlKeys = AppConstants.ddlKeys;
  protected readonly apiUrl = ApiEndpoints.inventory.products;

  protected dialogVisible = false;
  protected readonly editId = signal<number | null>(null);
  protected readonly saving = signal(false);

  protected form: ProductForm = this.emptyForm();

  protected readonly columns: TableColumn[] = [
    { field: 'productCode',  header: 'Code',     width: '110px' },
    { field: 'name',         header: 'Name',     sortable: true },
    { field: 'categoryCode', header: 'Category', width: '120px' },
    { field: 'baseUnitCode', header: 'Unit',     width: '80px' },
    { field: 'salePrice',    header: 'Price',    width: '110px', type: 'currency' },
    { field: 'isActive',     header: 'Active',   width: '80px',  type: 'boolean' },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: AppLabels.shared.edit,       icon: 'pi pi-pencil', severity: 'secondary', permission: Permissions.inventory.manage },
    { label: AppLabels.shared.deactivate, icon: 'pi pi-ban',    severity: 'danger',    permission: Permissions.inventory.manage },
  ];

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.inventory.newProduct, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.inventory.manage },
  ];

  protected onHeaderAction(action: string): void {
    if (action === this.labels.inventory.newProduct) {
      this.form = this.emptyForm();
      this.editId.set(null);
      this.dialogVisible = true;
    }
  }

  protected onRowAction(event: { action: string; row: Record<string, unknown> }): void {
    const id = event.row['id'] as number;
    if (event.action === this.labels.shared.edit) {
      this.form = {
        name:          String(event.row['name'] ?? ''),
        categoryCode:  String(event.row['categoryCode'] ?? ''),
        hsnSacCode:    '',
        gstRate:       0,
        baseUnitCode:  String(event.row['baseUnitCode'] ?? ''),
        salePrice:     Number(event.row['salePrice'] ?? 0),
        purchasePrice: 0,
        mrpPrice:      null,
        minStockLevel: 0,
        description:   '',
      };
      this.editId.set(id);
      this.dialogVisible = true;
    } else if (event.action === this.labels.shared.deactivate) {
      this.deactivate(id);
    }
  }

  protected async saveProduct(): Promise<void> {
    if (!this.form.name || !this.form.categoryCode || !this.form.baseUnitCode) return;
    this.saving.set(true);
    try {
      const payload = {
        name:          this.form.name,
        categoryCode:  this.form.categoryCode,
        hsnSacCode:    this.form.hsnSacCode || null,
        gstRate:       this.form.gstRate,
        baseUnitCode:  this.form.baseUnitCode,
        salePrice:     this.form.salePrice,
        purchasePrice: this.form.purchasePrice,
        mrpPrice:      this.form.mrpPrice,
        minStockLevel: this.form.minStockLevel,
        description:   this.form.description || null,
      };
      if (this.editId()) {
        await firstValueFrom(this.http.put(ApiEndpoints.inventory.product(this.editId()!), payload));
      } else {
        await firstValueFrom(this.http.post(ApiEndpoints.inventory.products, payload));
      }
      this.dialogVisible = false;
      this.toast.add({ severity: 'success', summary: 'Success', detail: 'Product saved.' });
    } catch { /* errorInterceptor shows toast */ }
    finally { this.saving.set(false); }
  }

  private async deactivate(id: number): Promise<void> {
    try {
      await firstValueFrom(this.http.delete(ApiEndpoints.inventory.product(id)));
      this.toast.add({ severity: 'info', summary: 'Done', detail: 'Product deactivated.' });
    } catch { /* handled */ }
  }

  private emptyForm(): ProductForm {
    return {
      name: '', categoryCode: '', hsnSacCode: '', gstRate: 18,
      baseUnitCode: 'PCS', salePrice: 0, purchasePrice: 0,
      mrpPrice: null, minStockLevel: 0, description: '',
    };
  }
}
