import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn } from '../../../shared/components/data-table/data-table.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { DdlDropdownComponent } from '../../../shared/components/ddl-dropdown/ddl-dropdown.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppConstants } from '../../../shared/messages/app-constants';
import { Permissions } from '../../../shared/messages/app-permissions';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';

interface DebitForm {
  amount: number;
  referenceType: string;
  referenceNumber: string;
  notes: string;
}

@Component({
  selector: 'app-wallet-transactions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    DialogModule, InputTextModule, InputNumberModule, ButtonModule,
    PageHeaderComponent, DataTableComponent, FormFieldComponent,
    DdlDropdownComponent, HasPermissionDirective,
  ],
  template: `
    <app-page-header
      [title]="labels.wallet.transactionsTitle"
      [subtitle]="customerId() ? 'Customer ID: ' + customerId() : labels.wallet.transactionsSubtitle"
    />

    @if (customerId()) {
      <!-- Balance card -->
      @if (balance() !== null) {
        <div class="mb-6 p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200
                    dark:border-slate-800 flex items-center gap-6">
          <div>
            <div class="text-xs text-slate-400 uppercase tracking-wide">
              {{ labels.wallet.currentBalance }}
            </div>
            <div class="text-2xl font-bold text-emerald-600">
              {{ balance()! | currency:'INR':'symbol':'1.2-2' }}
            </div>
          </div>
          <div class="flex gap-2 ml-auto">
            <ng-container *hasPermission="permissions.wallet.debit">
              <p-button [label]="labels.wallet.debitWallet" icon="pi pi-minus-circle"
                        severity="danger" [outlined]="true" size="small"
                        (onClick)="debitVisible = true" />
            </ng-container>
          </div>
        </div>
      }

      <app-data-table
        [columns]="columns"
        [apiUrl]="transactionsUrl()"
        [searchable]="false"
      />
    } @else {
      <div class="text-center py-20 text-slate-400">
        Open from the Customer Balances page to see transactions.
      </div>
    }

    <!-- Debit dialog -->
    <p-dialog [(visible)]="debitVisible" [header]="labels.wallet.debitWallet"
              [modal]="true" [style]="{ width: '420px' }">
      <form class="space-y-4 pt-2">
        <app-form-field [label]="labels.wallet.amount" [required]="true">
          <p-inputNumber [(ngModel)]="debitForm.amount" name="amount"
                         mode="decimal" [minFractionDigits]="2" [min]="0.01"
                         styleClass="w-full" />
        </app-form-field>
        <div class="grid grid-cols-2 gap-3">
          <app-form-field [label]="labels.wallet.referenceType">
            <app-ddl-dropdown [dkey]="ddlKeys.walletReferenceType"
                              [(ngModel)]="debitForm.referenceType" name="referenceType" />
          </app-form-field>
          <app-form-field [label]="labels.wallet.referenceNumber">
            <input pInputText [(ngModel)]="debitForm.referenceNumber" name="referenceNumber"
                   class="w-full" />
          </app-form-field>
        </div>
        <app-form-field [label]="labels.wallet.notes">
          <input pInputText [(ngModel)]="debitForm.notes" name="notes" class="w-full" />
        </app-form-field>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary"
                  [outlined]="true" (onClick)="debitVisible = false" />
        <p-button [label]="labels.wallet.debitWallet" severity="danger"
                  [loading]="acting()" (onClick)="submitDebit()" />
      </ng-template>
    </p-dialog>
  `
})
export class WalletTransactionsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(MessageService);

  protected readonly labels = AppLabels;
  protected readonly ddlKeys = AppConstants.ddlKeys;
  protected readonly permissions = Permissions;

  protected readonly customerId = signal<number | null>(null);
  protected readonly balance = signal<number | null>(null);
  protected readonly acting = signal(false);

  protected debitVisible = false;
  protected debitForm: DebitForm = this.emptyDebit();

  protected readonly transactionsUrl = computed(() =>
    this.customerId() ? ApiEndpoints.wallet.transactions(this.customerId()!) : ''
  );

  protected readonly columns: TableColumn[] = [
    { field: 'transactionType', header: 'Type',    width: '100px' },
    { field: 'amount',          header: 'Amount',  width: '120px', sortable: true },
    { field: 'balanceBefore',   header: 'Before',  width: '120px' },
    { field: 'balanceAfter',    header: 'After',   width: '120px' },
    { field: 'referenceType',   header: 'Ref Type', width: '110px' },
    { field: 'referenceNumber', header: 'Ref #',   width: '130px' },
    { field: 'receiptNumber',   header: AppLabels.wallet.receipt, width: '130px' },
    { field: 'createdAtUtc',    header: 'Date',    width: '140px', sortable: true },
  ];

  async ngOnInit(): Promise<void> {
    const cid = this.route.snapshot.queryParamMap.get('customerId');
    if (!cid) return;
    const id = Number(cid);
    this.customerId.set(id);
    try {
      const b = await firstValueFrom(
        this.http.get<{ balance: number }>(ApiEndpoints.wallet.balance(id))
      );
      this.balance.set(b?.balance ?? 0);
    } catch { /* balance shown as null */ }
  }

  protected async submitDebit(): Promise<void> {
    const cid = this.customerId();
    if (!cid || !this.debitForm.amount) return;
    this.acting.set(true);
    try {
      await firstValueFrom(
        this.http.post(ApiEndpoints.wallet.debit, {
          customerId:      cid,
          amount:          this.debitForm.amount,
          referenceType:   this.debitForm.referenceType || 'Manual',
          referenceId:     null,
          referenceNumber: this.debitForm.referenceNumber || null,
          notes:           this.debitForm.notes || null,
        })
      );
      this.debitVisible = false;
      this.debitForm = this.emptyDebit();
      this.toast.add({ severity: 'info', summary: 'Debited', detail: 'Wallet debited.' });
      // Refresh balance
      try {
        const b = await firstValueFrom(
          this.http.get<{ balance: number }>(ApiEndpoints.wallet.balance(cid))
        );
        this.balance.set(b?.balance ?? 0);
      } catch { /* ignore */ }
    } catch { /* errorInterceptor shows toast */ }
    finally { this.acting.set(false); }
  }

  private emptyDebit(): DebitForm {
    return { amount: 0, referenceType: 'Manual', referenceNumber: '', notes: '' };
  }
}
