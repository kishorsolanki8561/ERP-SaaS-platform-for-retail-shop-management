import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CalendarModule } from 'primeng/calendar';
import { DropdownModule } from 'primeng/dropdown';
import { ButtonModule } from 'primeng/button';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn, RowAction } from '../../../shared/components/data-table/data-table.component';
import { AuditLogComponent } from '../../../shared/components/audit-log/audit-log.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface DdlItem { value: string; label: string; }

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    CalendarModule, DropdownModule, ButtonModule,
    PageHeaderComponent, DataTableComponent, AuditLogComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        title="Audit Logs"
        subtitle="Track every change made to your data — who did it, when, and what changed."
        [actions]="[]"
      />

      <!-- Filters -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm p-4">
        <div class="flex flex-wrap items-end gap-3">
          <div class="flex flex-col gap-1 min-w-[180px]">
            <label class="text-xs font-medium text-slate-500">Entity Type</label>
            <p-dropdown
              [options]="entityTypeOptions()"
              [(ngModel)]="filterEntityType"
              placeholder="All entities"
              [showClear]="true"
              optionLabel="label"
              optionValue="value"
              styleClass="w-full text-sm"
            />
          </div>

          <div class="flex flex-col gap-1 min-w-[160px]">
            <label class="text-xs font-medium text-slate-500">From Date</label>
            <p-calendar [(ngModel)]="filterFrom" dateFormat="dd/mm/yy"
                        [showButtonBar]="true" styleClass="w-full text-sm" />
          </div>

          <div class="flex flex-col gap-1 min-w-[160px]">
            <label class="text-xs font-medium text-slate-500">To Date</label>
            <p-calendar [(ngModel)]="filterTo" dateFormat="dd/mm/yy"
                        [showButtonBar]="true" styleClass="w-full text-sm" />
          </div>

          <p-button label="Search" icon="pi pi-search"
                    styleClass="h-9 text-sm"
                    (onClick)="applyFilters()" />

          <p-button label="Clear" icon="pi pi-times"
                    severity="secondary" [outlined]="true"
                    styleClass="h-9 text-sm"
                    (onClick)="clearFilters()" />
        </div>
      </div>

      <!-- Table -->
      <app-data-table
        [columns]="columns"
        [apiUrl]="currentApiUrl()"
        [rowActions]="rowActions"
        [searchable]="false"
        (rowAction)="onRowAction($event)"
      />
    </div>

    <!-- Audit log sidebar for drill-down -->
    <app-audit-log
      #auditPanel
      [entityType]="auditEntityType()"
      [entityId]="auditEntityId()"
    />
  `
})
export class AuditLogsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  @ViewChild('auditPanel') auditPanel!: AuditLogComponent;

  protected filterEntityType: string | null = null;
  protected filterFrom: Date | null = null;
  protected filterTo: Date | null = null;

  protected readonly entityTypeOptions = signal<DdlItem[]>([]);
  protected readonly currentApiUrl     = signal<string>(ApiEndpoints.admin.auditLogs);
  protected readonly auditEntityType   = signal('');
  protected readonly auditEntityId     = signal<string | number | null>(null);

  protected readonly columns: TableColumn[] = [
    { field: 'occurredAtUtc',  header: 'Date / Time',    width: '160px', sortable: true, type: 'datetime' },
    { field: 'entityName',     header: 'Entity',         width: '140px', sortable: true },
    { field: 'entityId',       header: 'Record ID',      width: '100px' },
    { field: 'eventType',      header: 'Action',         width: '110px', type: 'status' },
    { field: 'changedByName',  header: 'Changed By',     sortable: true },
    { field: 'changedFields',  header: 'Fields Changed', width: '120px', type: 'number' },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: 'View Detail', icon: 'pi pi-eye', severity: 'secondary' },
  ];

  async ngOnInit(): Promise<void> {
    try {
      const items = await firstValueFrom(
        this.http.get<DdlItem[]>(ApiEndpoints.ddl.single('AUDIT_ENTITY_TYPE'))
      );
      this.entityTypeOptions.set(items ?? []);
    } catch { /* silently ignore — table still works */ }
  }

  protected applyFilters(): void {
    const params = new URLSearchParams();
    if (this.filterEntityType) params.set('entityType', this.filterEntityType);
    if (this.filterFrom) params.set('from', this.filterFrom.toISOString().split('T')[0]);
    if (this.filterTo)   params.set('to',   this.filterTo.toISOString().split('T')[0]);
    const qs = params.toString();
    this.currentApiUrl.set(qs ? `${ApiEndpoints.admin.auditLogs}?${qs}` : ApiEndpoints.admin.auditLogs);
  }

  protected clearFilters(): void {
    this.filterEntityType = null;
    this.filterFrom = null;
    this.filterTo   = null;
    this.currentApiUrl.set(ApiEndpoints.admin.auditLogs);
  }

  protected onRowAction(event: { action: string; row: Record<string, unknown> }): void {
    if (event.action === 'View Detail') {
      this.auditEntityType.set(String(event.row['entityName'] ?? ''));
      this.auditEntityId.set(String(event.row['entityId'] ?? ''));
      this.auditPanel.open();
    }
  }
}
