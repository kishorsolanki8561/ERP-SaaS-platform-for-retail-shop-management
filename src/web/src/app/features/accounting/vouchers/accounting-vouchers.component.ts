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
import { DdlDropdownComponent } from '../../../shared/components/ddl-dropdown/ddl-dropdown.component';
import { AuditLogComponent } from '../../../shared/components/audit-log/audit-log.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppConstants } from '../../../shared/messages/app-constants';
import { Permissions } from '../../../shared/messages/app-permissions';

interface VoucherRow {
  id: number;
  voucherNumber: string;
  voucherType: string;
  voucherDate: string;
  narration: string;
  totalAmount: number;
  status: string;
}

interface VoucherLine {
  accountId: number | null;
  debitAmount: number;
  creditAmount: number;
  narration: string;
}

const STATUS_BADGE: Record<string, string> = {
  Draft:    'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
  Posted:   'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700',
  Reversed: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-red-100 text-red-600',
  '*':      'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
};

@Component({
  selector: 'app-accounting-vouchers',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, CalendarModule,
    PageHeaderComponent, FormFieldComponent, DdlDropdownComponent, AuditLogComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.accounting.vouchersTitle"
        [subtitle]="labels.accounting.vouchersSubtitle"
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
            <span class="text-xs text-slate-400 hidden sm:block">{{ rows().length | number }} vouchers</span>
          }
        </div>

        <p-table [value]="rows()" [loading]="loading()" [paginator]="true" [rows]="20"
                 [rowsPerPageOptions]="[10, 25, 50]" styleClass="p-datatable-sm"
                 [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th style="width: 150px">Voucher #</th>
              <th style="width: 110px">Type</th>
              <th style="width: 130px">Date</th>
              <th>Narration</th>
              <th style="width: 140px" class="text-right">Amount</th>
              <th style="width: 100px">Status</th>
              <th style="width: 100px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm font-semibold text-slate-700 dark:text-slate-300">{{ row.voucherNumber }}</td>
              <td>
                <span class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold
                             bg-indigo-50 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-300">
                  {{ row.voucherType }}
                </span>
              </td>
              <td class="tabular-nums text-slate-500">{{ row.voucherDate | date:'dd MMM yyyy' }}</td>
              <td class="text-slate-600 max-w-xs truncate">{{ row.narration }}</td>
              <td class="text-right tabular-nums font-semibold text-slate-800 dark:text-slate-200">
                ₹ {{ row.totalAmount | number:'1.2-2' }}
              </td>
              <td><span [class]="statusClass(row.status)">{{ row.status }}</span></td>
              <td class="text-right">
                @if (row.status === 'Draft') {
                  <button pButton icon="pi pi-send" class="p-button-sm p-button-text p-button-rounded p-button-success"
                          pTooltip="Post Voucher" tooltipPosition="left"
                          (click)="post(row)" [disabled]="actionId() === row.id"></button>
                }
                @if (row.status === 'Posted') {
                  <button pButton icon="pi pi-replay" class="p-button-sm p-button-text p-button-rounded p-button-warning"
                          pTooltip="Reverse" tooltipPosition="left"
                          (click)="openReverse(row)" [disabled]="actionId() === row.id"></button>
                }
                <button pButton icon="pi pi-history" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        pTooltip="Audit Log" tooltipPosition="left" (click)="openAuditLog(row.id)"></button>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="7">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-file-edit text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No vouchers</p>
                  <p class="text-xs text-slate-400">Create vouchers to record accounting entries.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- Create Voucher dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.accounting.newVoucher"
              [modal]="true" [style]="{ width: '680px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.accounting.voucherType" [required]="true">
            <app-ddl-dropdown [dkey]="ddlKeys.voucherType" [(ngModel)]="form.voucherType" name="voucherType" />
          </app-form-field>
          <app-form-field [label]="labels.accounting.voucherDate" [required]="true">
            <p-calendar [(ngModel)]="form.voucherDate" name="voucherDate" dateFormat="dd/mm/yy"
                        styleClass="w-full" inputStyleClass="w-full" />
          </app-form-field>
        </div>
        <app-form-field label="Narration">
          <input pInputText [(ngModel)]="form.narration" name="narration" class="w-full"
                 placeholder="Brief description of this entry" />
        </app-form-field>

        <!-- Lines -->
        <div class="space-y-2">
          <div class="flex items-center justify-between">
            <span class="text-sm font-semibold text-slate-700 dark:text-slate-300">Entries</span>
            <button type="button" (click)="addLine()"
                    class="text-xs text-indigo-600 hover:text-indigo-700 font-medium flex items-center gap-1">
              <i class="pi pi-plus text-[10px]"></i> Add Line
            </button>
          </div>
          <div class="rounded-lg border border-slate-200 dark:border-slate-700 overflow-hidden">
            <table class="w-full text-xs">
              <thead>
                <tr class="bg-slate-50 dark:bg-slate-800">
                  <th class="text-left px-3 py-2 text-slate-500 font-medium">Account ID</th>
                  <th class="text-right px-3 py-2 text-slate-500 font-medium w-28">{{ labels.accounting.debit }}</th>
                  <th class="text-right px-3 py-2 text-slate-500 font-medium w-28">{{ labels.accounting.credit }}</th>
                  <th class="w-8"></th>
                </tr>
              </thead>
              <tbody>
                @for (line of form.lines; track $index) {
                  <tr class="border-t border-slate-100 dark:border-slate-700">
                    <td class="px-2 py-1">
                      <p-inputNumber [(ngModel)]="line.accountId" [name]="'accId_' + $index"
                                    [min]="1" styleClass="w-full" inputStyleClass="w-full !h-7 !text-xs" />
                    </td>
                    <td class="px-2 py-1">
                      <p-inputNumber [(ngModel)]="line.debitAmount" [name]="'dr_' + $index"
                                    [min]="0" [maxFractionDigits]="2"
                                    styleClass="w-full" inputStyleClass="w-full !h-7 !text-xs text-right" />
                    </td>
                    <td class="px-2 py-1">
                      <p-inputNumber [(ngModel)]="line.creditAmount" [name]="'cr_' + $index"
                                    [min]="0" [maxFractionDigits]="2"
                                    styleClass="w-full" inputStyleClass="w-full !h-7 !text-xs text-right" />
                    </td>
                    <td class="px-1">
                      @if (form.lines.length > 2) {
                        <button type="button" (click)="removeLine($index)"
                                class="p-1 text-red-400 hover:text-red-600">
                          <i class="pi pi-times text-[10px]"></i>
                        </button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
              <tfoot>
                <tr class="bg-slate-50 dark:bg-slate-800 border-t border-slate-200 dark:border-slate-700">
                  <td class="px-3 py-1.5 text-xs font-semibold text-slate-600">Total</td>
                  <td class="px-3 py-1.5 text-right text-xs font-semibold text-slate-700 tabular-nums">
                    ₹ {{ totalDebit() | number:'1.2-2' }}
                  </td>
                  <td class="px-3 py-1.5 text-right text-xs font-semibold text-slate-700 tabular-nums">
                    ₹ {{ totalCredit() | number:'1.2-2' }}
                  </td>
                  <td></td>
                </tr>
              </tfoot>
            </table>
          </div>
          @if (totalDebit() !== totalCredit() && form.lines.length > 0) {
            <p class="text-xs text-red-600">Debit and credit totals must match.</p>
          }
        </div>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.accounting.newVoucher" [loading]="saving()"
                  [disabled]="!form.voucherType || !form.voucherDate || totalDebit() !== totalCredit() || totalDebit() === 0"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>

    <!-- Reverse Voucher dialog -->
    <p-dialog [(visible)]="reverseDialogVisible" header="Reverse Voucher"
              [modal]="true" [style]="{ width: '400px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field label="Narration" [required]="true">
          <input pInputText [(ngModel)]="reverseNarration" name="reverseNarration"
                 class="w-full" placeholder="Reason for reversal" />
        </app-form-field>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="reverseDialogVisible = false" />
        <p-button label="Reverse" severity="warn" [loading]="saving()"
                  [disabled]="!reverseNarration"
                  (onClick)="reverseVoucher()" />
      </ng-template>
    </p-dialog>

    <app-audit-log #auditPanel entityType="Voucher" [entityId]="auditEntityId()" />
  `
})
export class AccountingVouchersComponent implements OnInit {
  private readonly http = inject(HttpClient);

  @ViewChild('auditPanel') auditPanel!: AuditLogComponent;
  protected readonly auditEntityId = signal<string | number | null>(null);

  protected readonly labels    = AppLabels;
  protected readonly ddlKeys   = AppConstants.ddlKeys;
  protected readonly loading   = signal(false);
  protected readonly saving    = signal(false);
  protected readonly actionId  = signal<number | null>(null);
  protected readonly allRows   = signal<VoucherRow[]>([]);

  protected searchQuery         = '';
  protected dialogVisible       = false;
  protected reverseDialogVisible = false;
  protected reverseNarration    = '';
  private selectedVoucherId: number | null = null;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.voucherNumber.toLowerCase().includes(q) ||
      r.narration.toLowerCase().includes(q) ||
      r.voucherType.toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.accounting.newVoucher, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.accounting.createVoucher },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected openAuditLog(id: number): void {
    this.auditEntityId.set(id);
    this.auditPanel.open();
  }

  protected statusClass(status: string): string {
    return STATUS_BADGE[status] ?? STATUS_BADGE['*'];
  }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.accounting.newVoucher) {
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected addLine(): void {
    this.form.lines.push({ accountId: null, debitAmount: 0, creditAmount: 0, narration: '' });
  }

  protected removeLine(i: number): void {
    this.form.lines.splice(i, 1);
  }

  protected totalDebit(): number {
    return this.form.lines.reduce((s, l) => s + (l.debitAmount || 0), 0);
  }

  protected totalCredit(): number {
    return this.form.lines.reduce((s, l) => s + (l.creditAmount || 0), 0);
  }

  protected openReverse(row: VoucherRow): void {
    this.selectedVoucherId = row.id;
    this.reverseNarration = '';
    this.reverseDialogVisible = true;
  }

  protected async post(row: VoucherRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.accounting.postVoucher(row.id), {}));
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async reverseVoucher(): Promise<void> {
    if (!this.selectedVoucherId) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.accounting.reverseVoucher(this.selectedVoucherId), {
        narration: this.reverseNarration,
      }));
      this.reverseDialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.accounting.vouchers, {
        voucherType: this.form.voucherType,
        voucherDate: this.form.voucherDate,
        narration:   this.form.narration || null,
        lines:       this.form.lines.map(l => ({
          accountId:    l.accountId,
          debitAmount:  l.debitAmount,
          creditAmount: l.creditAmount,
        })),
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const resp = await firstValueFrom(
        this.http.get<{ items: VoucherRow[] }>(`${ApiEndpoints.accounting.vouchers}?page=1&pageSize=200`)
      );
      this.allRows.set(resp?.items ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return {
      voucherType: null as string | null,
      voucherDate: null as Date | null,
      narration:   '',
      lines:       [
        { accountId: null as number | null, debitAmount: 0, creditAmount: 0, narration: '' },
        { accountId: null as number | null, debitAmount: 0, creditAmount: 0, narration: '' },
      ] as VoucherLine[],
    };
  }
}
