import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { TagModule } from 'primeng/tag';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface SystemHealthDto {
  errorsLast24h: number;
  hangfireQueueDepth: number;
  dbPingOk: boolean;
  redisPingOk: boolean;
  apiVersion: string;
}

@Component({
  selector: 'app-platform-system-health',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ButtonModule, BadgeModule, TagModule, PageHeaderComponent],
  template: `
    <div class="p-6 space-y-6 max-w-4xl mx-auto">
      <app-page-header
        title="System Health"
        subtitle="Real-time infrastructure status">
        <button pButton icon="pi pi-refresh" label="Refresh" class="p-button-outlined p-button-sm"
          (click)="load()"></button>
      </app-page-header>

      @if (loading()) {
        <div class="flex justify-center py-12"><i class="pi pi-spin pi-spinner text-4xl text-primary-400"></i></div>
      } @else if (data()) {
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">

          <!-- DB Ping -->
          <div class="bg-white rounded-xl border p-5 flex items-center gap-4">
            <div class="w-10 h-10 rounded-full flex items-center justify-center"
              [class]="data()!.dbPingOk ? 'bg-green-100' : 'bg-red-100'">
              <i class="pi pi-database" [class]="data()!.dbPingOk ? 'text-green-600' : 'text-red-600'"></i>
            </div>
            <div>
              <div class="text-sm font-semibold text-gray-700">Database</div>
              <div class="text-xs" [class]="data()!.dbPingOk ? 'text-green-600' : 'text-red-600'">
                {{ data()!.dbPingOk ? 'Connected' : 'Unreachable' }}
              </div>
            </div>
            <div class="ml-auto">
              <p-tag [value]="data()!.dbPingOk ? 'OK' : 'DOWN'"
                [severity]="data()!.dbPingOk ? 'success' : 'danger'"></p-tag>
            </div>
          </div>

          <!-- Redis Ping -->
          <div class="bg-white rounded-xl border p-5 flex items-center gap-4">
            <div class="w-10 h-10 rounded-full flex items-center justify-center"
              [class]="data()!.redisPingOk ? 'bg-green-100' : 'bg-red-100'">
              <i class="pi pi-server" [class]="data()!.redisPingOk ? 'text-green-600' : 'text-red-600'"></i>
            </div>
            <div>
              <div class="text-sm font-semibold text-gray-700">Redis Cache</div>
              <div class="text-xs" [class]="data()!.redisPingOk ? 'text-green-600' : 'text-red-600'">
                {{ data()!.redisPingOk ? 'Connected' : 'Unreachable' }}
              </div>
            </div>
            <div class="ml-auto">
              <p-tag [value]="data()!.redisPingOk ? 'OK' : 'DOWN'"
                [severity]="data()!.redisPingOk ? 'success' : 'danger'"></p-tag>
            </div>
          </div>

          <!-- Errors last 24h -->
          <div class="bg-white rounded-xl border p-5 flex items-center gap-4">
            <div class="w-10 h-10 rounded-full flex items-center justify-center"
              [class]="data()!.errorsLast24h === 0 ? 'bg-green-100' : data()!.errorsLast24h < 10 ? 'bg-amber-100' : 'bg-red-100'">
              <i class="pi pi-exclamation-triangle"
                [class]="data()!.errorsLast24h === 0 ? 'text-green-600' : data()!.errorsLast24h < 10 ? 'text-amber-600' : 'text-red-600'"></i>
            </div>
            <div>
              <div class="text-sm font-semibold text-gray-700">Errors (Last 24h)</div>
              <div class="text-2xl font-bold"
                [class]="data()!.errorsLast24h === 0 ? 'text-green-600' : data()!.errorsLast24h < 10 ? 'text-amber-600' : 'text-red-600'">
                {{ data()!.errorsLast24h }}
              </div>
            </div>
            <div class="ml-auto">
              <p-tag [value]="data()!.errorsLast24h === 0 ? 'Clean' : 'Errors'"
                [severity]="data()!.errorsLast24h === 0 ? 'success' : data()!.errorsLast24h < 10 ? 'warn' : 'danger'"></p-tag>
            </div>
          </div>

          <!-- Queue Depth -->
          <div class="bg-white rounded-xl border p-5 flex items-center gap-4">
            <div class="w-10 h-10 rounded-full flex items-center justify-center"
              [class]="data()!.hangfireQueueDepth < 50 ? 'bg-blue-100' : 'bg-amber-100'">
              <i class="pi pi-list" [class]="data()!.hangfireQueueDepth < 50 ? 'text-blue-600' : 'text-amber-600'"></i>
            </div>
            <div>
              <div class="text-sm font-semibold text-gray-700">Job Queue Depth</div>
              <div class="text-2xl font-bold"
                [class]="data()!.hangfireQueueDepth < 50 ? 'text-blue-600' : 'text-amber-600'">
                {{ data()!.hangfireQueueDepth }}
              </div>
            </div>
            <div class="ml-auto">
              <p-tag [value]="data()!.hangfireQueueDepth < 50 ? 'Normal' : 'Backlog'"
                [severity]="data()!.hangfireQueueDepth < 50 ? 'info' : 'warn'"></p-tag>
            </div>
          </div>
        </div>

        <div class="bg-gray-50 rounded-xl border px-5 py-3 text-sm text-gray-500">
          API Version: <span class="font-mono font-semibold text-gray-700">{{ data()!.apiVersion }}</span>
          &nbsp;·&nbsp; Last refreshed: {{ refreshedAt() | date:'HH:mm:ss' }}
        </div>
      }
    </div>
  `,
})
export class PlatformSystemHealthComponent implements OnInit {
  private http = inject(HttpClient);

  loading = signal(true);
  data = signal<SystemHealthDto | null>(null);
  refreshedAt = signal<Date>(new Date());

  ngOnInit() { this.load(); }

  async load() {
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.http.get<SystemHealthDto>(ApiEndpoints.platform.health)
      );
      this.data.set(result);
      this.refreshedAt.set(new Date());
    } finally {
      this.loading.set(false);
    }
  }
}
