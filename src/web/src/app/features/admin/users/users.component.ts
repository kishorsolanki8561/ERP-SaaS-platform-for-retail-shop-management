import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn, RowAction, PrimeSeverity } from '../../../shared/components/data-table/data-table.component';

@Component({
  selector: 'app-users',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeaderComponent, DataTableComponent],
  template: `
    <app-page-header
      title="Users"
      subtitle="Manage staff accounts for this shop."
      [actions]="headerActions"
      (actionClick)="onAction($event)"
    />
    <app-data-table
      [columns]="columns"
      apiUrl="/api/admin/users"
      [rowActions]="rowActions"
      [searchable]="true"
    />
  `
})
export class UsersComponent {
  protected readonly columns: TableColumn[] = [
    { field: 'displayName', header: 'Name', sortable: true },
    { field: 'email', header: 'Email', sortable: true },
    { field: 'phone', header: 'Phone' },
    { field: 'isActive', header: 'Active', width: '80px' },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: 'Edit', icon: 'pi pi-pencil', severity: 'secondary' },
    { label: 'Deactivate', icon: 'pi pi-ban', severity: 'danger' },
  ];

  protected readonly headerActions: PageAction[] = [
    { label: 'Invite User', icon: 'pi pi-user-plus', severity: 'primary', permission: 'Users.Invite' },
  ];

  protected onAction(action: string): void {
    if (action === 'Invite User') { /* TODO: open invite dialog */ }
  }
}
