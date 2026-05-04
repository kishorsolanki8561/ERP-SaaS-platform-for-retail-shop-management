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
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AuditLogComponent } from '../../../shared/components/audit-log/audit-log.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface SupplierRow {
  id: number;
  supplierName: string;
  contactPerson?: string;
  phone?: string;
  email?: string;
  gstNumber?: string;
  isActive: boolean;
}

@Component({
  selector: 'app-suppliers',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule, InputTextModule,
    PageHeaderComponent, FormFieldComponent, AuditLogComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.purchasing.suppliersTitle"
        [subtitle]="labels.purchasing.suppliersSubtitle"
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
              <th>Supplier Name</th>
              <th style="width: 160px">Contact</th>
              <th style="width: 140px">Phone</th>
              <th style="width: 180px">Email</th>
              <th style="width: 140px">GST Number</th>
              <th style="width: 80px">Active</th>
              <th style="width: 100px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ row.supplierName }}</td>
              <td class="text-slate-600">{{ row.contactPerson ?? '—' }}</td>
              <td class="text-slate-600">{{ row.phone ?? '—' }}</td>
              <td class="text-slate-600">{{ row.email ?? '—' }}</td>
              <td class="font-mono text-sm text-slate-600">{{ row.gstNumber ?? '—' }}</td>
              <td>
                @if (row.isActive) {
                  <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-emerald-100 dark:bg-emerald-900/40">
                    <i class="pi pi-check text-emerald-600" style="font-size: 0.5625rem"></i>
                  </span>
                } @else {
                  <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-slate-100 dark:bg-slate-800">
                    <i class="pi pi-times text-slate-400" style="font-size: 0.5625rem"></i>
                  </span>
                }
              </td>
              <td class="text-right">
                <button pButton icon="pi pi-pencil" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        pTooltip="Edit" tooltipPosition="left" (click)="openEdit(row)"></button>
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
                    <i class="pi pi-building text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No suppliers yet</p>
                  <p class="text-xs text-slate-400 dark:text-slate-600">Add your first supplier to get started.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New / Edit Supplier dialog -->
    <p-dialog [(visible)]="dialogVisible"
              [header]="editId() ? labels.purchasing.editSupplier : labels.purchasing.newSupplier"
              [modal]="true" [style]="{ width: '520px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field [label]="labels.purchasing.supplierName" [required]="true">
          <input pInputText [(ngModel)]="form.supplierName" name="supplierName"
                 class="w-full" placeholder="Supplier name" />
        </app-form-field>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.purchasing.contactPerson">
            <input pInputText [(ngModel)]="form.contactPerson" name="contactPerson"
                   class="w-full" placeholder="Contact person" />
          </app-form-field>
          <app-form-field [label]="labels.purchasing.phone">
            <input pInputText [(ngModel)]="form.phone" name="phone" class="w-full" placeholder="+91 9000000000" />
          </app-form-field>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.purchasing.email">
            <input pInputText [(ngModel)]="form.email" name="email" type="email" class="w-full" />
          </app-form-field>
          <app-form-field [label]="labels.purchasing.gstNumber">
            <input pInputText [(ngModel)]="form.gstNumber" name="gstNumber"
                   class="w-full" placeholder="22AAAAA0000A1Z5" />
          </app-form-field>
        </div>
        <app-form-field [label]="labels.purchasing.address">
          <input pInputText [(ngModel)]="form.address" name="address" class="w-full" placeholder="Address" />
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.shared.save" [loading]="saving()"
                  [disabled]="!form.supplierName"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>

    <app-audit-log #auditPanel entityType="Supplier" [entityId]="auditEntityId()" />
  `
})
export class SuppliersComponent implements OnInit {
  private readonly http = inject(HttpClient);

  @ViewChild('auditPanel') auditPanel!: AuditLogComponent;
  protected readonly auditEntityId = signal<string | number | null>(null);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly editId   = signal<number | null>(null);
  protected readonly allRows  = signal<SupplierRow[]>([]);

  protected searchQuery   = '';
  protected dialogVisible = false;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.supplierName.toLowerCase().includes(q) ||
      (r.phone ?? '').includes(q) ||
      (r.gstNumber ?? '').toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.purchasing.newSupplier, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.purchasing.manageSuppliers },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.purchasing.newSupplier) {
      this.form = this.emptyForm();
      this.editId.set(null);
      this.dialogVisible = true;
    }
  }

  protected openAuditLog(id: number): void {
    this.auditEntityId.set(id);
    this.auditPanel.open();
  }

  protected openEdit(row: SupplierRow): void {
    this.form = {
      supplierName:  row.supplierName,
      contactPerson: row.contactPerson ?? '',
      phone:         row.phone ?? '',
      email:         row.email ?? '',
      gstNumber:     row.gstNumber ?? '',
      address:       '',
    };
    this.editId.set(row.id);
    this.dialogVisible = true;
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      const payload = {
        supplierName:  this.form.supplierName,
        contactPerson: this.form.contactPerson || null,
        phone:         this.form.phone || null,
        email:         this.form.email || null,
        gstNumber:     this.form.gstNumber || null,
        address:       this.form.address || null,
      };
      if (this.editId()) {
        await firstValueFrom(this.http.put(ApiEndpoints.purchasing.supplier(this.editId()!), payload));
      } else {
        await firstValueFrom(this.http.post(ApiEndpoints.purchasing.suppliers, payload));
      }
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<SupplierRow[]>(ApiEndpoints.purchasing.suppliers));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return { supplierName: '', contactPerson: '', phone: '', email: '', gstNumber: '', address: '' };
  }
}
