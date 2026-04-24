import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { AuthService } from '../../../core/auth/auth.service';
import { AppLabels, AppMessages } from '../../../shared/messages/app-messages';
import { AppConstants } from '../../../shared/messages/app-constants';
import { AppRoutePaths, AppRoutes } from '../../../shared/messages/app-routes';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, InputTextModule, PasswordModule, ButtonModule, CheckboxModule],
  template: `
    <!-- Header -->
    <div class="mb-8">
      <div class="lg:hidden flex items-center gap-2 mb-6">
        <div class="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center font-bold text-white text-sm">SE</div>
        <span class="font-semibold text-slate-900 dark:text-white">ShopEarth ERP</span>
      </div>
      <h2 class="text-2xl font-bold text-slate-900 dark:text-white mb-1">Welcome back</h2>
      <p class="text-slate-500 dark:text-slate-400 text-sm">{{ labels.auth.loginTitle }}</p>
    </div>

    <!-- Error banner -->
    @if (error()) {
      <div class="mb-5 flex items-start gap-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800/60 text-red-700 dark:text-red-400 rounded-xl px-4 py-3 text-sm animate-fade-in">
        <i class="pi pi-exclamation-circle mt-0.5 shrink-0"></i>
        <span>{{ error() }}</span>
      </div>
    }

    <!-- Form -->
    <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">

      <div class="space-y-1.5">
        <label class="block text-sm font-medium text-slate-700 dark:text-slate-300">
          {{ labels.auth.identifierLabel }}
        </label>
        <input
          pInputText
          formControlName="identifier"
          [placeholder]="labels.auth.identifierPlaceholder"
          class="w-full"
          [class.ng-invalid]="isInvalid('identifier')"
          autocomplete="username" />
        @if (isInvalid('identifier')) {
          <p class="text-xs text-red-500">{{ messages.validation.required }}</p>
        }
      </div>

      <div class="space-y-1.5">
        <div class="flex items-center justify-between">
          <label class="text-sm font-medium text-slate-700 dark:text-slate-300">
            {{ labels.auth.passwordLabel }}
          </label>
          <a [routerLink]="'/' + routes.forgotPassword"
             class="text-xs text-indigo-600 dark:text-indigo-400 hover:underline font-medium">
            Forgot password?
          </a>
        </div>
        <p-password
          formControlName="password"
          [feedback]="false"
          [toggleMask]="true"
          [class.ng-invalid]="isInvalid('password')"
          autocomplete="current-password" />
        @if (isInvalid('password')) {
          <p class="text-xs text-red-500">{{ messages.validation.required }}</p>
        }
      </div>

      <p-button
        type="submit"
        [label]="labels.auth.signInButton"
        [loading]="loading()"
        [disabled]="form.invalid"
        styleClass="w-full mt-2"
        size="large" />
    </form>

    <p class="text-center text-xs text-slate-400 dark:text-slate-600 mt-8">
      Need access? Contact your shop administrator.
    </p>
  `
})
export class LoginComponent {
  protected readonly labels = AppLabels;
  protected readonly messages = AppMessages;
  protected readonly routes = AppRoutes;

  protected readonly form = new FormGroup({
    identifier: new FormControl('', [Validators.required]),
    password:   new FormControl('', [Validators.required, Validators.minLength(AppConstants.password.minLength)]),
  });

  protected readonly loading = signal(false);
  protected readonly error   = signal<string | null>(null);

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
