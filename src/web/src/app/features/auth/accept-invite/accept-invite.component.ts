import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AppMessages } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppRoutePaths } from '../../../shared/messages/app-routes';

interface AcceptResponse { accessToken: string; refreshToken: string; expiresAtUtc: string; }

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  imports: [CommonModule, FormsModule, RouterLink, InputTextModule, ButtonModule],
  template: `
    <div class="mb-8">
      <h2 class="text-2xl font-bold text-slate-900 dark:text-white mb-1">Welcome! Set your password</h2>
      <p class="text-slate-500 dark:text-slate-400 text-sm">
        You've been invited. Create a password to activate your account.
      </p>
    </div>

    @if (error()) {
      <div class="mb-5 flex items-start gap-3 bg-red-50 border border-red-200 text-red-700 rounded-xl px-4 py-3 text-sm">
        <i class="pi pi-exclamation-circle mt-0.5"></i>
        <span>{{ error() }}</span>
      </div>
    }

    @if (!invalidToken()) {
      <form (ngSubmit)="submit()" class="space-y-4">
        <div class="space-y-1.5">
          <label class="block text-sm font-medium text-slate-700 dark:text-slate-300">Password</label>
          <input pInputText type="password" [(ngModel)]="password" name="password"
                 placeholder="At least 6 characters" class="w-full" required />
        </div>
        <div class="space-y-1.5">
          <label class="block text-sm font-medium text-slate-700 dark:text-slate-300">Confirm Password</label>
          <input pInputText type="password" [(ngModel)]="confirm" name="confirm"
                 placeholder="Repeat password" class="w-full" required />
        </div>
        <p-button type="submit" label="Activate Account" [loading]="loading()"
                  styleClass="w-full mt-2" size="large" severity="success" />
      </form>
    } @else {
      <div class="text-center py-4">
        <div class="w-16 h-16 rounded-2xl bg-amber-100 flex items-center justify-center mx-auto mb-5">
          <i class="pi pi-exclamation-triangle text-2xl text-amber-600"></i>
        </div>
        <h2 class="text-xl font-bold text-slate-900 mb-2">Invite link expired</h2>
        <p class="text-slate-500 text-sm mb-6">Ask your administrator to resend the invitation.</p>
        <a [routerLink]="paths.login"
           class="inline-flex items-center gap-1.5 text-sm font-medium text-indigo-600 hover:underline">
          <i class="pi pi-arrow-left text-xs"></i> Back to Login
        </a>
      </div>
    }
  `
})
export class AcceptInviteComponent implements OnInit {
  private readonly http   = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly route  = inject(ActivatedRoute);

  protected readonly paths = AppRoutePaths;

  protected token       = '';
  protected password    = '';
  protected confirm     = '';
  protected loading     = signal(false);
  protected error       = signal<string | null>(null);
  protected invalidToken = signal(false);

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.token) this.invalidToken.set(true);
  }

  protected async submit(): Promise<void> {
    if (this.password !== this.confirm) { this.error.set('Passwords do not match.'); return; }
    if (this.password.length < 6) { this.error.set(AppMessages.validation.passwordTooShort); return; }

    this.loading.set(true);
    this.error.set(null);
    try {
      const resp = await firstValueFrom(
        this.http.post<{ value: AcceptResponse }>(ApiEndpoints.auth.acceptInvite, {
          token: this.token,
          newPassword: this.password,
        })
      );
      // Store tokens and navigate to dashboard
      localStorage.setItem('access_token', resp.value.accessToken);
      localStorage.setItem('refresh_token', resp.value.refreshToken);
      this.router.navigate([AppRoutePaths.dashboard]);
    } catch {
      this.invalidToken.set(true);
    } finally {
      this.loading.set(false);
    }
  }
}
