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
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface AccountRow {
  id: number;
  accountCode: string;
  accountName: string;
  accountType: string;
  groupName?: string;
  openingBalance: number;
  currentBalance: number;
  isActive: boolean;
}

interface AccountGroup {
  id: number;
  groupName: string;
  accountType: string;
}

const TYPE_BADGE: Record<string, string> = {
  Asset:     'inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold bg-blue-50 text-blue-700',
  Liability: 'inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold bg-red-50 text-red-700',
  Equity:    'inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold bg-purple-50 text-purple-700',
  Revenue:   'inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold bg-emerald-50 text-emerald-700',
  Expense:   'inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold bg-amber-50 text-amber-700',
  '*':       'inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold bg-slate-100 text-slate-600',
};

@Component({
  selector: 'app-accounting-accounts',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule, InputTextModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.accounting.accountsTitle"
        [subtitle]="labels.accounting.accountsSubtitle"
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
            <span class="text-xs text-slate-400 hidden sm:block">{{ rows().length | number }} accounts</span>
          }
        </div>

        <p-table [value]="rows()" [loading]="loading()" [paginator]="true" [rows]="25"
                 [rowsPerPageOptions]="[25, 50, 100]" styleClass="p-datatable-sm"
                 [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th style="width: 120px">Code</th>
              <th>Account Name</th>
              <th style="width: 110px">Type</th>
              <th style="width: 140px">Group</th>
              <th style="width: 140px" class="text-right">Opening Balance</th>
              <th style="width: 140px" class="text-right">Current Balance</th>
              <th style="width: 80px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm text-slate-500">{{ row.accountCode }}</td>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ row.accountName }}</td>
              <td><span [class]="typeClass(row.accountType)">{{ row.accountType }}</span></td>
              <td class="text-slate-600 text-sm">{{ row.groupName ?? '—' }}</td>
              <td class="text-right tabular-nums text-slate-600">₹ {{ row.openingBalance | number:'1.2-2' }}</td>
              <td class="text-right tabular-nums font-semibold"
                  [class.text-emerald-600]="row.currentBalance >= 0"
                  [class.text-red-600]="row.currentBalance < 0">
                ₹ {{ row.currentBalance | number:'1.2-2' }}
              </td>
              <td class="text-right">
                <button pButton icon="pi pi-pencil" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        pTooltip="Edit" tooltipPosition="left"
                        (click)="openEdit(row)"></button>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="7">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-book text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No accounts found</p>
                  <p class="text-xs text-slate-400">Your chart of accounts will appear here.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- Create / Edit Account dialog -->
    <p-dialog [(visible)]="dialogVisible"
              [header]="editId ? labels.accounting.editAccount : labels.accounting.newAccount"
              [modal]="true" [style]="{ width: '500px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <div class="grid grid-cols-2 gap-4">
          <app-form-field label="Account Code" [required]="true">
            <input pInputText [(ngModel)]="form.accountCode" name="accountCode"
                   class="w-full" placeholder="e.g. 1001" />
          </app-form-field>
          <app-form-field label="Account Type" [required]="true">
            <select [(ngModel)]="form.accountType" name="accountType"
                    class="w-full text-sm border border-slate-200 dark:border-slate-700 rounded-lg px-3 py-2
                           bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200">
              <option value="">Select type...</option>
              @for (t of accountTypes; track t) {
                <option [value]="t">{{ t }}</option>
              }
            </select>
          </app-form-field>
        </div>
        <app-form-field label="Account Name" [required]="true">
          <input pInputText [(ngModel)]="form.accountName" name="accountName"
                 class="w-full" placeholder="e.g. Cash in Hand" />
        </app-form-field>
        <app-form-field label="Account Group">
          <select [(ngModel)]="form.accountGroupId" name="groupId"
                  class="w-full text-sm border border-slate-200 dark:border-slate-700 rounded-lg px-3 py-2
                         bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200">
            <option [ngValue]="null">None</option>
            @for (g of groups(); track g.id) {
              <option [ngValue]="g.id">{{ g.groupName }}</option>
            }
          </select>
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="editId ? labels.shared.save : labels.accounting.newAccount"
                  [loading]="saving()"
                  [disabled]="!form.accountCode || !form.accountName || !form.accountType"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>
  `
})
export class AccountingAccountsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly allRows  = signal<AccountRow[]>([]);
  protected readonly groups   = signal<AccountGroup[]>([]);

  protected searchQuery   = '';
  protected dialogVisible = false;
  protected editId: number | null = null;

  protected readonly accountTypes = ['Asset', 'Liability', 'Equity', 'Revenue', 'Expense'];

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.accountCode.toLowerCase().includes(q) ||
      r.accountName.toLowerCase().includes(q) ||
      r.accountType.toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.accounting.newAccount, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.accounting.manage },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected typeClass(type: string): string {
    return TYPE_BADGE[type] ?? TYPE_BADGE['*'];
  }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.accounting.newAccount) {
      this.editId = null;
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected openEdit(row: AccountRow): void {
    this.editId = row.id;
    this.form = {
      accountCode:    row.accountCode,
      accountName:    row.accountName,
      accountType:    row.accountType,
      accountGroupId: null,
    };
    this.dialogVisible = true;
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      if (this.editId) {
        await firstValueFrom(this.http.patch(ApiEndpoints.accounting.account(this.editId), {
          accountName:    this.form.accountName,
          accountGroupId: this.form.accountGroupId,
        }));
      } else {
        await firstValueFrom(this.http.post(ApiEndpoints.accounting.accounts, {
          accountCode:    this.form.accountCode,
          accountName:    this.form.accountName,
          accountType:    this.form.accountType,
          accountGroupId: this.form.accountGroupId,
        }));
      }
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const [accsResp, grpsResp] = await Promise.all([
        firstValueFrom(this.http.get<{ items: AccountRow[] }>(
          `${ApiEndpoints.accounting.accounts}?page=1&pageSize=500`
        )),
        firstValueFrom(this.http.get<AccountGroup[]>(ApiEndpoints.accounting.accountGroups)),
      ]);
      this.allRows.set(accsResp?.items ?? []);
      this.groups.set(grpsResp ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return { accountCode: '', accountName: '', accountType: '', accountGroupId: null as number | null };
  }
}
