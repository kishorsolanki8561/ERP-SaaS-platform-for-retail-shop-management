import {
  ChangeDetectionStrategy, Component,
  inject, signal, ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SidebarModule } from 'primeng/sidebar';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn, RowAction } from '../../../shared/components/data-table/data-table.component';
import { AuditLogComponent } from '../../../shared/components/audit-log/audit-log.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface ShopUserDto {
  userId: number;
  displayName: string;
  email: string;
  roleName: string;
  isActive: boolean;
}

interface ShopDetailDto {
  id: number;
  name: string;
  ownerEmail: string;
  planLabel: string;
  planCode: string;
  isActive: boolean;
  createdAtUtc: string;
  userCount: number;
  invoiceCount: number;
  revenueThisMonth: number;
  users: ShopUserDto[];
}

@Component({
  selector: 'app-platform-shops',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    SidebarModule, ButtonModule, BadgeModule,
    PageHeaderComponent, DataTableComponent, AuditLogComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        title="All Shops"
        subtitle="Platform-wide view of every registered shop, their plan, and activity."
        [actions]="[]"
      />

      <app-data-table
        [columns]="columns"
        [apiUrl]="apiUrl"
        [rowActions]="rowActions"
        [searchable]="true"
        (rowAction)="onRowAction($event)"
      />
    </div>

    <!-- Shop detail sidebar -->
    <p-sidebar
      [(visible)]="detailVisible"
      position="right"
      [style]="{ width: '540px' }"
      [closeOnEscape]="true"
    >
      <ng-template pTemplate="header">
        <div class="flex items-center gap-2">
          <i class="pi pi-shop text-indigo-500 text-lg"></i>
          <span class="font-semibold text-slate-800 dark:text-slate-100">
            {{ selectedShop()?.name ?? 'Shop Detail' }}
          </span>
        </div>
      </ng-template>

      @if (detailLoading()) {
        <div class="flex items-center justify-center py-16">
          <i class="pi pi-spin pi-spinner text-2xl text-slate-400"></i>
        </div>
      } @else if (selectedShop()) {
        <!-- Stats row -->
        <div class="grid grid-cols-3 gap-3 mb-6">
          <div class="bg-slate-50 dark:bg-slate-800 rounded-xl p-3 text-center">
            <p class="text-xl font-bold text-slate-800 dark:text-slate-100">{{ selectedShop()!.userCount }}</p>
            <p class="text-xs text-slate-400 mt-0.5">Users</p>
          </div>
          <div class="bg-slate-50 dark:bg-slate-800 rounded-xl p-3 text-center">
            <p class="text-xl font-bold text-slate-800 dark:text-slate-100">{{ selectedShop()!.invoiceCount }}</p>
            <p class="text-xs text-slate-400 mt-0.5">Invoices</p>
          </div>
          <div class="bg-slate-50 dark:bg-slate-800 rounded-xl p-3 text-center">
            <p class="text-xl font-bold text-slate-800 dark:text-slate-100">₹{{ selectedShop()!.revenueThisMonth | number:'1.0-0' }}</p>
            <p class="text-xs text-slate-400 mt-0.5">Revenue MTD</p>
          </div>
        </div>

        <!-- Details -->
        <div class="space-y-2 text-sm mb-5">
          <div class="flex justify-between border-b border-slate-100 dark:border-slate-800 pb-2">
            <span class="text-slate-400">Plan</span>
            <span class="font-medium text-slate-700 dark:text-slate-300">{{ selectedShop()!.planLabel }}</span>
          </div>
          <div class="flex justify-between border-b border-slate-100 dark:border-slate-800 pb-2">
            <span class="text-slate-400">Owner</span>
            <span class="font-medium text-slate-700 dark:text-slate-300">{{ selectedShop()!.ownerEmail }}</span>
          </div>
          <div class="flex justify-between border-b border-slate-100 dark:border-slate-800 pb-2">
            <span class="text-slate-400">Status</span>
            <span [class]="selectedShop()!.isActive
              ? 'inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-100 text-emerald-700'
              : 'inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-red-100 text-red-600'">
              {{ selectedShop()!.isActive ? 'Active' : 'Inactive' }}
            </span>
          </div>
          <div class="flex justify-between border-b border-slate-100 dark:border-slate-800 pb-2">
            <span class="text-slate-400">Registered</span>
            <span class="font-medium text-slate-700 dark:text-slate-300">{{ selectedShop()!.createdAtUtc | date:'dd MMM yyyy' }}</span>
          </div>
        </div>

        <!-- Users table -->
        <h3 class="text-sm font-semibold text-slate-600 dark:text-slate-300 mb-2">Users</h3>
        <div class="rounded-xl border border-slate-100 dark:border-slate-800 overflow-hidden text-xs mb-5">
          <table class="w-full">
            <thead>
              <tr class="bg-slate-50 dark:bg-slate-800 text-left">
                <th class="px-3 py-2 font-medium text-slate-500">Name</th>
                <th class="px-3 py-2 font-medium text-slate-500">Email</th>
                <th class="px-3 py-2 font-medium text-slate-500">Role</th>
                <th class="px-3 py-2 font-medium text-slate-500">Status</th>
              </tr>
            </thead>
            <tbody>
              @for (u of selectedShop()!.users; track u.userId) {
                <tr class="border-t border-slate-100 dark:border-slate-800">
                  <td class="px-3 py-2 text-slate-700 dark:text-slate-200">{{ u.displayName }}</td>
                  <td class="px-3 py-2 text-slate-500">{{ u.email }}</td>
                  <td class="px-3 py-2 text-slate-500">{{ u.roleName }}</td>
                  <td class="px-3 py-2">
                    <span [class]="u.isActive
                      ? 'text-emerald-600 font-medium'
                      : 'text-red-500 font-medium'">
                      {{ u.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                </tr>
              }
              @if (selectedShop()!.users.length === 0) {
                <tr>
                  <td colspan="4" class="px-3 py-4 text-center text-slate-400">No users found.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Audit Log button -->
        <p-button
          label="View Audit Log"
          icon="pi pi-history"
          severity="secondary"
          [outlined]="true"
          styleClass="w-full"
          (onClick)="openAuditLog(selectedShop()!.id)"
        />
      }
    </p-sidebar>

    <!-- Audit log sidebar for shop -->
    <app-audit-log
      #auditPanel
      [entityType]="'Shop'"
      [entityId]="auditShopId()"
    />
  `
})
export class PlatformShopsComponent {
  private readonly http = inject(HttpClient);

  @ViewChild('auditPanel') auditPanel!: AuditLogComponent;

  protected readonly apiUrl = ApiEndpoints.platform.shops;

  protected detailVisible  = false;
  protected readonly detailLoading = signal(false);
  protected readonly selectedShop  = signal<ShopDetailDto | null>(null);
  protected readonly auditShopId   = signal<number | null>(null);

  protected readonly columns: TableColumn[] = [
    { field: 'name',       header: 'Shop Name',    sortable: true },
    { field: 'planLabel',  header: 'Plan',         width: '120px' },
    { field: 'isActive',   header: 'Status',       width: '100px', type: 'boolean' },
    { field: 'userCount',  header: 'Users',        width: '80px',  type: 'number' },
    { field: 'createdAtUtc', header: 'Registered', width: '130px', type: 'date', sortable: true },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: 'View Details', icon: 'pi pi-eye',     severity: 'secondary' },
    { label: 'Audit Log',    icon: 'pi pi-history', severity: 'secondary' },
  ];

  protected onRowAction(event: { action: string; row: Record<string, unknown> }): void {
    const id = event.row['id'] as number;
    if (event.action === 'View Details') {
      this.loadDetail(id);
    } else if (event.action === 'Audit Log') {
      this.openAuditLog(id);
    }
  }

  protected openAuditLog(shopId: number): void {
    this.auditShopId.set(shopId);
    this.detailVisible = false;
    this.auditPanel.open();
  }

  private async loadDetail(shopId: number): Promise<void> {
    this.selectedShop.set(null);
    this.detailVisible = true;
    this.detailLoading.set(true);
    try {
      const [detail, users] = await Promise.all([
        firstValueFrom(this.http.get<ShopDetailDto>(`${ApiEndpoints.platform.shop(shopId)}`)),
        firstValueFrom(this.http.get<ShopUserDto[]>(`${ApiEndpoints.platform.shopUsers(shopId)}`)),
      ]);
      this.selectedShop.set({ ...detail, users: users ?? [] });
    } catch { /* handled by interceptor */ }
    finally { this.detailLoading.set(false); }
  }
}
