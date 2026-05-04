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
import { AuditLogComponent } from '../../../shared/components/audit-log/audit-log.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface EmployeeRow {
  id: number;
  employeeCode: string;
  fullName: string;
  designation?: string;
  department?: string;
  mobileNumber?: string;
  joiningDate: string;
  isActive: boolean;
}

@Component({
  selector: 'app-employees',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, CalendarModule,
    PageHeaderComponent, FormFieldComponent, AuditLogComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.hr.employeesTitle"
        [subtitle]="labels.hr.employeesSubtitle"
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
              <th style="width: 120px">Code</th>
              <th>Name</th>
              <th style="width: 150px">Designation</th>
              <th style="width: 150px">Department</th>
              <th style="width: 140px">Mobile</th>
              <th style="width: 130px">Joining Date</th>
              <th style="width: 80px">Active</th>
              <th style="width: 80px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm font-semibold text-slate-700">{{ row.employeeCode }}</td>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ row.fullName }}</td>
              <td class="text-slate-600">{{ row.designation ?? '—' }}</td>
              <td class="text-slate-600">{{ row.department ?? '—' }}</td>
              <td class="text-slate-600">{{ row.mobileNumber ?? '—' }}</td>
              <td class="tabular-nums text-slate-500">{{ row.joiningDate | date:'dd MMM yyyy' }}</td>
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
                <button pButton icon="pi pi-pencil" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        pTooltip="Edit" tooltipPosition="left" (click)="openEdit(row)"></button>
                <button pButton icon="pi pi-history" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        pTooltip="Audit Log" tooltipPosition="left" (click)="openAuditLog(row.id)"></button>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="8">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-users text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No employees yet</p>
                  <p class="text-xs text-slate-400 dark:text-slate-600">Add your first employee record.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New / Edit Employee dialog -->
    <p-dialog [(visible)]="dialogVisible"
              [header]="editId() ? labels.hr.editEmployee : labels.hr.newEmployee"
              [modal]="true" [style]="{ width: '580px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.hr.employeeCode" [required]="true">
            <input pInputText [(ngModel)]="form.employeeCode" name="employeeCode"
                   class="w-full" placeholder="EMP001" />
          </app-form-field>
          <app-form-field [label]="labels.hr.employeeName" [required]="true">
            <input pInputText [(ngModel)]="form.fullName" name="fullName"
                   class="w-full" placeholder="Full name" />
          </app-form-field>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.hr.designation">
            <input pInputText [(ngModel)]="form.designation" name="designation"
                   class="w-full" placeholder="e.g. Sales Executive" />
          </app-form-field>
          <app-form-field [label]="labels.hr.department">
            <input pInputText [(ngModel)]="form.department" name="department"
                   class="w-full" placeholder="e.g. Sales" />
          </app-form-field>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field [label]="labels.hr.mobileNumber">
            <input pInputText [(ngModel)]="form.mobileNumber" name="mobileNumber"
                   class="w-full" placeholder="+91 9000000000" />
          </app-form-field>
          <app-form-field [label]="labels.hr.joiningDate" [required]="true">
            <p-calendar [(ngModel)]="form.joiningDate" name="joiningDate" dateFormat="dd/mm/yy"
                       styleClass="w-full" inputStyleClass="w-full" [showIcon]="true" />
          </app-form-field>
        </div>
        <app-form-field [label]="labels.hr.basicSalary">
          <p-inputNumber [(ngModel)]="form.basicSalary" name="basicSalary" [min]="0"
                        mode="decimal" [minFractionDigits]="2"
                        styleClass="w-full" inputStyleClass="w-full" />
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.shared.save" [loading]="saving()"
                  [disabled]="!form.employeeCode || !form.fullName || !form.joiningDate"
                  (onClick)="save()" />
      </ng-template>
    </p-dialog>

    <app-audit-log #auditPanel entityType="Employee" [entityId]="auditEntityId()" />
  `
})
export class EmployeesComponent implements OnInit {
  private readonly http = inject(HttpClient);

  @ViewChild('auditPanel') auditPanel!: AuditLogComponent;
  protected readonly auditEntityId = signal<string | number | null>(null);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly editId   = signal<number | null>(null);
  protected readonly allRows  = signal<EmployeeRow[]>([]);

  protected searchQuery   = '';
  protected dialogVisible = false;

  protected readonly rows = computed(() => {
    const q = this.searchQuery.toLowerCase();
    if (!q) return this.allRows();
    return this.allRows().filter(r =>
      r.fullName.toLowerCase().includes(q) ||
      r.employeeCode.toLowerCase().includes(q) ||
      (r.designation ?? '').toLowerCase().includes(q)
    );
  });

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.hr.newEmployee, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.hr.manage },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onSearch(q: string): void { this.searchQuery = q; }

  protected openAuditLog(id: number): void {
    this.auditEntityId.set(id);
    this.auditPanel.open();
  }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.hr.newEmployee) {
      this.form = this.emptyForm();
      this.editId.set(null);
      this.dialogVisible = true;
    }
  }

  protected openEdit(row: EmployeeRow): void {
    this.form = {
      employeeCode: row.employeeCode,
      fullName:     row.fullName,
      designation:  row.designation ?? '',
      department:   row.department ?? '',
      mobileNumber: row.mobileNumber ?? '',
      joiningDate:  new Date(row.joiningDate),
      basicSalary:  0,
    };
    this.editId.set(row.id);
    this.dialogVisible = true;
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      const payload = {
        employeeCode: this.form.employeeCode,
        fullName:     this.form.fullName,
        designation:  this.form.designation || null,
        department:   this.form.department || null,
        mobileNumber: this.form.mobileNumber || null,
        joiningDate:  this.form.joiningDate instanceof Date
          ? this.form.joiningDate.toISOString()
          : new Date().toISOString(),
        basicSalary:  this.form.basicSalary,
      };
      if (this.editId()) {
        await firstValueFrom(this.http.patch(ApiEndpoints.hr.employee(this.editId()!), payload));
      } else {
        await firstValueFrom(this.http.post(ApiEndpoints.hr.employees, payload));
      }
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<EmployeeRow[]>(ApiEndpoints.hr.employees));
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return {
      employeeCode: '',
      fullName:     '',
      designation:  '',
      department:   '',
      mobileNumber: '',
      joiningDate:  null as Date | null,
      basicSalary:  0,
    };
  }
}
