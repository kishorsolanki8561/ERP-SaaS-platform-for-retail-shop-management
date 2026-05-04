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
import { DdlDropdownComponent } from '../../../shared/components/ddl-dropdown/ddl-dropdown.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppConstants } from '../../../shared/messages/app-constants';
import { Permissions } from '../../../shared/messages/app-permissions';

interface MarketplaceAccountRow {
  id: number;
  channelName: string;
  sellerId: string;
  isActive: boolean;
  lastSyncAt?: string;
}

@Component({
  selector: 'app-marketplace-accounts',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule, InputTextModule,
    PageHeaderComponent, FormFieldComponent, DdlDropdownComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.marketplace.accountsTitle"
        [subtitle]="labels.marketplace.accountsSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <p-table [value]="rows()" [loading]="loading()" [paginator]="true" [rows]="20"
                 styleClass="p-datatable-sm" [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th>Channel</th>
              <th style="width: 180px">Seller ID</th>
              <th style="width: 160px">Last Sync</th>
              <th style="width: 80px">Active</th>
              <th style="width: 120px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ row.channelName }}</td>
              <td class="font-mono text-sm text-slate-600">{{ row.sellerId }}</td>
              <td class="tabular-nums text-slate-500">{{ row.lastSyncAt ? (row.lastSyncAt | date:'dd MMM yyyy, HH:mm') : 'Never' }}</td>
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
              <td class="text-right">
                <button pButton icon="pi pi-sync" class="p-button-sm p-button-text p-button-rounded p-button-info"
                        pTooltip="Sync Now" tooltipPosition="left"
                        (click)="syncAccount(row)" [disabled]="syncId() === row.id"></button>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="5">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-globe text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No marketplace accounts connected</p>
                  <p class="text-xs text-slate-400">Connect Amazon, Flipkart or other channels.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New Account dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.marketplace.newAccount"
              [modal]="true" [style]="{ width: '480px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field [label]="labels.marketplace.platform" [required]="true">
          <app-ddl-dropdown [dkey]="ddlKeys.marketplace" [(ngModel)]="form.channelName" name="channel" />
        </app-form-field>
        <app-form-field [label]="labels.marketplace.sellerId" [required]="true">
          <input pInputText [(ngModel)]="form.sellerId" name="sellerId"
                 class="w-full" placeholder="Your seller ID on this platform" />
        </app-form-field>
        <app-form-field [label]="labels.marketplace.apiKey">
          <input pInputText [(ngModel)]="form.apiKey" name="apiKey"
                 class="w-full" placeholder="API key / token (stored encrypted)" />
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.marketplace.newAccount" [loading]="saving()"
                  [disabled]="!form.channelName || !form.sellerId"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>
  `
})
export class MarketplaceAccountsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly ddlKeys  = AppConstants.ddlKeys;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly syncId   = signal<number | null>(null);
  protected readonly allRows  = signal<MarketplaceAccountRow[]>([]);

  protected dialogVisible = false;

  protected readonly rows = computed(() => this.allRows());

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.marketplace.newAccount, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.marketplace.manage },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.marketplace.newAccount) {
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.marketplace.accounts, {
        channelName: this.form.channelName,
        sellerId:    this.form.sellerId,
        apiKey:      this.form.apiKey || null,
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  protected async syncAccount(row: MarketplaceAccountRow): Promise<void> {
    this.syncId.set(row.id);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.marketplace.syncAll, { accountId: row.id }));
      await this.load();
    } catch { /* handled */ }
    finally { this.syncId.set(null); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<MarketplaceAccountRow[]>(ApiEndpoints.marketplace.accounts));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return { channelName: null as string | null, sellerId: '', apiKey: '' };
  }
}
