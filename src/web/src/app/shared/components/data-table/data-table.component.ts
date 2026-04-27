import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, input, output, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AppConstants } from '../../messages/app-constants';
import { AppLabels } from '../../messages/app-messages';

export interface TableColumn {
  field: string;
  header: string;
  sortable?: boolean;
  width?: string;
  /** Drives cell rendering. Default: 'text' */
  type?: 'text' | 'currency' | 'date' | 'datetime' | 'status' | 'boolean' | 'number';
  /** Maps status string → Tailwind badge classes. Key '*' is the fallback. */
  statusMap?: Record<string, string>;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export type PrimeSeverity = 'primary' | 'secondary' | 'success' | 'info' | 'warn' | 'danger' | 'contrast';

export interface RowAction {
  label: string;
  icon?: string;
  severity?: PrimeSeverity;
  permission?: string;
}

const DEFAULT_STATUS_MAP: Record<string, string> = {
  Draft:     'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  Finalized: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-400',
  Paid:      'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-400',
  Cancelled: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-600 dark:bg-red-900/40 dark:text-red-400',
  Open:      'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-400',
  Closed:    'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  Active:    'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-400',
  Inactive:  'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400',
  Credit:    'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-400',
  Debit:     'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-400',
  '*':       'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-600 dark:bg-indigo-900/40 dark:text-indigo-400',
};

@Component({
  selector: 'app-data-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, TooltipModule],
  template: `
    <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
      <!-- Toolbar -->
      <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800">
        @if (searchable()) {
          <div class="relative">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm pointer-events-none"></i>
            <input pInputText [ngModel]="searchQuery()" (ngModelChange)="onSearch($event)"
                   [placeholder]="labels.shared.search"
                   class="pl-9 pr-4 py-2 text-sm rounded-lg border border-slate-200 dark:border-slate-700
                          bg-slate-50 dark:bg-slate-800 text-slate-900 dark:text-slate-100
                          focus:outline-none focus:ring-2 focus:ring-indigo-500 w-64" />
          </div>
        } @else {
          <span></span>
        }

        @if (exportable()) {
          <p-button icon="pi pi-download" label="Export" severity="secondary" size="small"
                    (onClick)="export.emit()" />
        }
      </div>

      <p-table
        [value]="rows()"
        [lazy]="true"
        [totalRecords]="totalCount()"
        [rows]="pageSize()"
        [paginator]="true"
        [rowsPerPageOptions]="pageSizeOptions"
        [loading]="loading()"
        (onLazyLoad)="onLazyLoad($event)"
        [sortField]="currentSortField()"
        [sortOrder]="currentSortOrder()"
        styleClass="p-datatable-sm"
        [tableStyle]="{'min-width': '100%'}"
      >
        <ng-template pTemplate="header">
          <tr class="bg-slate-50 dark:bg-slate-800/60">
            @for (col of columns(); track col.field) {
              <th [pSortableColumn]="col.sortable ? col.field : ''"
                  [style.width]="col.width ?? 'auto'"
                  class="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide px-4 py-3">
                {{ col.header }}
                @if (col.sortable) { <p-sortIcon [field]="col.field" /> }
              </th>
            }
            @if (rowActions().length) {
              <th [style.width]="actionsColWidth"
                  class="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide px-4 py-3 text-right">
                Actions
              </th>
            }
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-row>
          <tr class="border-t border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors">
            @for (col of columns(); track col.field) {
              <td class="px-4 py-3 text-sm text-slate-700 dark:text-slate-300">
                @switch (col.type) {
                  @case ('currency') {
                    <span class="tabular-nums font-medium">
                      {{ (row[col.field] | number:'1.2-2') ? ('₹' + (row[col.field] | number:'1.2-2')) : '—' }}
                    </span>
                  }
                  @case ('date') {
                    <span class="tabular-nums text-slate-500 dark:text-slate-400">
                      {{ row[col.field] ? (row[col.field] | date:'dd MMM yyyy') : '—' }}
                    </span>
                  }
                  @case ('datetime') {
                    <span class="tabular-nums text-slate-500 dark:text-slate-400">
                      {{ row[col.field] ? (row[col.field] | date:'dd MMM yyyy, HH:mm') : '—' }}
                    </span>
                  }
                  @case ('number') {
                    <span class="tabular-nums">{{ row[col.field] ?? '—' }}</span>
                  }
                  @case ('boolean') {
                    @if (row[col.field]) {
                      <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-emerald-100 dark:bg-emerald-900/40">
                        <i class="pi pi-check text-emerald-600 dark:text-emerald-400" style="font-size:0.625rem"></i>
                      </span>
                    } @else {
                      <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-slate-100 dark:bg-slate-800">
                        <i class="pi pi-times text-slate-400" style="font-size:0.625rem"></i>
                      </span>
                    }
                  }
                  @case ('status') {
                    <span [class]="getStatusClass(col, row[col.field])">
                      {{ row[col.field] ?? '—' }}
                    </span>
                  }
                  @default {
                    {{ row[col.field] ?? '—' }}
                  }
                }
              </td>
            }
            @if (rowActions().length) {
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1">
                  @for (action of rowActions(); track action.label) {
                    <p-button [icon]="action.icon ?? ''" [severity]="action.severity ?? 'secondary'"
                              size="small" pTooltip="{{ action.label }}" tooltipPosition="left"
                              [rounded]="true" [text]="true"
                              (onClick)="rowAction.emit({ action: action.label, row })" />
                  }
                </div>
              </td>
            }
          </tr>
        </ng-template>

        <ng-template pTemplate="emptymessage">
          <tr>
            <td [attr.colspan]="columns().length + (rowActions().length ? 1 : 0)">
              <div class="flex flex-col items-center justify-center py-16 gap-3 text-slate-400">
                <i class="pi pi-inbox text-4xl opacity-40"></i>
                <span class="text-sm font-medium">No records found</span>
                @if (searchQuery()) {
                  <span class="text-xs">Try adjusting your search term</span>
                }
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>
    </div>
  `
})
export class DataTableComponent<T extends Record<string, unknown>> implements OnInit {
  readonly columns = input.required<TableColumn[]>();
  readonly apiUrl = input.required<string>();
  readonly rowActions = input<RowAction[]>([]);
  readonly searchable = input(true);
  readonly exportable = input(false);

