import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface EndpointRow {
  id: number;
  name: string;
  url: string;
  eventsCsv: string;
  isActive: boolean;
  createdAtUtc: string;
}

interface DeliveryRow {
  deliveryId: string;
  eventCode: string;
  status: string;
  httpStatusCode: number | null;
  attemptCount: number;
  lastAttemptAtUtc: string | null;
}

@Component({
  selector: 'app-webhooks',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe, FormsModule,
    TableModule, ButtonModule, DialogModule, InputTextModule, TooltipModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.integration.webhooksTitle"
        [subtitle]="labels.integration.webhooksSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <!-- Endpoint list -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <p-table [value]="endpoints()" [loading]="loading()"
                 styleClass="p-datatable-sm" [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th>{{ labels.integration.endpointName }}</th>
              <th>{{ labels.integration.endpointUrl }}</th>
              <th style="width: 220px">{{ labels.integration.events }}</th>
              <th style="width: 80px">{{ labels.integration.status }}</th>
              <th style="width: 140px" class="text-right">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-ep>
            <tr>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ ep.name }}</td>
              <td class="font-mono text-sm text-slate-500 break-all">{{ ep.url }}</td>
              <td class="text-sm text-slate-500">{{ ep.eventsCsv }}</td>
              <td>
                @if (ep.isActive) {
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-700">Active</span>
                } @else {
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-500">Inactive</span>
                }
              </td>
              <td class="text-right space-x-1">
                <button pButton icon="pi pi-sync" class="p-button-sm p-button-text p-button-rounded p-button-info"
                        pTooltip="{{ labels.integration.rotateSecret }}" tooltipPosition="left"
                        (click)="rotateSecret(ep)"></button>
                <button pButton icon="pi pi-send" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        pTooltip="{{ labels.integration.testEndpoint }}" tooltipPosition="left"
                        (click)="testEndpoint(ep)"></button>
                <button pButton icon="pi pi-list" class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        pTooltip="{{ labels.integration.viewDeliveries }}" tooltipPosition="left"
                        (click)="viewDeliveries(ep)"></button>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr><td colspan="5">
              <div class="flex flex-col items-center justify-center py-20 gap-3 text-center px-4">
                <div class="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                  <i class="pi pi-link text-2xl text-slate-300 dark:text-slate-600"></i>
                </div>
                <p class="text-sm font-semibold text-slate-600 dark:text-slate-400">{{ labels.integration.noEndpoints }}</p>
                <p class="text-xs text-slate-400">Register a URL to receive real-time event notifications.</p>
              </div>
            </td></tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- Register endpoint dialog -->
    <p-dialog [(visible)]="dialogVisible" [header]="labels.integration.registerEndpoint"
              [modal]="true" [style]="{ width: '480px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field [label]="labels.integration.endpointName" [required]="true">
          <input pInputText [(ngModel)]="form.name" name="name" class="w-full"
                 placeholder="e.g. Order Notifications" />
        </app-form-field>
        <app-form-field [label]="labels.integration.endpointUrl" [required]="true">
          <input pInputText [(ngModel)]="form.url" name="url" class="w-full"
                 placeholder="https://yourapp.com/webhook" />
        </app-form-field>
        <app-form-field [label]="labels.integration.events" [required]="true">
          <input pInputText [(ngModel)]="form.eventsCsv" name="events" class="w-full"
                 placeholder="invoice.finalized,payment.received (or *)" />
        </app-form-field>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="dialogVisible = false" />
        <p-button [label]="labels.integration.registerEndpoint" [loading]="saving()"
                  [disabled]="!form.name || !form.url || !form.eventsCsv"
                  (onClick)="register()" />
      </ng-template>
    </p-dialog>

    <!-- Delivery log dialog -->
    <p-dialog [(visible)]="deliveryDialogVisible" [header]="'Delivery Log — ' + (selectedEndpoint()?.name ?? '')"
              [modal]="true" [style]="{ width: '700px' }" [draggable]="false">
      <p-table [value]="deliveries()" [loading]="deliveriesLoading()"
               styleClass="p-datatable-sm" [tableStyle]="{ 'min-width': '100%' }">
        <ng-template pTemplate="header">
          <tr>
            <th>Event</th>
            <th style="width: 100px">Status</th>
            <th style="width: 60px">HTTP</th>
            <th style="width: 60px">Attempts</th>
            <th style="width: 160px">Last Attempt</th>
            <th style="width: 80px" class="text-right">Actions</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-d>
          <tr>
            <td class="font-mono text-sm text-slate-600">{{ d.eventCode }}</td>
            <td>
              <span [class]="deliveryStatusClass(d.status)">{{ d.status }}</span>
            </td>
            <td class="tabular-nums text-sm text-slate-500">{{ d.httpStatusCode ?? '—' }}</td>
            <td class="tabular-nums text-sm text-slate-500">{{ d.attemptCount }}</td>
            <td class="tabular-nums text-sm text-slate-500">
              {{ d.lastAttemptAtUtc ? (d.lastAttemptAtUtc | date:'dd MMM, HH:mm') : '—' }}
            </td>
            <td class="text-right">
              @if (d.status === 'DeadLettered') {
                <button pButton icon="pi pi-refresh" class="p-button-sm p-button-text p-button-rounded p-button-warning"
                        pTooltip="{{ labels.integration.retryDelivery }}" tooltipPosition="left"
                        (click)="retry(d)"></button>
              }
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr><td colspan="6" class="text-center py-10 text-sm text-slate-400">{{ labels.integration.noDeliveries }}</td></tr>
        </ng-template>
      </p-table>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="deliveryDialogVisible = false" />
      </ng-template>
    </p-dialog>
  `
})
export class WebhooksComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels              = AppLabels;
  protected readonly loading             = signal(false);
  protected readonly saving              = signal(false);
  protected readonly deliveriesLoading   = signal(false);
  protected readonly endpoints           = signal<EndpointRow[]>([]);
  protected readonly deliveries          = signal<DeliveryRow[]>([]);
  protected readonly selectedEndpoint    = signal<EndpointRow | null>(null);
  protected dialogVisible                = false;
  protected deliveryDialogVisible        = false;

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.integration.newEndpoint, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.integration.manageWebhooks },
  ];

  protected form = this.emptyForm();

  ngOnInit(): void { this.load(); }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.integration.newEndpoint) {
      this.form = this.emptyForm();
      this.dialogVisible = true;
    }
  }

  protected async register(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.integration.webhookEndpoints, {
        name:      this.form.name,
        url:       this.form.url,
        eventsCsv: this.form.eventsCsv,
      }));
      this.dialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  protected async rotateSecret(ep: EndpointRow): Promise<void> {
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.integration.rotateSecret(ep.id), {}));
    } catch { /* handled */ }
  }

  protected async testEndpoint(ep: EndpointRow): Promise<void> {
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.integration.testEndpoint(ep.id), {}));
    } catch { /* handled */ }
  }

  protected async viewDeliveries(ep: EndpointRow): Promise<void> {
    this.selectedEndpoint.set(ep);
    this.deliveries.set([]);
    this.deliveryDialogVisible = true;
    this.deliveriesLoading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<DeliveryRow[]>(ApiEndpoints.integration.deliveries(ep.id)));
      this.deliveries.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.deliveriesLoading.set(false); }
  }

  protected async retry(d: DeliveryRow): Promise<void> {
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.integration.retryDelivery(d.deliveryId), {}));
      if (this.selectedEndpoint()) {
        await this.viewDeliveries(this.selectedEndpoint()!);
      }
    } catch { /* handled */ }
  }

  protected deliveryStatusClass(status: string): string {
    const base = 'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ';
    switch (status) {
      case 'Succeeded':    return base + 'bg-emerald-100 text-emerald-700';
      case 'Failed':       return base + 'bg-red-100 text-red-700';
      case 'DeadLettered': return base + 'bg-amber-100 text-amber-700';
      default:             return base + 'bg-slate-100 text-slate-500';
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const rows = await firstValueFrom(this.http.get<EndpointRow[]>(ApiEndpoints.integration.webhookEndpoints));
      this.endpoints.set(rows ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyForm() {
    return { name: '', url: '', eventsCsv: '' };
  }
}
