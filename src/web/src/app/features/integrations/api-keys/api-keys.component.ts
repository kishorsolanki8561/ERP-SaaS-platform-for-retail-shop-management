import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface ApiKeyRow {
  id: number;
  name: string;
  keyPrefix: string;
  scopesCsv: string | null;
  isActive: boolean;
  expiresAtUtc: string | null;
  lastUsedAtUtc: string | null;
  createdAtUtc: string;
}

interface CreateApiKeyResponse {
  id: number;
  rawKey: string;
}

@Component({
  selector: 'app-api-keys',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe, FormsModule,
    TableModule, ButtonModule, DialogModule, InputTextModule, TooltipModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.integration.apiKeysTitle"
        [subtitle]="labels.integration.apiKeysSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <p-table [value]="rows()" [loading]="loading()"
                 styleClass="p-datatable-sm" [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th>{{ labels.integration.keyName }}</th>
              <th style="width: 120px">Prefix</th>
              <th style="width: 160px">Scopes</th>
              <th style="width: 160px">{{ labels.integration.expiresAt }}</th>
              <th style="width: 160px">Last Used</th>
              <th style="width: 80px">{{ labels.integration.status }}</th>
              <th style="width: 80px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ row.name }}</td>
              <td class="font-mono text-sm text-slate-500">{{ row.keyPrefix }}…</td>
              <td class="text-sm text-slate-500">{{ row.scopesCsv ?? 'All' }}</td>
              <td class="tabular-nums text-slate-500 text-sm">
                {{ row.expiresAtUtc ? (row.expiresAtUtc | date:'dd MMM yyyy') : 'Never' }}
              </td>
              <td class="tabular-nums text-slate-500 text-sm">
                {{ row.lastUsedAtUtc ? (row.lastUsedAtUtc | date:'dd MMM yyyy, HH:mm') : 'Never' }}
              </td>
              <td>
                @if (row.isActive) {
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700">Active</span>
                } @else {
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-500">Revoked</span>
                }
              </td>
              <td class="text-right">
                @if (row.isActive) {
                  <button pButton icon="pi pi-ban" class="p-button-sm p-button-text p-button-rounded p-button-danger"
                          pTooltip="{{ labels.integration.revokeKey }}" tooltipPosition="left"
                          (click)="revoke(row)"></button>
                }
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr><td colspan="7">
              <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                  <i class="pi pi-key text-2xl text-slate-300 dark:text-slate-600"></i>
                </div>
                <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">{{ labels.integration.noApiKeys }}</p>
                <p class="text-xs text-slate-400">Create a key to allow external systems to call your API.</p>
              </div>
            </td></tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- Create key dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.integration.createApiKey"
              [modal]="true" [style]="{ width: '440px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field [label]="labels.integration.keyName" [required]="true">
          <input pInputText [(ngModel)]="form.name" name="name" class="w-full"
                 placeholder="e.g. My ERP Connector" />
        </app-form-field>
        <app-form-field [label]="labels.integration.scopes">
          <input pInputText [(ngModel)]="form.scopesCsv" name="scopes" class="w-full"
                 placeholder="invoices:read,products:read (blank = all)" />
        </app-form-field>
        <app-form-field [label]="labels.integration.expiresAt">
          <input pInputText [(ngModel)]="form.expiresAt" name="expiresAt" class="w-full"
                 type="date" />
        </app-form-field>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.integration.createApiKey" [loading]="saving()"
                  [disabled]="!form.name"
                  (onClick)="create()" />
      </ng-template>
    </p-dialog>

    <!-- Raw key display dialog -->
    <p-dialog [(visible)]="rawKeyDialogVisible" header="API Key Created"
              [modal]="true" [style]="{ width: '480px' }" [draggable]="false" [closable]="false">
      <div class="space-y-4 pt-2">
        <p class="text-sm text-amber-700 bg-amber-50 rounded-lg px-4 py-3 border border-amber-200">
          <i class="pi pi-exclamation-triangle mr-2"></i>{{ labels.integration.rawKeyNotice }}
        </p>
        <div class="relative">
          <input pInputText [value]="rawKey()" readonly class="w-full font-mono text-sm pr-10" />
          <button pButton icon="pi pi-copy" class="p-button-text p-button-sm p-button-rounded absolute right-1 top-1"
                  pTooltip="Copy" (click)="copyKey()"></button>
        </div>
      </div>
      <ng-template pTemplate="footer">
        <p-button label="I've copied it" severity="success" (onClick)="rawKeyDialogVisible = false" />
      </ng-template>
    </p-dialog>
  `
})
export class ApiKeysComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels      = AppLabels;
  protected readonly loading     = signal(false);
  protected readonly saving      = signal(false);
  protected readonly rows        = signal<ApiKeyRow[]>([]);
  protected readonly rawKey      = signal('');
  protected dialogVisible        = false;
  protected rawKeyDialogVisible  = false;

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.integration.newApiKey, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.integration.manageApiKeys },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.integration.newApiKey) {
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected async create(): Promise<void> {
    this.saving.set(true);
    try {
      const result = await firstValueFrom(
        this.http.post<CreateApiKeyResponse>(ApiEndpoints.integration.apiKeys, {
          name:        this.form.name,
          scopesCsv:   this.form.scopesCsv || null,
          expiresAtUtc: this.form.expiresAt || null,
        })
      );
      this.dialogVisible = false;
      this.rawKey.set(result.rawKey);
      this.rawKeyDialogVisible = true;
      await this.load();
    } catch { /* handled globally */ }
    finally { this.saving.set(false); }
  }

  protected async revoke(row: ApiKeyRow): Promise<void> {
    try {
      await firstValueFrom(this.http.delete(ApiEndpoints.integration.apiKey(row.id)));
      await this.load();
    } catch { /* handled */ }
  }

  protected copyKey(): void {
    navigator.clipboard?.writeText(this.rawKey()).catch(() => undefined);
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<ApiKeyRow[]>(ApiEndpoints.integration.apiKeys));
      this.rows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return { name: '', scopesCsv: '', expiresAt: '' };
  }
}
