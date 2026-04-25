import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ThemeService } from '../../../core/theme/theme.service';
import { AuthService } from '../../../core/auth/auth.service';
import { AppLabels, AppMessages } from '../../../shared/messages/app-messages';
import { AppConstants } from '../../../shared/messages/app-constants';
import { AppRoutePaths, AppRoutes } from '../../../shared/messages/app-routes';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="mb-7">
      <h2 class="text-2xl font-bold tracking-tight mb-1.5 text-slate-900 dark:text-white">
        Welcome back
      </h2>
      <p class="text-sm text-slate-500 dark:text-slate-400">Sign in to your account to continue.</p>
    </div>

    @if (error()) {
      <div class="mb-5 flex items-start gap-3 rounded-xl px-4 py-3 text-sm animate-fade-in"
           style="background:rgba(239,68,68,0.10);border:1px solid rgba(239,68,68,0.28)">
        <i class="pi pi-exclamation-circle text-red-400 mt-0.5 shrink-0"></i>
        <span class="text-red-400">{{ error() }}</span>
      </div>
    }

    <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-5" novalidate>

      <div>
        <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
          {{ labels.auth.identifierLabel }}
        </label>
        <input
          type="text"
          formControlName="identifier"
          [placeholder]="labels.auth.identifierPlaceholder"
          autocomplete="username"
          class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                 text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
          [style]="themeService.isDark()
            ? 'background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.13)'
            : 'background:#fff;border:1px solid ' + (isInvalid('identifier') ? 'rgba(239,68,68,0.5)' : 'rgba(99,102,241,0.25)') + ';box-shadow:0 1px 3px rgba(0,0,0,0.04)'"
        />
        @if (isInvalid('identifier')) {
          <p class="text-xs text-red-400 mt-1 flex items-center gap-1">
            <i class="pi pi-exclamation-circle text-[10px]"></i>
            {{ messages.validation.required }}
          </p>
        }
      </div>

      <div>
        <div class="flex items-center justify-between mb-1.5">
          <label class="text-[13px] font-medium text-slate-700 dark:text-slate-300">
            {{ labels.auth.passwordLabel }}
          </label>
          <a [routerLink]="'/' + routes.forgotPassword"
             class="text-xs font-medium text-indigo-600 dark:text-indigo-400 hover:text-indigo-500 dark:hover:text-indigo-300 transition-colors">
            Forgot password?
          </a>
        </div>
        <div class="relative">
          <input
            [type]="showPwd() ? 'text' : 'password'"
            formControlName="password"
            placeholder="••••••••"
            autocomplete="current-password"
            class="w-full h-11 px-4 pr-11 rounded-xl text-sm outline-none transition-all duration-200
                   text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
            [style]="themeService.isDark()
              ? 'background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.13)'
              : 'background:#fff;border:1px solid ' + (isInvalid('password') ? 'rgba(239,68,68,0.5)' : 'rgba(99,102,241,0.25)') + ';box-shadow:0 1px 3px rgba(0,0,0,0.04)'"
          />
          <button type="button"
                  class="absolute right-3.5 top-1/2 -translate-y-1/2 transition-colors
                         text-slate-400 dark:text-white/35 hover:text-slate-600 dark:hover:text-white/70"
                  (click)="showPwd.set(!showPwd())">
            <i [class]="showPwd() ? 'pi pi-eye-slash text-[14px]' : 'pi pi-eye text-[14px]'"></i>
          </button>
        </div>
        @if (isInvalid('password')) {
          <p class="text-xs text-red-400 mt-1 flex items-center gap-1">
            <i class="pi pi-exclamation-circle text-[10px]"></i>
            {{ messages.validation.required }}
          </p>
        }
      </div>

      <button
        type="submit"
        [disabled]="loading()"
        class="w-full h-12 rounded-xl text-white font-semibold text-sm mt-1
               flex items-center justify-center gap-2 transition-all duration-200
               hover:brightness-110 active:scale-[0.99] disabled:opacity-55 disabled:cursor-not-allowed"
        style="background:linear-gradient(135deg,#6366f1 0%,#4338ca 100%);box-shadow:0 8px 24px rgba(99,102,241,0.35)">
        @if (loading()) { <i class="pi pi-spin pi-spinner text-sm"></i> }
        {{ labels.auth.signInButton }}
      </button>

    </form>

    <p class="text-center text-xs mt-7 text-slate-400 dark:text-slate-600">
      Need access? Contact your shop administrator.
    </p>
  `
})
export class LoginComponent {
  protected readonly themeService = inject(ThemeService);
  protected readonly labels       = AppLabels;
  protected readonly messages     = AppMessages;
  protected readonly routes       = AppRoutes;

  protected readonly form = new FormGroup({
    identifier: new FormControl('', [Validators.required]),
    password:   new FormControl('', [Validators.required, Validators.minLength(AppConstants.password.minLength)]),
  });

  protected readonly loading  = signal(false);
  protected readonly error    = signal<string | null>(null);
  protected readonly showPwd  = signal(false);

  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);

  protected isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  protected async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.auth.login({
        identifier: this.form.value.identifier!,
        password:   this.form.value.password!,
      });
      await this.router.navigate([AppRoutePaths.dashboard]);
    } catch (err: unknown) {
      const msg = (err as { error?: { errors?: string[] } })?.error?.errors?.[0]
        ?? AppMessages.auth.loginFailed;
      this.error.set(msg);
    } finally {
      this.loading.set(false);
    }
  }
}
