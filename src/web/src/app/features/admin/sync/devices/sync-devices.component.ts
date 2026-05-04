import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CommonModule, DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../../shared/messages/app-api';

interface DeviceDto {
  id: number;
  deviceId: string;
  branchId: number;
  assignedUserId: number;
  type: string;
  platformInfo: string;
  appVersion: string;
  lastSeenAtUtc: string;
  lastSyncedAtUtc: string | null;
  isActive: boolean;
}

const TYPE_ICONS: Record<string, string> = {
  DesktopPos: 'pi pi-desktop',
  MobilePos:  'pi pi-mobile',
  TabletPos:  'pi pi-tablet',
  WebBrowser: 'pi pi-globe',
};

@Component({
  selector: 'app-sync-devices',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, DatePipe, ButtonModule, TooltipModule,
    TagModule, ConfirmDialogModule, ToastModule, PageHeaderComponent,
  ],
  providers: [ConfirmationService, MessageService],
  template: `
    <p-toast />
    <p-confirmDialog />

    <div class="p-6 space-y-6 max-w-5xl mx-auto">
      <app-page-header
        title="Registered Devices"
        subtitle="Manage POS terminals and mobile devices registered to this shop."
        [actions]="[]"
        (actionClick)="void(0)"
      />

      @if (loading()) {
        <div class="flex justify-center py-20">
          <i class="pi pi-spinner pi-spin text-3xl text-slate-400"></i>
        </div>
      } @else if (devices().length === 0) {
        <div class="flex flex-col items-center justify-center py-20 text-slate-400 space-y-3">
          <i class="pi pi-desktop text-5xl"></i>
          <p class="text-lg font-medium">No devices registered</p>
          <p class="text-sm">Devices appear here once a POS app connects for the first time.</p>
        </div>
      } @else {
        <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          @for (d of devices(); track d.id) {
            <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800
                        p-4 space-y-3 hover:shadow-md transition-shadow">
              <!-- Header row -->
              <div class="flex items-start justify-between gap-2">
                <div class="flex items-center gap-2">
                  <div class="w-9 h-9 rounded-lg flex items-center justify-center shrink-0"
                       [class.bg-indigo-100]="d.isActive"
                       [class.bg-slate-100]="!d.isActive">
                    <i [class]="(typeIcon(d.type)) + ' text-sm'"
                       [class.text-indigo-600]="d.isActive"
                       [class.text-slate-400]="!d.isActive"></i>
                  </div>
                  <div>
                    <div class="text-sm font-semibold text-slate-800 dark:text-slate-200 truncate">
                      {{ d.deviceId }}
                    </div>
                    <div class="text-xs text-slate-500">{{ d.type }}</div>
                  </div>
                </div>
                <p-tag
                  [value]="d.isActive ? 'Active' : 'Inactive'"
                  [severity]="d.isActive ? 'success' : 'secondary'" />
              </div>

              <!-- Details -->
              <div class="text-xs text-slate-500 space-y-1">
                <div class="flex justify-between">
                  <span>Platform</span>
                  <span class="text-slate-700 dark:text-slate-300 font-medium truncate max-w-[150px]">
                    {{ d.platformInfo }}
                  </span>
                </div>
                <div class="flex justify-between">
                  <span>App version</span>
                  <span class="text-slate-700 dark:text-slate-300 font-medium">{{ d.appVersion }}</span>
                </div>
                <div class="flex justify-between">
                  <span>Last seen</span>
                  <span class="text-slate-700 dark:text-slate-300 font-medium">
                    {{ d.lastSeenAtUtc | date:'dd MMM, HH:mm' }}
                  </span>
                </div>
                @if (d.lastSyncedAtUtc) {
                  <div class="flex justify-between">
                    <span>Last synced</span>
                    <span class="text-slate-700 dark:text-slate-300 font-medium">
                      {{ d.lastSyncedAtUtc | date:'dd MMM, HH:mm' }}
                    </span>
                  </div>
                }
              </div>

              <!-- Actions -->
              @if (d.isActive) {
                <div class="pt-1 border-t border-slate-100 dark:border-slate-800">
                  <button
                    pButton
                    severity="danger"
                    size="small"
                    label="Deactivate"
                    icon="pi pi-power-off"
                    [outlined]="true"
                    class="w-full"
                    (click)="confirmDeactivate(d)"></button>
                </div>
              }
            </div>
          }
        </div>

        <!-- Stats footer -->
        <div class="flex gap-6 text-sm text-slate-500 pt-2">
          <span><strong class="text-slate-800 dark:text-slate-200">{{ totalDevices() }}</strong> total</span>
          <span><strong class="text-green-600">{{ activeCount() }}</strong> active</span>
          <span><strong class="text-slate-400">{{ totalDevices() - activeCount() }}</strong> inactive</span>
        </div>
      }
    </div>
  `,
})
export class SyncDevicesComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly confirm = inject(ConfirmationService);
  private readonly toast = inject(MessageService);

  protected readonly loading = signal(true);
  protected readonly devices = signal<DeviceDto[]>([]);

  protected readonly totalDevices = computed(() => this.devices().length);
  protected readonly activeCount  = computed(() => this.devices().filter(d => d.isActive).length);

  protected typeIcon(type: string): string {
    return TYPE_ICONS[type] ?? 'pi pi-desktop';
  }

  ngOnInit(): void {
    this.load();
  }

  private async load(): Promise<void> {
    try {
      const list = await firstValueFrom(
        this.http.get<DeviceDto[]>(ApiEndpoints.sync.devices)
      );
      this.devices.set(list);
    } finally {
      this.loading.set(false);
    }
  }

  protected confirmDeactivate(device: DeviceDto): void {
    this.confirm.confirm({
      message: `Deactivate device "${device.deviceId}"? It will no longer be able to sync.`,
      header: 'Deactivate Device',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.deactivate(device),
    });
  }

  private async deactivate(device: DeviceDto): Promise<void> {
    await firstValueFrom(
      this.http.post(`${ApiEndpoints.sync.device(device.id)}/deactivate`, {})
    );
    this.devices.update(list =>
      list.map(d => d.id === device.id ? { ...d, isActive: false } : d)
    );
    this.toast.add({ severity: 'success', summary: 'Device deactivated', life: 3000 });
  }

  protected void(_: unknown): void {}
}
