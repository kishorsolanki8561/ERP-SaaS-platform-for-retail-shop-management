import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AppLabels, AppMessages } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppRoutes } from '../../../shared/messages/app-routes';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, RouterLink, InputTextModule, ButtonModule],
  template: `
    @if (!done()) {
      <div class="mb-8">
        <h2 class="text-2xl font-bold text-slate-900 dark:text-white mb-1">Set new password</h2>
        <p class="text-slate-500 dark:text-slate-400 text-sm">Choose a strong password for your account.</p>
      </div>

      @if (error()) {
        <div class="mb-5 flex items-start gap-3 bg-red-50 border border-red-200 text-red-700 rounded-xl px-4 py-3 text-sm">
          <i class="pi pi-exclamation-circle mt-0.5"></i>
          <span>{{ error() }}</span>
        </div>
      }

      <form (ngSubmit)="submit()" class="space-y-4">
        <div class="space-y-1.5">
          <label class="block text-sm font-medium text-slate-700 dark:text-slate-300">New Password</label>
          <input pInputText type="password" [(ngModel)]="password" name="password"
                 placeholder="At least 6 characters" class="w-full" required />
        </div>
        <div class="space-y-1.5">
          <label class="block text-sm font-medium text-slate-700 dark:text-slate-300">Confirm Password</label>
          <input pInputText type="password" [(ngModel)]="confirm" name="confirm"
                 placeholder="Repeat password" class="w-full" required />
        </div>
        <p-button type="submit" label="Reset Password" [loading]="loading()"
                  styleClass="w-full mt-2" size="large" />
      </form>
    } @else {
      <div class="text-center py-4">
        <div class="w-16 h-16 rounded-2xl bg-green-100 flex items-center justify-center mx-auto mb-5">
          <i class="pi pi-check text-2xl text-green-600"></i>
        </div>
        <h2 class="text-xl font-bold text-slate-900 dark:text-white mb-2">Password updated!</h2>
        <p class="text-slate-500 text-sm mb-6">You can now sign in with your new password.</p>
        <a [routerLink]="'/' + routes.login"
           class="inline-flex items-center gap-1.5 text-sm font-medium text-indigo-600 hover:underline">
          <i class="pi pi-arrow-left text-xs"></i>
          Back to Login
        </a>
      </div>
    }
  `
})
export class ResetPasswordComponent implements OnInit {
  private readonly http    = inject(HttpClient);
  private readonly route   = inject(ActivatedRoute);

  protected readonly routes = AppRoutes;

  protected token    = '';
  protected password = '';
  protected confirm  = '';
  protected loading  = signal(false);
  protected error    = signal<string | null>(null);
  protected done     = signal(false);

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
  }

  protected async submit(): Promise<void> {
    if (this.password !== this.confirm) { this.error.set('Passwords do not match.'); return; }
    if (this.password.length < 6) { this.error.set(AppMessages.validation.passwordTooShort); return; }
    if (!this.token) { this.error.set('Invalid or missing reset token.'); return; }

    this.loading.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.auth.resetPassword, {
        token: this.token,
        newPassword: this.password,
      }));
      this.done.set(true);
    } catch {
      this.error.set('Reset link is invalid or has expired.');
    } finally {
      this.loading.set(false);
    }
  }
}
