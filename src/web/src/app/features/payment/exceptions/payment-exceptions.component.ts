import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface ReconciliationExceptionDto {
  id: number;
  gatewayCode: string;
  gatewayTxnId: string | null;
  exceptionType: string;
  status: string;
  ourAmount: number | null;
  gatewayAmount: number | null;
  detectedAtUtc: string;
  resolutionNotes: string | null;
}

interface PagedResult {
  items: ReconciliationExceptionDto[];
}

@Component({
  selector: 'app-payment-exceptions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, DatePipe, FormsModule, ButtonModule, TagModule,
    DialogModule, TextareaModule, ToastModule, PageHeaderComponent,
  ],
  providers: [MessageService],
  template: `
    <p-toast />

    <div class="p-6 space-y-5 max-w-6xl mx-auto">
      <app-page-header
        title="Reconciliation Exceptions"
        subtitle="Payment amounts or settlement timing that don't match gateway records."
        [actions]="[]"
        (actionClick)="void(0)"
      />

      <!-- Filter tabs -->
      <div class="flex gap-2 border-b border-slate-200 dark:border-slate-700">
        @for (tab of ['Open', 'Resolved', 'All']; track tab) {
          <button
            class="px-4 py-2 text-sm font-medium border-b-2 transition-colors"
            [class.border-indigo-600]="activeTab() === tab"
            [class.text-indigo-600]="activeTab() === tab"
            [class.border-transparent]="activeTab() !== tab"
            [class.text-slate-500]="activeTab() !== tab"
            (click)="setTab(tab)">
            {{ tab }}
          </button>
        }
      </div>

      @if (loading()) {
        <div class="flex justify-center py-20">
          <i class="pi pi-spinner pi-spin text-3xl text-slate-400"></i>
        </div>
      } @else if (items().length === 0) {
        <div class="flex flex-col items-center justify-center py-20 text-slate-400 space-y-3">
          <i class="pi pi-check-circle text-5xl text-green-400"></i>
          <p class="text-lg font-medium">No exceptions found</p>
          <p class="text-sm">All reconciliation records match perfectly.</p>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-700">
                  <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Gateway Txn</th>
                  <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Type</th>
                  <th class="px-4 py-3 text-right font-semibold text-slate-600 dark:text-slate-400">Our Amt</th>
                  <th class="px-4 py-3 text-right font-semibold text-slate-600 dark:text-slate-400">Gateway Amt</th>
                  <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Status</th>
                  <th class="px-4 py-3 text-left font-semibold text-slate-600 dark:text-slate-400">Detected</th>
                  <th class="px-4 py-3"></th>
                </tr>
              </thead>
              <tbody>
                @for (ex of items(); track ex.id) {
                  <tr class="border-b border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                    <td class="px-4 py-3">
                      <div class="text-slate-700 dark:text-slate-300">{{ ex.gatewayCode }}</div>
                      <div class="font-mono text-xs text-slate-400 truncate max-w-[140px]">
                        {{ ex.gatewayTxnId ?? '—' }}
                      </div>
                    </td>
                    <td class="px-4 py-3">
                      <p-tag [value]="ex.exceptionType" [severity]="typeSeverity(ex.exceptionType)" />
                    </td>
                    <td class="px-4 py-3 text-right font-medium text-slate-800 dark:text-slate-200">
                      {{ ex.ourAmount != null ? ('₹' + (ex.ourAmount | number:'1.2-2')) : '—' }}
                    </td>
                    <td class="px-4 py-3 text-right font-medium text-slate-800 dark:text-slate-200">
                      {{ ex.gatewayAmount != null ? ('₹' + (ex.gatewayAmount | number:'1.2-2')) : '—' }}
                    </td>
                    <td class="px-4 py-3">
                      <p-tag
                        [value]="ex.status"
                        [severity]="ex.status === 'Resolved' ? 'success' : 'warn'" />
                    </td>
                    <td class="px-4 py-3 text-slate-500 whitespace-nowrap">
                      {{ ex.detectedAtUtc | date:'dd MMM, HH:mm' }}
                    </td>
                    <td class="px-4 py-3">
                      @if (ex.status === 'Open') {
                        <button pButton label="Resolve" icon="pi pi-check"
                          severity="secondary" [outlined]="true" size="small"
                          (click)="openResolve(ex)"></button>
                      } @else if (ex.resolutionNotes) {
                        <span class="text-xs text-slate-400 italic truncate max-w-[100px] block"
                              [title]="ex.resolutionNotes">
                          {{ ex.resolutionNotes }}
                        </span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>

        <p class="text-sm text-slate-500">
          {{ items().length }} {{ activeTab().toLowerCase() }} exception{{ items().length !== 1 ? 's' : '' }}
        </p>
      }
    </div>

    <!-- Resolve dialog -->
    <p-dialog
      [(visible)]="resolveVisible"
      header="Resolve Exception"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      styleClass="w-full max-w-sm"
    >
      <div class="space-y-4 py-2">
        <p class="text-sm text-slate-600 dark:text-slate-400">
          Describe how this exception was resolved. This note will be saved for audit purposes.
        </p>
        <div class="space-y-1.5">
          <label class="text-sm font-medium text-slate-700 dark:text-slate-300">Resolution notes</label>
          <textarea pTextarea [(ngModel)]="resolveNotes"
            rows="3" class="w-full" placeholder="Manually verified with bank statement..."></textarea>
        </div>
      </div>

      <ng-template pTemplate="footer">
        <div class="flex gap-2 justify-end">
          <button pButton label="Cancel" severity="secondary" [outlined]="true"
            (click)="resolveVisible = false"></button>
          <button pButton label="Mark Resolved" icon="pi pi-check"
            [loading]="resolving()"
            [disabled]="!resolveNotes"
            (click)="submitResolve()"></button>
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class PaymentExceptionsComponent implements OnInit {
  private readonly http  = inject(HttpClient);
  private readonly toast = inject(MessageService);

  protected readonly loading   = signal(true);
  protected readonly resolving = signal(false);
  protected readonly items     = signal<ReconciliationExceptionDto[]>([]);
  protected readonly activeTab = signal<string>('Open');

  protected resolveVisible = false;
  protected resolveExId    = 0;
  protected resolveNotes   = '';

  ngOnInit(): void { this.load(); }

  protected setTab(tab: string): void {
    this.activeTab.set(tab);
    this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const status = this.activeTab() === 'All' ? null : this.activeTab();
      const url = status
        ? `${ApiEndpoints.payment.exceptions}?status=${status}`
        : ApiEndpoints.payment.exceptions;
      const result = await firstValueFrom(this.http.get<PagedResult>(url));
      this.items.set(result.items);
    } finally {
      this.loading.set(false);
    }
  }

  protected openResolve(ex: ReconciliationExceptionDto): void {
    this.resolveExId  = ex.id;
    this.resolveNotes = '';
    this.resolveVisible = true;
  }

  protected async submitResolve(): Promise<void> {
    this.resolving.set(true);
    try {
      await firstValueFrom(
        this.http.post(ApiEndpoints.payment.resolveException(this.resolveExId), {
          resolutionNotes: this.resolveNotes,
        })
      );
      this.toast.add({ severity: 'success', summary: 'Exception resolved', life: 3000 });
      this.resolveVisible = false;
      await this.load();
    } catch {
      this.toast.add({ severity: 'error', summary: 'Could not resolve — try again.', life: 4000 });
    } finally {
      this.resolving.set(false);
    }
  }

  protected typeSeverity(type: string): 'danger' | 'warn' | 'info' {
    if (type === 'AmountMismatch')    return 'danger';
    if (type === 'MissingInGateway')  return 'warn';
    if (type === 'MissingInOurDb')    return 'warn';
    if (type === 'FeeUnexpected')     return 'info';
    return 'info';
  }

  protected void(_: unknown): void {}
}
