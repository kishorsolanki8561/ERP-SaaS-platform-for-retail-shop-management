import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { PortalAuthService } from '../../core/auth/portal-auth.service';

type Step = 'identifier' | 'otp';

@Component({
  selector: 'app-otp-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, CardModule, InputTextModule, ButtonModule, MessageModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-surface-50 p-4">
      <p-card styleClass="w-full max-w-md shadow-lg">
        <ng-template pTemplate="header">
          <div class="text-center pt-6 px-6">
            <h1 class="text-2xl font-bold text-surface-800">Customer Portal</h1>
            <p class="text-surface-500 mt-1 text-sm">
              {{ step() === 'identifier' ? 'Enter your email or mobile number.' : 'Enter the OTP sent to you.' }}
            </p>
          </div>
        </ng-template>

        @if (step() === 'identifier') {
          <form (ngSubmit)="requestOtp()" class="flex flex-col gap-4">
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">Email / Mobile</label>
              <input pInputText [(ngModel)]="identifier" name="identifier"
                     placeholder="you@example.com or 9876543210"
                     class="w-full" required />
            </div>

            @if (error()) {
              <p-message severity="error" [text]="error()!" />
            }

            <p-button type="submit" label="Send OTP" styleClass="w-full" [loading]="loading()" />
          </form>
        } @else {
          <form (ngSubmit)="verifyOtp()" class="flex flex-col gap-4">
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">One-Time Password</label>
              <input pInputText [(ngModel)]="otp" name="otp"
                     placeholder="6-digit code" maxlength="6"
                     class="w-full" required />
            </div>

            @if (error()) {
              <p-message severity="error" [text]="error()!" />
            }

            <p-button type="submit" label="Verify OTP" styleClass="w-full" [loading]="loading()" />

            <div class="text-center text-sm">
              <button type="button" class="text-primary-600 hover:underline"
                      (click)="step.set('identifier')">
                Change identifier
              </button>
            </div>
          </form>
        }
      </p-card>
    </div>
  `
})
export class OtpLoginComponent {
  private readonly auth = inject(PortalAuthService);

  protected identifier = '';
  protected otp = '';
  protected step = signal<Step>('identifier');
  protected loading = signal(false);
  protected error = signal<string | null>(null);

  protected async requestOtp(): Promise<void> {
    if (!this.identifier) return;
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.auth.requestOtp(this.identifier);
      this.step.set('otp');
    } catch {
      this.error.set('Could not send OTP. Please check the identifier and try again.');
    } finally {
      this.loading.set(false);
    }
  }

  protected async verifyOtp(): Promise<void> {
    if (!this.otp) return;
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.auth.verifyOtp(this.identifier, this.otp);
    } catch {
      this.error.set('Invalid or expired OTP. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }
}
