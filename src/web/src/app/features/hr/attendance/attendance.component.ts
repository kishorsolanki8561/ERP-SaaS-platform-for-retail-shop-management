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
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectButtonModule } from 'primeng/selectbutton';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface AttendanceRow {
  id: number;
  employeeId: number;
  employeeName: string;
  date: string;
  checkInTime?: string;
  checkOutTime?: string;
  status: string;
  hoursWorked?: number;
}

const STATUS_BADGE: Record<string, string> = {
  Present:  'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700',
  Absent:   'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-red-100 text-red-600',
  HalfDay:  'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-100 text-amber-700',
  Leave:    'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-blue-100 text-blue-700',
  Holiday:  'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
  '*':      'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600',
};

@Component({
  selector: 'app-attendance',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule,
    InputNumberModule, SelectButtonModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.hr.attendanceTitle"
        [subtitle]="labels.hr.attendanceSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <!-- Month/Year filter -->
      <div class="flex items-center gap-3">
        <div class="flex items-center gap-2">
          <label class="text-sm text-slate-600 dark:text-slate-400">Month:</label>
          <select [(ngModel)]="filterMonth" (ngModelChange)="load()"
                  class="text-sm border border-slate-200 dark:border-slate-700 rounded-lg px-3 py-1.5
                         bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200">
            @for (m of months; track m.value) {
              <option [value]="m.value">{{ m.label }}</option>
            }
          </select>
        </div>
        <div class="flex items-center gap-2">
          <label class="text-sm text-slate-600 dark:text-slate-400">Year:</label>
          <select [(ngModel)]="filterYear" (ngModelChange)="load()"
                  class="text-sm border border-slate-200 dark:border-slate-700 rounded-lg px-3 py-1.5
                         bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200">
            @for (y of years; track y) {
              <option [value]="y">{{ y }}</option>
            }
          </select>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <p-table [value]="rows()" [loading]="loading()" [paginator]="true" [rows]="25"
                 [rowsPerPageOptions]="[25, 50, 100]" styleClass="p-datatable-sm"
                 [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th>Employee</th>
              <th style="width: 130px">Date</th>
              <th style="width: 110px">Check-In</th>
              <th style="width: 110px">Check-Out</th>
              <th style="width: 100px">Hours</th>
              <th style="width: 100px">Status</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ row.employeeName }}</td>
              <td class="tabular-nums text-slate-500">{{ row.date | date:'dd MMM yyyy' }}</td>
              <td class="tabular-nums text-slate-600">{{ row.checkInTime ? (row.checkInTime | date:'HH:mm') : '—' }}</td>
              <td class="tabular-nums text-slate-600">{{ row.checkOutTime ? (row.checkOutTime | date:'HH:mm') : '—' }}</td>
              <td class="tabular-nums text-slate-600">{{ row.hoursWorked != null ? (row.hoursWorked | number:'1.1-1') + 'h' : '—' }}</td>
              <td><span [class]="statusClass(row.status)">{{ row.status }}</span></td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="6">
                <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                  <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                    <i class="pi pi-calendar text-2xl text-slate-300 dark:text-slate-600"></i>
                  </div>
                  <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">No attendance records for this period</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- Check-In dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.hr.checkIn"
              [modal]="true" [style]="{ width: '400px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field label="Employee ID" [required]="true">
          <p-inputNumber [(ngModel)]="checkInForm.employeeId" name="employeeId" [min]="1"
                        styleClass="w-full" inputStyleClass="w-full" />
        </app-form-field>
        <app-form-field label="Notes">
          <input pInputText [(ngModel)]="checkInForm.notes" name="notes" class="w-full" placeholder="Optional notes" />
        </app-form-field>
      </form>

      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.hr.checkIn" [loading]="saving()"
                  [disabled]="!checkInForm.employeeId"
                  (onClick)="checkIn()" />
      </ng-template>
    </p-dialog>
  `
})
export class AttendanceComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly loading  = signal(false);
  protected readonly saving   = signal(false);
  protected readonly allRows  = signal<AttendanceRow[]>([]);

  protected dialogVisible = false;

  protected filterMonth = new Date().getMonth() + 1;
  protected filterYear  = new Date().getFullYear();

  protected readonly months = [
    { value: 1, label: 'January'   }, { value: 2,  label: 'February' },
    { value: 3, label: 'March'     }, { value: 4,  label: 'April'    },
    { value: 5, label: 'May'       }, { value: 6,  label: 'June'     },
    { value: 7, label: 'July'      }, { value: 8,  label: 'August'   },
    { value: 9, label: 'September' }, { value: 10, label: 'October'  },
    { value: 11, label: 'November' }, { value: 12, label: 'December' },
  ];

  protected readonly years = Array.from({ length: 5 }, (_, i) => new Date().getFullYear() - i);

  protected readonly rows = computed(() => this.allRows());

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.hr.checkIn, icon: 'pi pi-sign-in', severity: 'primary', permission: Permissions.hr.attendance },
  ];

  protected checkInForm = { employeeId: 0, notes: '' };

  ngOnInit(): void { this.load(); }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.hr.checkIn) {
      this.checkInForm = { employeeId: 0, notes: '' };
      this.dialogVisible = true;
    }
  }

  protected statusClass(status: string): string {
    return STATUS_BADGE[status] ?? STATUS_BADGE['*'];
  }

  protected async checkIn(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.hr.checkIn, {
        employeeId: this.checkInForm.employeeId,
        notes:      this.checkInForm.notes || null,
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(
        this.http.get<AttendanceRow[]>(ApiEndpoints.hr.attendance, {
          params: { year: String(this.filterYear), month: String(this.filterMonth) }
        })
      );
      this.allRows.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }
}
