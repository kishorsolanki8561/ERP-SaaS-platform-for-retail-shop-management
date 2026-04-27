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
import { DdlDropdownComponent } from '../../../shared/components/ddl-dropdown/ddl-dropdown.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppConstants } from '../../../shared/messages/app-constants';
import { AppRoutePaths } from '../../../shared/messages/app-routes';
import { Permissions } from '../../../shared/messages/app-permissions';

interface CreditForm {
  customerId: number;
  customerName: string;
  amount: number;
  referenceType: string;
  referenceNumber: string;
  notes: string;
}

@Component({
  selector: 'app-wallet-balances',
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
        [title]="labels.wallet.balancesTitle"
        [subtitle]="labels.wallet.balancesSubtitle"
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

    <!-- Credit dialog -->
    <p-dialog
      [(visible)]="creditVisible"
      [header]="labels.wallet.creditWallet"
      [modal]="true"
      [style]="{ width: '480px' }"
    >
      <form class="space-y-4 pt-2">
        <div class="grid grid-cols-2 gap-3">
          <app-form-field [label]="labels.wallet.customerId" [required]="true">
            <input pInputText [(ngModel)]="creditForm.customerId" name="customerId"
                   type="number" class="w-full" />
          </app-form-field>
          <app-form-field [label]="labels.wallet.customer" [required]="true">
            <input pInputText [(ngModel)]="creditForm.customerName" name="customerName"
                   class="w-full" />
          </app-form-field>
        </div>
        <app-form-field [label]="labels.wallet.amount" [required]="true">
          <p-inputNumber [(ngModel)]="creditForm.amount" name="amount"
                         mode="decimal" [minFractionDigits]="2" [min]="0.01"
                         styleClass="w-full" />
        </app-form-field>
        <div class="grid grid-cols-2 gap-3">
          <app-form-field [label]="labels.wallet.referenceType">
            <app-ddl-dropdown [dkey]="ddlKeys.walletReferenceType"
                              [(ngModel)]="creditForm.referenceType" name="referenceType" />
          </app-form-field>
          <app-form-field [label]="labels.wallet.referenceNumber">
            <input pInputText [(ngModel)]="creditForm.referenceNumber" name="referenceNumber"
                   class="w-full" />
          </app-form-field>
        </div>
        <app-form-field [label]="labels.wallet.notes">
          <input pInputText [(ngModel)]="creditForm.notes" name="notes" class="w-full" />
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary"
                  [outlined]="true" (onClick)="creditVisible = false" />
        <p-button [label]="labels.wallet.creditWallet" [loading]="acting()"
                  (onClick)="submitCredit()" />
      </ng-template>
    </p-dialog>
  `
})
export class WalletBalancesComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly toast = inject(MessageService);

  protected readonly labels = AppLabels;
  protected readonly ddlKeys = AppConstants.ddlKeys;
  protected readonly apiUrl = ApiEndpoints.wallet.balances;

  protected creditVisible = false;
  protected readonly acting = signal(false);
  protected creditForm: CreditForm = this.emptyCredit();

  protected readonly columns: TableColumn[] = [
    { field: 'customerCode',        header: 'Code',     width: '110px' },
    { field: 'customerNameSnapshot', header: 'Customer', sortable: true },
    { field: 'balance',              header: 'Balance',  width: '140px', sortable: true, type: 'currency' },
    { field: 'lastTransactionAtUtc', header: 'Last Txn', width: '150px', sortable: true, type: 'datetime' },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: 'View Transactions', icon: 'pi pi-list',   severity: 'secondary' },
    { label: AppLabels.wallet.creditWallet, icon: 'pi pi-plus-circle', severity: 'secondary',
      permission: Permissions.wallet.credit },
  ];

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.wallet.creditWallet, icon: 'pi pi-plus', severity: 'primary',
      permission: Permissions.wallet.credit },
  ];

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.wallet.creditWallet) {
      this.creditForm = this.emptyCredit();
      this.creditVisible = true;
    }
  }

  protected onRowAction(event: { action: string; row: Record<string, unknown> }): void {
    const customerId = event.row['customerId'] as number;
    if (event.action === 'View Transactions') {
      this.router.navigate([AppRoutePaths.wallet.transactions], { queryParams: { customerId } });
    } else if (event.action === AppLabels.wallet.creditWallet) {
      this.creditForm = {
        customerId,
        customerName: String(event.row['customerNameSnapshot'] ?? ''),
        amount: 0,
        referenceType: 'Manual',
        referenceNumber: '',
        notes: '',
      };
      this.creditVisible = true;
    }
  }

  protected async submitCredit(): Promise<void> {
    if (!this.creditForm.customerId || !this.creditForm.customerName || !this.creditForm.amount) return;
    this.acting.set(true);
    try {
      const result = await firstValueFrom(
        this.http.post<{ receiptNumber: string; newBalance: number }>(ApiEndpoints.wallet.credit, {
          customerId:      this.creditForm.customerId,
          customerName:    this.creditForm.customerName,
          amount:          this.creditForm.amount,
          referenceType:   this.creditForm.referenceType || 'Manual',
          referenceId:     null,
          referenceNumber: this.creditForm.referenceNumber || null,
          notes:           this.creditForm.notes || null,
        })
      );
      this.creditVisible = false;
      this.toast.add({
        severity: 'success',
        summary: 'Credited',
        detail: `Receipt: ${result?.receiptNumber} | Balance: ₹${result?.newBalance?.toFixed(2)}`,
      });
    } catch { /* errorInterceptor shows toast */ }
    finally { this.acting.set(false); }
  }

  private emptyCredit(): CreditForm {
    return { customerId: 0, customerName: '', amount: 0, referenceType: 'Manual', referenceNumber: '', notes: '' };
  }
}
