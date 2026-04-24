import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, CardModule, InputTextModule, ButtonModule, MessageModule],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-surface-50 p-4">
      <p-card styleClass="w-full max-w-md shadow-lg">
        <ng-template pTemplate="header">
          <div class="text-center pt-6 px-6">
            <h1 class="text-2xl font-bold text-surface-800">Reset Password</h1>
            <p class="text-surface-500 mt-1 text-sm">Enter your email to receive a reset link.</p>
          </div>
        </ng-template>

        @if (!sent()) {
          <form (ngSubmit)="submit()" class="flex flex-col gap-4">
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">Email</label>
              <input pInputText type="email" [(ngModel)]="email" name="email"
                     placeholder="you@example.com" class="w-full" required />
            </div>

            @if (error()) {
              <p-message severity="error" [text]="error()!" />
            }

            <p-button type="submit" label="Send Reset Link" styleClass="w-full"
                      [loading]="loading()" />

            <div class="text-center text-sm">
              <a routerLink="/login" class="text-primary-600 hover:underline">Back to Login</a>
            </div>
          </form>
        } @else {
          <div class="text-center py-4">
            <i class="pi pi-check-circle text-4xl text-green-500 mb-3 block"></i>
            <p class="text-surface-700">Check your inbox for the reset link.</p>
            <a routerLink="/login" class="text-primary-600 hover:underline text-sm mt-3 block">
              Back to Login
            </a>
          </div>
        }
      </p-card>
    </div>
  `
})
export class ForgotPasswordComponent {
  private readonly http = inject(HttpClient);

  protected email = '';
  protected loading = signal(false);
  protected error = signal<string | null>(null);
  protected sent = signal(false);

  protected async submit(): Promise<void> {
    if (!this.email) return;
    this.loading.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.http.post('/api/auth/forgot-password', { email: this.email })
      );
      this.sent.set(true);
    } catch {
      this.error.set('Unable to send reset link. Please try again.');
    } finally {
      this.loading.set(false);
    }
  }
}
