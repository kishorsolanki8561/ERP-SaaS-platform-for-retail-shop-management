import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn, RowAction } from '../../../shared/components/data-table/data-table.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

@Component({
  selector: 'app-users',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeaderComponent, DataTableComponent],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.admin.usersTitle"
        [subtitle]="labels.admin.usersSubtitle"
        [actions]="headerActions"
        (actionClick)="onAction($event)"
      />
      <app-data-table
        [columns]="columns"
        [apiUrl]="apiUrl"
        [rowActions]="rowActions"
        [searchable]="true"
      />
    </div>
  `
})
export class UsersComponent {
  protected readonly labels = AppLabels;
  protected readonly apiUrl = ApiEndpoints.admin.users;

  protected readonly columns: TableColumn[] = [
    { field: 'displayName', header: 'Name',   sortable: true },
    { field: 'email',       header: 'Email',  sortable: true },
    { field: 'phone',       header: 'Phone' },
    { field: 'isActive',    header: 'Active', width: '80px', type: 'boolean' },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: AppLabels.admin.editUser, icon: 'pi pi-pencil', severity: 'secondary' },
    { label: AppLabels.admin.deactivate, icon: 'pi pi-ban', severity: 'danger' },
  ];

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.admin.inviteUser, icon: 'pi pi-user-plus', severity: 'primary', permission: Permissions.users.invite },
  ];

  protected onAction(action: string): void {
    if (action === AppLabels.admin.inviteUser) { /* TODO: open invite dialog */ }
  }
}
