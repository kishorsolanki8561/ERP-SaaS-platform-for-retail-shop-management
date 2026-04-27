import {
  ChangeDetectionStrategy, Component, inject
} from '@angular/core';
import { Router } from '@angular/router';
import { TagModule } from 'primeng/tag';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn, RowAction } from '../../../shared/components/data-table/data-table.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppRoutePaths } from '../../../shared/messages/app-routes';
import { Permissions } from '../../../shared/messages/app-permissions';

interface ShiftListItem extends Record<string, unknown> {
  id: number;
  cashierName: string;
  openedAtUtc: string;
  status: string;
  totalSales: number;
  transactionCount: number;
  cashVariance: number | null;
}

@Component({
  selector: 'app-shifts',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TagModule, PageHeaderComponent, DataTableComponent],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.shiftsTitle"
        [subtitle]="labels.shiftsSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <app-data-table
        [apiUrl]="listUrl"
        [columns]="columns"
        [rowActions]="rowActions"
        (rowAction)="onRowAction($event)"
      />
    </div>
  `,
})
export class ShiftsComponent {
  private readonly router = inject(Router);

  protected readonly labels  = AppLabels.pos;
  protected readonly listUrl = ApiEndpoints.shift.list;

  protected readonly headerActions: PageAction[] = [
    {
      label:      AppLabels.pos.openShift,
      icon:       'pi pi-play-circle',
      severity:   'success',
      permission: Permissions.shift.open,
    },
  ];

  protected readonly columns: TableColumn[] = [
    { field: 'cashierName',      header: AppLabels.pos.cashier,       sortable: true },
    { field: 'openedAtUtc',      header: AppLabels.pos.openedAt,      sortable: true, width: '160px', type: 'datetime' },
    { field: 'status',           header: AppLabels.common.status,     sortable: true, width: '110px', type: 'status' },
    { field: 'transactionCount', header: AppLabels.pos.transactions,  sortable: true, width: '110px', type: 'number' },
    { field: 'totalSales',       header: AppLabels.pos.totalSales,    sortable: true, width: '130px', type: 'currency' },
    { field: 'cashVariance',     header: AppLabels.pos.cashVariance,  sortable: true, width: '120px', type: 'currency' },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: AppLabels.common.view, icon: 'pi pi-eye' },
  ];

  protected onHeaderAction(label: string): void {
    if (label === AppLabels.pos.openShift) {
      this.router.navigate([AppRoutePaths.pos.openShift]);
    }
  }

  protected onRowAction(event: { action: string; row: ShiftListItem }): void {
    if (event.action === AppLabels.common.view) {
      this.router.navigate([AppRoutePaths.pos.shifts, event.row['id']]);
    }
  }
}
