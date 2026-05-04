import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CommonModule, DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../../shared/messages/app-api';

interface SyncExceptionDto {
  id: number;
  clientCommandId: string;
  deviceId: string;
  commandType: string;
  status: string;
  rejectionReason: string | null;
  warningNote: string | null;
  clientTimestampUtc: string;
  receivedAtUtc: string;
}

interface PagedResult {
  items: SyncExceptionDto[];
  totalCount: number;
}

@Component({
  selector: 'app-sync-exceptions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, DatePipe, ButtonModule, TooltipModule,
    TagModule, PaginatorModule, PageHeaderComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-5xl mx-auto">
      <app-page-header
        title="Sync Exceptions"
        subtitle="Review rejected and warning commands from offline POS devices."
        [actions]="[]"
        (actionClick)="void(0)"
      />

      @if (loading()) {
        <div class="flex justify-center py-20">
          <i class="pi pi-spinner pi-spin text-3xl text-slate-400"></i>
        </div>
      } @else if (items().length === 0) {
        <div class="flex flex-col items-center justify-center py-20 text-slate-400 space-y-3">
          <i class="pi pi-check-circle text-5xl text-green-400"></i>
          <p class="text-lg font-medium">No sync exceptions</p>
          <p class="text-sm">All offline commands have applied successfully.</p>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 overflow-hidden">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-700">
                <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Device</th>
                <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Command</th>
                <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Status</th>
                <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Reason</th>
                <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Time</th>
              </tr>
            </thead>
            <tbody>
              @for (ex of items(); track ex.id) {
                <tr class="border-b border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                  <td class="px-4 py-3">
                    <div class="font-medium text-slate-800 dark:text-slate-200">{{ ex.deviceId }}</div>
                    <div class="text-xs text-slate-400 font-mono">{{ ex.clientCommandId | slice:0:8 }}…</div>
                  </td>
                  <td class="px-4 py-3 text-slate-700 dark:text-slate-300">{{ ex.commandType }}</td>
                  <td class="px-4 py-3">
                    <p-tag
                      [value]="ex.status"
                      [severity]="statusSeverity(ex.status)" />
                  </td>
                  <td class="px-4 py-3 text-slate-600 dark:text-slate-400 max-w-xs">
                    <span class="truncate block" [title]="ex.rejectionReason ?? ex.warningNote ?? ''">
                      {{ ex.rejectionReason ?? ex.warningNote ?? '—' }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-slate-500 whitespace-nowrap">
                    {{ ex.receivedAtUtc | date:'dd MMM, HH:mm' }}
                  </td>
                </tr>
              }
            </tbody>
          </table>

          @if (totalCount() > pageSize) {
            <div class="px-4 py-3 border-t border-slate-200 dark:border-slate-700">
              <p-paginator
                [rows]="pageSize"
                [totalRecords]="totalCount()"
                [first]="(page() - 1) * pageSize"
                (onPageChange)="onPage($event)" />
            </div>
          }
        </div>

        <p class="text-sm text-slate-500">
          Showing {{ items().length }} of <strong>{{ totalCount() }}</strong> exceptions
        </p>
      }
    </div>
  `,
})
export class SyncExceptionsComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly loading    = signal(true);
  protected readonly items      = signal<SyncExceptionDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page       = signal(1);
  protected readonly pageSize   = 20;

  ngOnInit(): void {
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const params = new HttpParams()
        .set('page', this.page())
        .set('pageSize', this.pageSize);

      const result = await firstValueFrom(
        this.http.get<PagedResult>(ApiEndpoints.sync.exceptions, { params })
      );
      this.items.set(result.items);
      this.totalCount.set(result.totalCount);
    } finally {
      this.loading.set(false);
    }
  }

  protected onPage(e: PaginatorState): void {
    this.page.set((e.page ?? 0) + 1);
    this.load();
  }

  protected statusSeverity(status: string): 'danger' | 'warn' | 'info' {
    if (status === 'Rejected') return 'danger';
    if (status === 'AppliedWithWarning') return 'warn';
    return 'info';
  }

  protected void(_: unknown): void {}
}
