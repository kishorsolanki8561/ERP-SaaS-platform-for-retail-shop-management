import {
  ChangeDetectionStrategy, Component,
  inject, input, signal, effect
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SidebarModule } from 'primeng/sidebar';
import { ButtonModule } from 'primeng/button';
import { ApiEndpoints } from '../../messages/app-api';

interface AuditChangedField {
  field: string;
  displayName: string;
  oldValue: string | null;
  newValue: string | null;
}

interface AuditLogEntryDto {
  id: number;
  eventType: string;
  entityName: string;
  entityId: string | null;
  parentEntityName: string | null;
  parentEntityId: string | null;
  changedByUserId: number | null;
  changedByName: string;
  occurredAtUtc: string;
  changedFields: AuditChangedField[];
}

interface AuditLogPagedDto {
  items: AuditLogEntryDto[];
  totalCount: number;
}

@Component({
  selector: 'app-audit-log',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, SidebarModule, ButtonModule],
  template: `
    <p-sidebar
      [(visible)]="visible"
      position="right"
      [style]="{ width: '520px' }"
      [closeOnEscape]="true"
    >
      <ng-template pTemplate="header">
        <div class="flex items-center gap-2">
          <i class="pi pi-history text-indigo-500 text-lg"></i>
          <span class="font-semibold text-slate-800 dark:text-slate-100">Audit Log</span>
          @if (entityId()) {
            <span class="text-xs text-slate-400 font-normal ml-1">— {{ entityType() }} #{{ entityId() }}</span>
          }
        </div>
      </ng-template>

      @if (loading()) {
        <div class="flex items-center justify-center py-16">
          <i class="pi pi-spin pi-spinner text-2xl text-slate-400"></i>
        </div>
      } @else if (entries().length === 0) {
        <div class="text-center py-16 text-slate-400 text-sm">No audit records found.</div>
      } @else {

        <!-- Summary row: created by + last modified by -->
        @if (createdEntry(); as ce) {
          <div class="mx-1 mb-4 p-3 bg-slate-50 dark:bg-slate-800 rounded-xl text-xs text-slate-500 dark:text-slate-400 space-y-0.5">
            <div><span class="font-medium text-slate-600 dark:text-slate-300">Created by</span> {{ ce.changedByName }} on {{ ce.occurredAtUtc | date:'dd MMM yyyy, h:mm a' }}</div>
            @if (lastModifiedEntry(); as lm) {
              @if (lm.id !== ce.id) {
                <div><span class="font-medium text-slate-600 dark:text-slate-300">Last modified by</span> {{ lm.changedByName }} on {{ lm.occurredAtUtc | date:'dd MMM yyyy, h:mm a' }}</div>
              }
            }
          </div>
        }

        <!-- Timeline -->
        <ol class="relative border-l border-slate-200 dark:border-slate-700 ml-3">
          @for (entry of entries(); track entry.id) {
            <li class="mb-6 ml-5">
              <!-- Dot -->
              <span [class]="dotClass(entry.eventType)"
                    class="absolute -left-2 flex items-center justify-center w-4 h-4 rounded-full ring-2 ring-white dark:ring-slate-950">
              </span>

              <!-- Header row -->
              <div class="flex items-start justify-between gap-2 mb-1">
                <div class="flex items-center gap-2 flex-wrap">
                  <span [class]="badgeClass(entry.eventType)"
                        class="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold">
                    {{ eventLabel(entry.eventType) }}
                  </span>
                  @if (entry.parentEntityName) {
                    <span class="text-xs text-slate-400">
                      {{ entry.parentEntityName }} line
                    </span>
                  }
                  <span class="text-xs font-medium text-slate-600 dark:text-slate-300">{{ entry.changedByName }}</span>
                </div>
                <time class="text-xs text-slate-400 whitespace-nowrap">
                  {{ entry.occurredAtUtc | date:'dd MMM, h:mm a' }}
                </time>
              </div>

              <!-- Changed fields diff -->
              @if (entry.changedFields.length > 0) {
                <div class="mt-2 rounded-lg border border-slate-100 dark:border-slate-800 overflow-hidden text-xs">
                  <table class="w-full">
                    <thead>
                      <tr class="bg-slate-50 dark:bg-slate-800 text-left">
                        <th class="px-3 py-1.5 font-medium text-slate-500 dark:text-slate-400 w-1/3">Field</th>
                        <th class="px-3 py-1.5 font-medium text-slate-500 dark:text-slate-400 w-1/3">Before</th>
                        <th class="px-3 py-1.5 font-medium text-slate-500 dark:text-slate-400 w-1/3">After</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (f of entry.changedFields; track f.field) {
                        <tr class="border-t border-slate-100 dark:border-slate-800">
                          <td class="px-3 py-1.5 text-slate-600 dark:text-slate-300 font-medium">{{ f.displayName }}</td>
                          <td class="px-3 py-1.5 text-slate-400 dark:text-slate-500">{{ f.oldValue ?? '—' }}</td>
                          <td class="px-3 py-1.5 text-slate-700 dark:text-slate-200">{{ f.newValue ?? '—' }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              }
            </li>
          }
        </ol>

        @if (hasMore()) {
          <div class="text-center mt-2">
            <button (click)="loadMore()"
                    class="text-xs text-indigo-500 hover:text-indigo-700 underline underline-offset-2 transition">
              Load more
            </button>
          </div>
        }
      }
    </p-sidebar>
  `
})
export class AuditLogComponent {
  private readonly http = inject(HttpClient);

