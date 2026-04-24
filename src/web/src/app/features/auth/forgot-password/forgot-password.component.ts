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
import { AppLabels, AppMessages } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppRoutes } from '../../../shared/messages/app-routes';

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
            <h1 class="text-2xl font-bold text-surface-800">{{ labels.auth.forgotPasswordTitle }}</h1>
            <p class="text-surface-500 mt-1 text-sm">{{ labels.auth.forgotPasswordSubtext }}</p>
          </div>
        </ng-template>

        @if (!sent()) {
          <form (ngSubmit)="submit()" class="flex flex-col gap-4">
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">{{ labels.auth.emailLabel }}</label>
              <input pInputText type="email" [(ngModel)]="email" name="email"
                     [placeholder]="labels.auth.emailPlaceholder" class="w-full" required />
            </div>

            @if (error()) {
              <p-message severity="error" [text]="error()!" />
            }

            <p-button type="submit" [label]="labels.auth.sendResetButton" styleClass="w-full"
                      [loading]="loading()" />

            <div class="text-center text-sm">
              <a [routerLink]="'/' + routes.login" class="text-primary-600 hover:underline">{{ labels.auth.backToLogin }}</a>
            </div>
          </form>
        } @else {
          <div class="text-center py-4">
            <i class="pi pi-check-circle text-4xl text-green-500 mb-3 block"></i>
            <p class="text-surface-700">{{ messages.common.saveSuccess }}</p>
            <a [routerLink]="'/' + routes.login" class="text-primary-600 hover:underline text-sm mt-3 block">
              {{ labels.auth.backToLogin }}
            </a>
          </div>
        }
      </p-card>
    </div>
  `
})
export class ForgotPasswordComponent {
  private readonly http = inject(HttpClient);

  protected readonly labels = AppLabels;
  protected readonly messages = AppMessages;
  protected readonly routes = AppRoutes;

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
        this.http.post(ApiEndpoints.auth.forgotPassword, { email: this.email })
      );
      this.sent.set(true);
    } catch {
      this.error.set(AppMessages.common.error);
    } finally {
      this.loading.set(false);
    }
  }
}
