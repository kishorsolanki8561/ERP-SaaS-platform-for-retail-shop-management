import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ApiEndpoints } from '../../../shared/messages/app-api';

interface GatewayAccountDto {
  gatewayCode: string;
  isActive: boolean;
  isDefault: boolean;
}

interface GatewayDef {
  code: string;
  label: string;
  icon: string;
  description: string;
}

const GATEWAYS: GatewayDef[] = [
  { code: 'Simulated', label: 'Simulated',  icon: 'pi pi-cog',        description: 'Built-in test gateway — no credentials needed.' },
  { code: 'Razorpay',  label: 'Razorpay',   icon: 'pi pi-credit-card', description: 'India\'s leading payment gateway.' },
  { code: 'Stripe',    label: 'Stripe',      icon: 'pi pi-credit-card', description: 'Global card & wallet processing.' },
  { code: 'PhonePe',   label: 'PhonePe',     icon: 'pi pi-mobile',      description: 'UPI-first payments for India.' },
  { code: 'Paytm',     label: 'Paytm',       icon: 'pi pi-wallet',      description: 'Wallet and UPI gateway.' },
];

@Component({
  selector: 'app-payment-gateways',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule, ButtonModule, TagModule, DialogModule,
    InputTextModule, CheckboxModule, ToastModule, PageHeaderComponent,
  ],
  providers: [MessageService],
  template: `
    <p-toast />

    <div class="p-6 space-y-6 max-w-4xl mx-auto">
      <app-page-header
        title="Payment Gateways"
        subtitle="Connect a payment gateway to start accepting online payments. Unconfigured gateways use the built-in simulator."
        [actions]="[]"
        (actionClick)="void(0)"
      />

      @if (loading()) {
        <div class="flex justify-center py-20">
          <i class="pi pi-spinner pi-spin text-3xl text-slate-400"></i>
        </div>
      } @else {
        <div class="grid gap-4 sm:grid-cols-2">
          @for (gw of GATEWAYS; track gw.code) {
            <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800 p-5 space-y-4
                        hover:shadow-md transition-shadow">
              <div class="flex items-start justify-between gap-3">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg flex items-center justify-center shrink-0"
                       [class.bg-indigo-100]="isConfigured(gw.code)"
                       [class.bg-slate-100]="!isConfigured(gw.code)">
                    <i [class]="gw.icon + ' text-base'"
                       [class.text-indigo-600]="isConfigured(gw.code)"
                       [class.text-slate-400]="!isConfigured(gw.code)"></i>
                  </div>
                  <div>
                    <div class="font-semibold text-slate-800 dark:text-slate-200">{{ gw.label }}</div>
                    <div class="text-xs text-slate-500 mt-0.5">{{ gw.description }}</div>
                  </div>
                </div>
                <p-tag
                  [value]="isConfigured(gw.code) ? 'Connected' : 'Not configured'"
                  [severity]="isConfigured(gw.code) ? 'success' : 'secondary'"
                  class="shrink-0" />
              </div>

              @if (gw.code !== 'Simulated') {
                <div class="pt-3 border-t border-slate-100 dark:border-slate-800">
                  <button
                    pButton
                    size="small"
                    [label]="isConfigured(gw.code) ? 'Update credentials' : 'Configure'"
                    icon="pi pi-pencil"
                    [outlined]="true"
                    class="w-full"
                    (click)="openDialog(gw.code)"></button>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>

    <!-- Config dialog -->
    <p-dialog
      [(visible)]="dialogVisible"
      [header]="'Configure ' + dialogCode()"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      styleClass="w-full max-w-md"
    >
      <div class="space-y-4 py-2">
        <div class="space-y-1.5">
          <label class="text-sm font-medium text-slate-700 dark:text-slate-300">API Key / Key ID</label>
          <input pInputText class="w-full" [(ngModel)]="form.keyId" placeholder="rk_live_..." />
        </div>
        <div class="space-y-1.5">
          <label class="text-sm font-medium text-slate-700 dark:text-slate-300">API Secret / Key Secret</label>
          <input pInputText type="password" class="w-full" [(ngModel)]="form.keySecret" placeholder="••••••••" />
        </div>
        <div class="space-y-1.5">
          <label class="text-sm font-medium text-slate-700 dark:text-slate-300">Webhook Secret (optional)</label>
          <input pInputText class="w-full" [(ngModel)]="form.webhookSecret" placeholder="whsec_..." />
        </div>
        <div class="flex items-center gap-2 pt-1">
          <p-checkbox [(ngModel)]="form.isActive" [binary]="true" inputId="active" />
          <label for="active" class="text-sm text-slate-700 dark:text-slate-300">Active</label>
        </div>
      </div>

      <ng-template pTemplate="footer">
        <div class="flex gap-2 justify-end">
          <button pButton label="Test connection" icon="pi pi-wifi"
            severity="secondary" [outlined]="true"
            [loading]="testing()"
            (click)="testConnection()"></button>
          <button pButton label="Save" icon="pi pi-check"
            [loading]="saving()"
            [disabled]="!form.keyId || !form.keySecret"
            (click)="save()"></button>
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class PaymentGatewaysComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(MessageService);

  protected readonly GATEWAYS = GATEWAYS;
  protected readonly loading = signal(true);
  protected readonly saving  = signal(false);
  protected readonly testing = signal(false);
  protected readonly accounts = signal<GatewayAccountDto[]>([]);

  protected dialogVisible = false;
  protected dialogCode = signal('');
  protected form = { keyId: '', keySecret: '', webhookSecret: '', isActive: true };

  ngOnInit(): void { this.load(); }

  private async load(): Promise<void> {
    try {
      const list = await firstValueFrom(this.http.get<GatewayAccountDto[]>(ApiEndpoints.payment.gateways));
      this.accounts.set(list);
    } finally {
      this.loading.set(false);
    }
  }

  protected isConfigured(code: string): boolean {
    return this.accounts().some(a => a.gatewayCode === code && a.isActive);
  }

  protected openDialog(code: string): void {
    this.dialogCode.set(code);
    this.form = { keyId: '', keySecret: '', webhookSecret: '', isActive: true };
    this.dialogVisible = true;
  }

  protected async testConnection(): Promise<void> {
    this.testing.set(true);
    try {
      const creds = JSON.stringify({ KeyId: this.form.keyId, KeySecret: this.form.keySecret });
      await firstValueFrom(this.http.post<{ status: string; message: string }>(
        ApiEndpoints.payment.testGateway,
        { gatewayCode: this.dialogCode(), credentialsJsonEncrypted: creds, isActive: true, isDefault: false }
      ));
      this.toast.add({ severity: 'success', summary: 'Connection OK', life: 3000 });
    } catch {
      this.toast.add({ severity: 'error', summary: 'Connection failed — check your credentials.', life: 5000 });
    } finally {
      this.testing.set(false);
    }
  }

  protected async save(): Promise<void> {
    this.saving.set(true);
    try {
      const creds = JSON.stringify({ KeyId: this.form.keyId, KeySecret: this.form.keySecret });
      await firstValueFrom(this.http.post(ApiEndpoints.payment.gateways, {
        gatewayCode: this.dialogCode(),
        credentialsJsonEncrypted: creds,
        webhookSecretEncrypted: this.form.webhookSecret || null,
        isActive: this.form.isActive,
        isDefault: false,
      }));
      this.toast.add({ severity: 'success', summary: 'Gateway saved', life: 3000 });
      this.dialogVisible = false;
      await this.load();
    } catch {
      this.toast.add({ severity: 'error', summary: 'Save failed', life: 4000 });
    } finally {
      this.saving.set(false);
    }
  }

  protected void(_: unknown): void {}
}