  readonly entityType = input.required<string>();
  readonly entityId   = input<string | number | null>(null);

  protected visible = false;
  protected readonly loading  = signal(false);
  protected readonly entries  = signal<AuditLogEntryDto[]>([]);
  protected readonly total    = signal(0);
  protected readonly page     = signal(1);

  protected readonly createdEntry = () =>
    [...this.entries()].reverse().find(e => e.eventType === 'Insert' || e.eventType === 'Created');

  protected readonly lastModifiedEntry = () =>
    this.entries().find(e => e.eventType === 'Update' || e.eventType === 'Updated');

  protected readonly hasMore = () => this.entries().length < this.total();

  constructor() {
    effect(() => {
      const id = this.entityId();
      if (this.visible && id != null) {
        this.page.set(1);
        this.entries.set([]);
        this.fetch();
      }
    });
  }

  open(): void {
    this.visible = true;
    if (this.entityId() != null && this.entries().length === 0) {
      this.fetch();
    }
  }

  protected loadMore(): void {
    this.page.set(this.page() + 1);
    this.fetch(true);
  }

  private async fetch(append = false): Promise<void> {
    const id = this.entityId();
    if (!id) return;
    this.loading.set(true);
    try {
      const params = new URLSearchParams({
        entityType: this.entityType(),
        entityId:   String(id),
        page:       String(this.page()),
        pageSize:   '20',
      });
      const result = await firstValueFrom(
        this.http.get<AuditLogPagedDto>(`${ApiEndpoints.admin.auditLogs}?${params}`)
      );
      this.total.set(result.totalCount);
      if (append) {
        this.entries.update(prev => [...prev, ...result.items]);
      } else {
        this.entries.set(result.items);
      }
    } catch { /* handled by interceptor */ }
    finally { this.loading.set(false); }
  }

  protected dotClass(eventType: string): string {
    if (eventType === 'Insert' || eventType === 'Created')
      return 'bg-emerald-400';
    if (eventType === 'Delete' || eventType === 'Deleted')
      return 'bg-red-400';
    return 'bg-indigo-400';
  }

  protected badgeClass(eventType: string): string {
    if (eventType === 'Insert' || eventType === 'Created')
      return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-400';
    if (eventType === 'Delete' || eventType === 'Deleted')
      return 'bg-red-100 text-red-600 dark:bg-red-900/40 dark:text-red-400';
    return 'bg-indigo-100 text-indigo-600 dark:bg-indigo-900/40 dark:text-indigo-400';
  }

  protected eventLabel(eventType: string): string {
    if (eventType === 'Insert' || eventType === 'Created') return 'Created';
    if (eventType === 'Delete' || eventType === 'Deleted') return 'Deleted';
    return 'Updated';
  }
}