  readonly rowAction = output<{ action: string; row: T }>();
  readonly export = output();

  private readonly http = inject(HttpClient);

  protected readonly labels = AppLabels;
  protected readonly pageSizeOptions: number[] = [...AppConstants.pagination.pageSizeOptions];
  protected readonly actionsColWidth = AppConstants.table.actionsColumnWidth;

  protected readonly rows = signal<T[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);
  protected readonly pageSize = signal<number>(AppConstants.pagination.defaultPageSize);
  protected readonly searchQuery = signal('');
  protected readonly currentSortField = signal('');
  protected readonly currentSortOrder = signal<number>(AppConstants.table.initialSortOrder);

  private currentFirst = 0;

  ngOnInit(): void { this.fetch(0, this.pageSize()); }

  protected getStatusClass(col: TableColumn, value: unknown): string {
    const v = String(value ?? '');
    const map = col.statusMap ?? DEFAULT_STATUS_MAP;
    return map[v] ?? map['*'] ?? DEFAULT_STATUS_MAP['*'];
  }

  protected onSearch(query: string): void {
    this.searchQuery.set(query);
    this.fetch(0, this.pageSize(), query);
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    const first = event.first ?? 0;
    const rows: number = event.rows ?? AppConstants.pagination.defaultPageSize;
    this.pageSize.set(rows);
    this.currentSortField.set(String(event.sortField ?? ''));
    this.currentSortOrder.set(event.sortOrder ?? AppConstants.table.initialSortOrder);
    this.currentFirst = first;
    this.fetch(first, rows, this.searchQuery(), String(event.sortField ?? ''), event.sortOrder ?? AppConstants.table.initialSortOrder);
  }

  private async fetch(
    first: number, rows: number,
    search = this.searchQuery(),
    sortField = this.currentSortField(),
    sortOrder = this.currentSortOrder()
  ): Promise<void> {
    this.loading.set(true);
    try {
      const params: Record<string, string> = {
        pageNumber: String(Math.floor(first / rows) + 1),
        pageSize: String(rows),
      };
      if (search) params['search'] = search;
      if (sortField) { params['sortBy'] = sortField; params['sortDesc'] = String(sortOrder === -1); }

      const result = await firstValueFrom(
        this.http.get<PagedResult<T>>(this.apiUrl(), { params })
      );
      this.rows.set(result.items);
      this.totalCount.set(result.totalCount);
    } catch { /* error handled by errorInterceptor */ }
    finally { this.loading.set(false); }
  }
}
