import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { AuthService } from '../../../core/auth/auth.service';
import { AppLabels, AppMessages } from '../../../shared/messages/app-messages';
import { AppConstants } from '../../../shared/messages/app-constants';
import { AppRoutePaths } from '../../../shared/messages/app-routes';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    MessageModule
  ],
  template: `
    <p-card class="w-96">
      <ng-template pTemplate="header">
        <div class="text-center px-6 pt-6">
          <h1 class="text-2xl font-bold text-surface-900">{{ labels.appName }}</h1>
          <p class="text-surface-500 mt-1">{{ labels.auth.loginTitle }}</p>
        </div>
      </ng-template>

      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
        @if (error()) {
          <p-message severity="error" [text]="error()!" />
        }

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ labels.auth.identifierLabel }}</label>
          <input pInputText formControlName="identifier" [placeholder]="labels.auth.identifierPlaceholder"
                 [class.ng-invalid]="form.get('identifier')?.invalid && form.get('identifier')?.touched" />
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ labels.auth.passwordLabel }}</label>
          <p-password formControlName="password" [feedback]="false" [toggleMask]="true"
                      styleClass="w-full" inputStyleClass="w-full" />
        </div>

        <p-button type="submit" [label]="labels.auth.signInButton" [loading]="loading()"
                  [disabled]="form.invalid" styleClass="w-full" />
      </form>
    </p-card>
  `
})
export class LoginComponent {
  protected readonly labels = AppLabels;

  protected readonly form = new FormGroup({
    identifier: new FormControl('', [Validators.required]),
    password: new FormControl('', [Validators.required, Validators.minLength(AppConstants.password.minLength)])
  });

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected async submit(): Promise<void> {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set(null);

    try {
      await this.auth.login({
        identifier: this.form.value.identifier!,
        password: this.form.value.password!
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
