import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AppLabels, AppMessages } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppRoutes } from '../../../shared/messages/app-routes';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  imports: [CommonModule, FormsModule, RouterLink, InputTextModule, ButtonModule],
  template: `
    @if (!sent()) {
      <!-- Header -->
      <div class="mb-8">
        <a [routerLink]="'/' + routes.login"
           class="inline-flex items-center gap-1.5 text-sm text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition-colors mb-6">
          <i class="pi pi-arrow-left text-xs"></i>
          {{ labels.auth.backToLogin }}
        </a>
        <h2 class="text-2xl font-bold text-slate-900 dark:text-white mb-1">
          {{ labels.auth.forgotPasswordTitle }}
        </h2>
        <p class="text-slate-500 dark:text-slate-400 text-sm">{{ labels.auth.forgotPasswordSubtext }}</p>
      </div>

      @if (error()) {
        <div class="mb-5 flex items-start gap-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800/60 text-red-700 dark:text-red-400 rounded-xl px-4 py-3 text-sm animate-fade-in">
          <i class="pi pi-exclamation-circle mt-0.5 shrink-0"></i>
          <span>{{ error() }}</span>
        </div>
      }

      <form (ngSubmit)="submit()" class="space-y-4">
        <div class="space-y-1.5">
          <label class="block text-sm font-medium text-slate-700 dark:text-slate-300">
            {{ labels.auth.emailLabel }}
          </label>
          <input
            pInputText
            type="email"
            [(ngModel)]="email"
            name="email"
            [placeholder]="labels.auth.emailPlaceholder"
            class="w-full"
            required />
        </div>

        <p-button
          type="submit"
          [label]="labels.auth.sendResetButton"
          [loading]="loading()"
          styleClass="w-full mt-2"
          size="large" />
      </form>

    } @else {
      <!-- Success state -->
      <div class="text-center py-4 animate-fade-in">
        <div class="w-16 h-16 rounded-2xl bg-green-100 dark:bg-green-950/40 flex items-center justify-center mx-auto mb-5">
          <i class="pi pi-check text-2xl text-green-600 dark:text-green-400"></i>
        </div>
        <h2 class="text-xl font-bold text-slate-900 dark:text-white mb-2">Check your email</h2>
        <p class="text-slate-500 dark:text-slate-400 text-sm mb-6">
          We've sent a password reset link to <strong class="text-slate-700 dark:text-slate-300">{{ email }}</strong>.
        </p>
        <a [routerLink]="'/' + routes.login"
           class="inline-flex items-center gap-1.5 text-sm font-medium text-indigo-600 dark:text-indigo-400 hover:underline">
          <i class="pi pi-arrow-left text-xs"></i>
          {{ labels.auth.backToLogin }}
        </a>
      </div>
    }
  `
})
export class ForgotPasswordComponent {
  private readonly http = inject(HttpClient);

  protected readonly labels   = AppLabels;
  protected readonly messages = AppMessages;
  protected readonly routes   = AppRoutes;

  protected email   = '';
  protected loading = signal(false);
  protected error   = signal<string | null>(null);
  protected sent    = signal(false);

  protected async submit(): Promise<void> {
    if (!this.email) return;
    this.loading.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.auth.forgotPassword, { email: this.email }));
      this.sent.set(true);
    } catch {
      this.error.set(AppMessages.common.error);
    } finally {
      this.loading.set(false);
    }
  }
}
