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

export interface TableColumn {
  field: string;
  header: string;
  sortable?: boolean;
  width?: string;
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

@Component({
  selector: 'app-data-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, TooltipModule],
  template: `
    <div class="card">
      <div class="flex justify-between items-center mb-3">
        @if (searchable()) {
          <span class="p-input-icon-left">
            <i class="pi pi-search"></i>
            <input pInputText [ngModel]="searchQuery()" (ngModelChange)="onSearch($event)"
                   placeholder="Search..." class="w-64" />
          </span>
        } @else { <span></span> }

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
        [rowsPerPageOptions]="[10, 25, 50]"
        [loading]="loading()"
        (onLazyLoad)="onLazyLoad($event)"
        [sortField]="currentSortField()"
        [sortOrder]="currentSortOrder()"
        styleClass="p-datatable-sm p-datatable-striped"
        [tableStyle]="{'min-width': '100%'}"
      >
        <ng-template pTemplate="header">
          <tr>
            @for (col of columns(); track col.field) {
              <th [pSortableColumn]="col.sortable ? col.field : ''"
                  [style.width]="col.width ?? 'auto'">
                {{ col.header }}
                @if (col.sortable) { <p-sortIcon [field]="col.field" /> }
              </th>
            }
            @if (rowActions().length) { <th style="width:120px">Actions</th> }
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-row>
          <tr>
            @for (col of columns(); track col.field) {
              <td>{{ row[col.field] }}</td>
            }
            @if (rowActions().length) {
              <td>
                <div class="flex gap-1">
                  @for (action of rowActions(); track action.label) {
                    <p-button [icon]="action.icon ?? ''" [severity]="action.severity ?? 'secondary'"
                              size="small" pTooltip="{{ action.label }}"
                              (onClick)="rowAction.emit({ action: action.label, row })" />
                  }
                </div>
              </td>
            }
          </tr>
        </ng-template>

        <ng-template pTemplate="emptymessage">
          <tr><td [attr.colspan]="columns().length + (rowActions().length ? 1 : 0)"
                  class="text-center p-6 text-surface-500">No records found.</td></tr>
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

  protected readonly rows = signal<T[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);
  protected readonly pageSize = signal(10);
  protected readonly searchQuery = signal('');
  protected readonly currentSortField = signal('');
  protected readonly currentSortOrder = signal(1);

  private currentFirst = 0;

  ngOnInit(): void { this.fetch(0, this.pageSize()); }

  protected onSearch(query: string): void {
    this.searchQuery.set(query);
    this.fetch(0, this.pageSize(), query);
  }

  protected onLazyLoad(event: TableLazyLoadEvent): void {
    const first = event.first ?? 0;
    const rows = event.rows ?? 10;
    this.pageSize.set(rows);
    this.currentSortField.set(String(event.sortField ?? ''));
    this.currentSortOrder.set(event.sortOrder ?? 1);
    this.currentFirst = first;
    this.fetch(first, rows, this.searchQuery(), String(event.sortField ?? ''), event.sortOrder ?? 1);
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
