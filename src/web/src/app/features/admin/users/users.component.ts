import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputSwitchModule } from 'primeng/inputswitch';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { DataTableComponent, TableColumn, RowAction } from '../../../shared/components/data-table/data-table.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';
import { catchError, of } from 'rxjs';

interface InviteForm {
  displayName: string;
  email: string;
  phone: string;
}

interface PermissionStatusDto {
  code: string;
  label: string;
  module: string;
  isFromRole: boolean;
  hasOverride: boolean;
  isGranted: boolean;
}

interface UserPermissionSummary {
  userId: number;
  displayName: string;
  permissions: PermissionStatusDto[];
}

@Component({
  selector: 'app-users',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule, ButtonModule, DialogModule, InputTextModule,
    InputSwitchModule, MessageModule, TagModule, TooltipModule,
    PageHeaderComponent, DataTableComponent,
  ],
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
        (rowAction)="onRowAction($event)"
      />
    </div>

    <!-- Invite User Dialog -->
    <p-dialog
      [(visible)]="showInviteDialog"
      header="Invite User"
      [modal]="true"
      [style]="{ width: '420px' }"
      [closable]="true"
      [draggable]="false">

      <div class="flex flex-col gap-4 pt-2">
        <p-message *ngIf="inviteError()" severity="error" [text]="inviteError()!" />
        <p-message *ngIf="inviteSuccess()" severity="success" text="Invitation sent successfully." />

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Full Name *</label>
          <input pInputText [(ngModel)]="form.displayName" placeholder="e.g. Rahul Sharma"
                 [disabled]="submitting()" class="w-full" />
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Email *</label>
          <input pInputText type="email" [(ngModel)]="form.email" placeholder="user@company.com"
                 [disabled]="submitting()" class="w-full" />
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Phone</label>
          <input pInputText type="tel" [(ngModel)]="form.phone" placeholder="+91 9876543210"
                 [disabled]="submitting()" class="w-full" />
        </div>
      </div>

      <ng-template pTemplate="footer">
        <p-button label="Cancel" severity="secondary" [outlined]="true"
                  (onClick)="closeInviteDialog()" [disabled]="submitting()" />
        <p-button label="Send Invite" icon="pi pi-send" severity="primary"
                  (onClick)="submitInvite()" [loading]="submitting()"
                  [disabled]="!form.displayName || !form.email" />
      </ng-template>
    </p-dialog>

    <!-- User Permissions Dialog -->
    <p-dialog
      [(visible)]="showPermissionsDialog"
      [header]="'Permissions — ' + (permSummary()?.displayName ?? '')"
      [modal]="true"
      [style]="{ width: '680px', maxHeight: '80vh' }"
      [closable]="true"
      [draggable]="false">

      @if (permLoading()) {
        <div class="py-8 text-center text-gray-500">Loading permissions...</div>
      }
      @if (!permLoading() && permSummary()) {
        <div class="flex flex-col gap-1 pt-2 overflow-y-auto" style="max-height: 60vh">
          @for (group of permissionGroups(); track group.module) {
            <div class="mb-4">
              <div class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2 px-1">
                {{ group.module }}
              </div>
              @for (perm of group.permissions; track perm.code) {
                <div class="flex items-center justify-between py-2 px-2 rounded hover:bg-gray-50">
                  <div class="flex flex-col">
                    <span class="text-sm font-medium text-gray-800">{{ perm.label }}</span>
                    <div class="flex gap-1 mt-0.5">
                      @if (perm.isFromRole) {
                        <span class="text-xs px-1.5 py-0.5 rounded bg-blue-50 text-blue-600">Role</span>
                      }
                      @if (perm.hasOverride) {
                        <span class="text-xs px-1.5 py-0.5 rounded bg-amber-50 text-amber-600">Override</span>
                      }
                    </div>
                  </div>
                  <p-inputSwitch
                    [ngModel]="perm.isGranted"
                    (ngModelChange)="setPermissionOverride(perm, $event)"
                    [disabled]="permSaving()"
                  />
                </div>
              }
            </div>
          }
        </div>
        @if (permError()) {
          <p-message severity="error" [text]="permError()!" class="mt-2" />
        }
      }

      <ng-template pTemplate="footer">
        <p-button label="Close" severity="secondary" [outlined]="true" (onClick)="closePermissionsDialog()" />
      </ng-template>
    </p-dialog>
  `
})
export class UsersComponent {
  protected readonly labels = AppLabels;
  protected readonly apiUrl = ApiEndpoints.admin.users;

  private readonly http = inject(HttpClient);

  protected showInviteDialog = false;
  protected readonly submitting = signal(false);
  protected readonly inviteError = signal<string | null>(null);
  protected readonly inviteSuccess = signal(false);

  protected form: InviteForm = { displayName: '', email: '', phone: '' };

  // Permissions dialog state
  protected showPermissionsDialog = false;
  protected readonly permLoading = signal(false);
  protected readonly permSaving = signal(false);
  protected readonly permError = signal<string | null>(null);
  protected readonly permSummary = signal<UserPermissionSummary | null>(null);
  protected readonly permissionGroups = signal<{ module: string; permissions: PermissionStatusDto[] }[]>([]);

  protected readonly columns: TableColumn[] = [
    { field: 'displayName', header: 'Name',   sortable: true },
    { field: 'email',       header: 'Email',  sortable: true },
    { field: 'phone',       header: 'Phone' },
    { field: 'isActive',    header: 'Active', width: '80px', type: 'boolean' },
  ];

  protected readonly rowActions: RowAction[] = [
    { label: AppLabels.admin.editUser,   icon: 'pi pi-pencil', severity: 'secondary' },
    { label: 'Customize Permissions',    icon: 'pi pi-sliders-h', severity: 'info' },
    { label: AppLabels.admin.deactivate, icon: 'pi pi-ban',    severity: 'danger' },
  ];

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.admin.inviteUser, icon: 'pi pi-user-plus', severity: 'primary', permission: Permissions.users.invite },
  ];

  protected onAction(action: string): void {
    if (action === AppLabels.admin.inviteUser) {
      this.openInviteDialog();
    }
  }

  protected onRowAction(event: { action: string; row: Record<string, unknown> }): void {
    if (event.action === 'Customize Permissions') {
      this.openPermissionsDialog(event.row['id'] as number);
    }
  }

  protected openInviteDialog(): void {
    this.form = { displayName: '', email: '', phone: '' };
    this.inviteError.set(null);
    this.inviteSuccess.set(false);
    this.showInviteDialog = true;
  }

  protected closeInviteDialog(): void {
    this.showInviteDialog = false;
  }

  protected submitInvite(): void {
    if (!this.form.displayName.trim() || !this.form.email.trim()) return;

    this.submitting.set(true);
    this.inviteError.set(null);
    this.inviteSuccess.set(false);

    const payload = {
      displayName: this.form.displayName.trim(),
      email:       this.form.email.trim(),
      phone:       this.form.phone.trim() || null,
    };

    this.http.post(ApiEndpoints.admin.inviteUser, payload)
      .pipe(catchError(err => {
        this.inviteError.set(err?.error?.message ?? 'Failed to send invite. Please try again.');
        this.submitting.set(false);
        return of(null);
      }))
      .subscribe(res => {
        if (res !== null) {
          this.inviteSuccess.set(true);
          this.submitting.set(false);
          setTimeout(() => this.closeInviteDialog(), 1500);
        }
      });
  }

  protected openPermissionsDialog(userId: number): void {
    this.permLoading.set(true);
    this.permError.set(null);
    this.permSummary.set(null);
    this.permissionGroups.set([]);
    this.showPermissionsDialog = true;

    this.http.get<UserPermissionSummary>(ApiEndpoints.shopAccess.userPermissions(userId))
      .pipe(catchError(() => of(null)))
      .subscribe(data => {
        this.permLoading.set(false);
        if (data) {
          this.permSummary.set(data);
          const grouped = data.permissions.reduce((acc, p) => {
            const key = p.module;
            let group = acc.find(g => g.module === key);
            if (!group) { group = { module: key, permissions: [] }; acc.push(group); }
            group.permissions.push(p);
            return acc;
          }, [] as { module: string; permissions: PermissionStatusDto[] }[]);
          this.permissionGroups.set(grouped);
        }
      });
  }

  protected closePermissionsDialog(): void {
    this.showPermissionsDialog = false;
  }

  protected setPermissionOverride(perm: PermissionStatusDto, isGranted: boolean): void {
    const userId = this.permSummary()?.userId;
    if (!userId) return;

    this.permSaving.set(true);
    this.permError.set(null);

    const base$ = isGranted === perm.isFromRole && !perm.hasOverride
      ? this.http.delete(ApiEndpoints.shopAccess.userPermission(userId, perm.code))
      : this.http.post(ApiEndpoints.shopAccess.userPermissions(userId), { permissionCode: perm.code, isGranted });

    base$.pipe(catchError(err => {
      this.permError.set(err?.error?.message ?? 'Failed to update permission');
      return of(null);
    })).subscribe(res => {
      this.permSaving.set(false);
      if (res !== null) {
        this.permSummary.update(s => s ? {
          ...s,
          permissions: s.permissions.map(p =>
            p.code === perm.code ? { ...p, isGranted, hasOverride: true } : p
          )
        } : s);
        this.permissionGroups.update(groups => groups.map(g => ({
          ...g,
          permissions: g.permissions.map(p =>
            p.code === perm.code ? { ...p, isGranted, hasOverride: true } : p
          )
        })));
      }
    });
  }
}
