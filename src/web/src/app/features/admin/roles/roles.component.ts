import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface PermissionDto { id: number; code: string; module: string; label: string; }
interface RoleDto { id: number; code: string; label: string; isSystemRole: boolean; permissionCodes: string[]; }

interface PermissionGroup { module: string; items: PermissionDto[]; }

@Component({
  selector: 'app-roles',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    DialogModule, InputTextModule, ButtonModule, CheckboxModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.admin.rolesTitle"
        [subtitle]="labels.admin.rolesSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <div class="space-y-3">
        @for (role of roles(); track role.id) {
          <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200
                      dark:border-slate-800 p-5 transition-shadow hover:shadow-sm">
            <div class="flex items-start justify-between gap-4">
              <div class="min-w-0 flex-1">
                <div class="flex items-center gap-2 flex-wrap">
                  <span class="font-semibold text-slate-900 dark:text-white">{{ role.label }}</span>
                  @if (role.isSystemRole) {
                    <span class="text-xs px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800
                                 text-slate-500 font-medium">System</span>
                  }
                </div>
                <div class="text-xs text-slate-400 mt-0.5 font-mono">{{ role.code }}</div>
                <div class="mt-2.5 flex flex-wrap gap-1.5">
                  @for (code of role.permissionCodes.slice(0, 8); track code) {
                    <span class="text-xs px-2 py-0.5 rounded-full bg-indigo-50 dark:bg-indigo-950/40
                                 text-indigo-600 dark:text-indigo-400 font-mono">{{ code }}</span>
                  }
                  @if (role.permissionCodes.length > 8) {
                    <span class="text-xs text-slate-400 font-medium">+{{ role.permissionCodes.length - 8 }} more</span>
                  }
                  @if (role.permissionCodes.length === 0) {
                    <span class="text-xs text-slate-400 italic">No permissions assigned</span>
                  }
                </div>
              </div>
              @if (!role.isSystemRole) {
                <button class="shrink-0 text-sm font-medium text-indigo-500 hover:text-indigo-700
                               dark:hover:text-indigo-300 transition-colors"
                        (click)="openEditPermissions(role)">
                  {{ labels.admin.editRole }}
                </button>
              }
            </div>
          </div>
        }
        @if (loading()) {
          <div class="flex justify-center py-12">
            <i class="pi pi-spin pi-spinner text-2xl text-primary-500"></i>
          </div>
        }
        @if (!loading() && roles().length === 0) {
          <div class="flex flex-col items-center justify-center py-16 gap-3 text-slate-400
                      bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800">
            <i class="pi pi-shield text-4xl opacity-30"></i>
            <span class="text-sm">No roles found</span>
          </div>
        }
      </div>
    </div>

    <!-- New Role dialog -->
    <p-dialog [(visible)]="createVisible" [header]="labels.admin.newRole"
              [modal]="true" [style]="{ width: '440px' }">
      <form class="space-y-4 pt-2">
        <app-form-field [label]="labels.admin.roleCode" [required]="true">
          <input pInputText [(ngModel)]="newRoleForm.code" name="code"
                 class="w-full font-mono uppercase" placeholder="CUSTOM_MANAGER" />
        </app-form-field>
        <app-form-field [label]="labels.admin.roleLabel" [required]="true">
          <input pInputText [(ngModel)]="newRoleForm.label" name="label" class="w-full" />
        </app-form-field>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary"
                  [outlined]="true" (onClick)="createVisible = false" />
        <p-button [label]="labels.admin.newRole" [loading]="saving()"
                  (onClick)="createRole()" />
      </ng-template>
    </p-dialog>

    <!-- Edit permissions dialog -->
    <p-dialog [(visible)]="editVisible" [header]="labels.admin.editRole + ': ' + (editingRole()?.label ?? '')"
              [modal]="true" [style]="{ width: '600px' }" [maximizable]="true">
      <div class="space-y-4 pt-2 max-h-96 overflow-y-auto">
        @for (group of permissionGroups(); track group.module) {
          <div>
            <div class="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-2">
              {{ group.module }}
            </div>
            <div class="grid grid-cols-2 gap-y-1.5">
              @for (perm of group.items; track perm.id) {
                <label class="flex items-center gap-2 cursor-pointer">
                  <p-checkbox
                    [binary]="true"
                    [(ngModel)]="selectedPerms[perm.code]"
                    [name]="perm.code" />
                  <span class="text-sm text-slate-700 dark:text-slate-300">{{ perm.label }}</span>
                </label>
              }
            </div>
          </div>
        }
      </div>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary"
                  [outlined]="true" (onClick)="editVisible = false" />
        <p-button [label]="labels.shared.save" [loading]="saving()"
                  (onClick)="savePermissions()" />
      </ng-template>
    </p-dialog>
  `
})
export class RolesComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(MessageService);

  protected readonly labels = AppLabels;
  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly editingRole = signal<RoleDto | null>(null);
  protected readonly permissionGroups = signal<PermissionGroup[]>([]);

  protected createVisible = false;
  protected editVisible = false;
  protected newRoleForm = { code: '', label: '' };
  protected selectedPerms: Record<string, boolean> = {};

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.admin.newRole, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.users.manage },
  ];

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadRoles(), this.loadPermissions()]);
  }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.admin.newRole) {
      this.newRoleForm = { code: '', label: '' };
      this.createVisible = true;
    }
  }

  protected openEditPermissions(role: RoleDto): void {
    this.editingRole.set(role);
    this.selectedPerms = {};
    for (const code of role.permissionCodes) this.selectedPerms[code] = true;
    this.editVisible = true;
  }

  protected async createRole(): Promise<void> {
    if (!this.newRoleForm.code || !this.newRoleForm.label) return;
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.http.post(ApiEndpoints.admin.roles, {
          code: this.newRoleForm.code.toUpperCase(),
          label: this.newRoleForm.label,
        })
      );
      this.createVisible = false;
      this.toast.add({ severity: 'success', summary: 'Created', detail: 'Role created.' });
      await this.loadRoles();
    } catch { /* errorInterceptor shows toast */ }
    finally { this.saving.set(false); }
  }

  protected async savePermissions(): Promise<void> {
    const role = this.editingRole();
    if (!role) return;
    this.saving.set(true);
    try {
      const codes = Object.entries(this.selectedPerms)
        .filter(([, v]) => v)
        .map(([k]) => k);
      await firstValueFrom(
        this.http.patch(ApiEndpoints.admin.rolePermissions(role.id), { permissionCodes: codes })
      );
      this.editVisible = false;
      this.toast.add({ severity: 'success', summary: 'Saved', detail: 'Permissions updated.' });
      await this.loadRoles();
    } catch { /* errorInterceptor shows toast */ }
    finally { this.saving.set(false); }
  }

  private async loadRoles(): Promise<void> {
    this.loading.set(true);
    try {
      const data = await firstValueFrom(this.http.get<RoleDto[]>(ApiEndpoints.admin.roles));
      this.roles.set(data ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private async loadPermissions(): Promise<void> {
    try {
      const data = await firstValueFrom(this.http.get<PermissionDto[]>(ApiEndpoints.admin.permissions));
      const groups = new Map<string, PermissionDto[]>();
      for (const p of data ?? []) {
        if (!groups.has(p.module)) groups.set(p.module, []);
        groups.get(p.module)!.push(p);
      }
      this.permissionGroups.set(
        Array.from(groups.entries()).map(([module, items]) => ({ module, items }))
      );
    } catch { /* handled */ }
  }
}
