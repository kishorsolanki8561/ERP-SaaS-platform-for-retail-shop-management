import { ChangeDetectionStrategy, Component, inject, signal, viewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ThemeService } from '../../../core/theme/theme.service';
import { AppRoutes } from '../../../shared/messages/app-routes';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppMessages } from '../../../shared/messages/app-messages';
import { AppConstants } from '../../../shared/messages/app-constants';
import { TurnstileComponent } from '../../../shared/components/turnstile/turnstile.component';
import { environment } from '../../../../environments/environment';

type Step = 'shop' | 'account' | 'done';

@Component({
  selector: 'app-shop-register',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  imports: [ReactiveFormsModule, RouterLink, TurnstileComponent],
  template: `
    <div class="mb-7">
      <h2 class="text-2xl font-bold tracking-tight mb-1.5 text-slate-900 dark:text-white">
        Register Your Shop
      </h2>
      <p class="text-sm text-slate-500 dark:text-slate-400">
        @if (step() === 'shop') { Step 1 of 2 — Shop details }
        @else if (step() === 'account') { Step 2 of 2 — Admin account }
      </p>
    </div>

    <!-- Progress bar -->
    @if (step() !== 'done') {
      <div class="flex gap-2 mb-7">
        <div class="flex-1 h-1 rounded-full"
             [style]="'background:' + (step() === 'shop' || step() === 'account' ? '#6366f1' : 'rgba(99,102,241,0.2)')"></div>
        <div class="flex-1 h-1 rounded-full"
             [style]="'background:' + (step() === 'account' ? '#6366f1' : 'rgba(99,102,241,0.2)')"></div>
      </div>
    }

    <!-- Error banner -->
    @if (error()) {
      <div class="mb-5 flex items-start gap-3 rounded-xl px-4 py-3 text-sm animate-fade-in"
           style="background:rgba(239,68,68,0.10);border:1px solid rgba(239,68,68,0.28)">
        <i class="pi pi-exclamation-circle text-red-400 mt-0.5 shrink-0"></i>
        <span class="text-red-400">{{ error() }}</span>
      </div>
    }

    <!-- ── Step 1: Shop details ─────────────────────────────────────────── -->
    @if (step() === 'shop') {
      <form [formGroup]="shopForm" (ngSubmit)="nextStep()" class="space-y-5" novalidate>

        <div>
          <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
            Shop Code <span class="text-red-400">*</span>
          </label>
          <input type="text" formControlName="shopCode"
                 placeholder="e.g. NEXUS01"
                 class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                        text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                 [style]="inputStyle('shopCode')" />
          <p class="text-xs text-slate-400 mt-1">Unique identifier for your shop (max 20 chars, no spaces).</p>
          @if (shopInvalid('shopCode')) {
            <p class="text-xs text-red-400 mt-1"><i class="pi pi-exclamation-circle text-[10px]"></i> Required.</p>
          }
        </div>

        <div>
          <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
            Legal Business Name <span class="text-red-400">*</span>
          </label>
          <input type="text" formControlName="legalName"
                 placeholder="e.g. Nexus Electronics Pvt Ltd"
                 class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                        text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                 [style]="inputStyle('legalName')" />
          @if (shopInvalid('legalName')) {
            <p class="text-xs text-red-400 mt-1"><i class="pi pi-exclamation-circle text-[10px]"></i> Required.</p>
          }
        </div>

        <div>
          <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
            Trade Name <span class="text-xs font-normal text-slate-400">(optional)</span>
          </label>
          <input type="text" formControlName="tradeName"
                 placeholder="e.g. Nexus Electronics"
                 class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                        text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                 [style]="inputStyle('tradeName')" />
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
              GST Number <span class="text-xs font-normal text-slate-400">(optional)</span>
            </label>
            <input type="text" formControlName="gstNumber"
                   placeholder="22AAAAA0000A1Z5"
                   class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                          text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                   [style]="inputStyle('gstNumber')" />
          </div>
          <div>
            <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
              Contact Phone <span class="text-xs font-normal text-slate-400">(optional)</span>
            </label>
            <input type="tel" formControlName="contactPhone"
                   placeholder="+91 98765 43210"
                   class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                          text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                   [style]="inputStyle('contactPhone')" />
          </div>
        </div>

        <div>
          <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
            Message to admin <span class="text-xs font-normal text-slate-400">(optional)</span>
          </label>
          <textarea formControlName="notes" rows="2"
                    placeholder="Anything you'd like the platform admin to know…"
                    class="w-full px-4 py-3 rounded-xl text-sm outline-none transition-all duration-200 resize-none
                           text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                    [style]="inputStyle('notes')"></textarea>
        </div>

        <button type="submit"
                class="w-full h-12 rounded-xl text-white font-semibold text-sm mt-1
                       flex items-center justify-center gap-2 transition-all duration-200
                       hover:brightness-110 active:scale-[0.99]"
                style="background:linear-gradient(135deg,#6366f1 0%,#4338ca 100%);box-shadow:0 8px 24px rgba(99,102,241,0.35)">
          Continue <i class="pi pi-arrow-right text-sm"></i>
        </button>
      </form>
    }

    <!-- ── Step 2: Admin account ───────────────────────────────────────── -->
    @if (step() === 'account') {
      <form [formGroup]="accountForm" (ngSubmit)="submit()" class="space-y-5" novalidate>

        <div>
          <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
            Your Name <span class="text-red-400">*</span>
          </label>
          <input type="text" formControlName="displayName"
                 placeholder="e.g. Rahul Sharma"
                 class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                        text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                 [style]="accountInputStyle('displayName')" />
          @if (accountInvalid('displayName')) {
            <p class="text-xs text-red-400 mt-1"><i class="pi pi-exclamation-circle text-[10px]"></i> Required.</p>
          }
        </div>

        <div>
          <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
            Email Address <span class="text-red-400">*</span>
          </label>
          <input type="email" formControlName="adminEmail"
                 placeholder="you@example.com"
                 autocomplete="username"
                 class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                        text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                 [style]="accountInputStyle('adminEmail')" />
          @if (accountInvalid('adminEmail')) {
            <p class="text-xs text-red-400 mt-1"><i class="pi pi-exclamation-circle text-[10px]"></i> Valid email required.</p>
          }
        </div>

        <div>
          <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
            Password <span class="text-red-400">*</span>
          </label>
          <div class="relative">
            <input [type]="showPwd() ? 'text' : 'password'"
                   formControlName="password"
                   placeholder="••••••••"
                   autocomplete="new-password"
                   class="w-full h-11 px-4 pr-11 rounded-xl text-sm outline-none transition-all duration-200
                          text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                   [style]="accountInputStyle('password')" />
            <button type="button"
                    class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400 dark:text-white/35"
                    (click)="showPwd.set(!showPwd())">
              <i [class]="showPwd() ? 'pi pi-eye-slash text-[14px]' : 'pi pi-eye text-[14px]'"></i>
            </button>
          </div>
          @if (accountInvalid('password')) {
            <p class="text-xs text-red-400 mt-1"><i class="pi pi-exclamation-circle text-[10px]"></i> {{ messages.validation.passwordTooShort }}</p>
          }
        </div>

        <div>
          <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">
            Confirm Password <span class="text-red-400">*</span>
          </label>
          <input [type]="showPwd() ? 'text' : 'password'"
                 formControlName="confirmPassword"
                 placeholder="••••••••"
                 autocomplete="new-password"
                 class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                        text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                 [style]="accountInputStyle('confirmPassword')" />
          @if (accountForm.errors?.['mismatch'] && accountForm.get('confirmPassword')?.touched) {
            <p class="text-xs text-red-400 mt-1"><i class="pi pi-exclamation-circle text-[10px]"></i> Passwords do not match.</p>
          }
        </div>

        <div class="flex justify-center">
          <app-turnstile
            [siteKey]="turnstileSiteKey"
            [theme]="themeService.isDark() ? 'dark' : 'light'"
            (resolved)="captchaToken.set($event)" />
        </div>

        <div class="flex gap-3">
          <button type="button"
                  (click)="step.set('shop')"
                  class="flex-1 h-12 rounded-xl font-semibold text-sm border transition-all duration-200
                         text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-white/5"
                  style="border:1px solid rgba(99,102,241,0.25)">
            <i class="pi pi-arrow-left text-sm mr-1"></i> Back
          </button>
          <button type="submit"
                  [disabled]="loading() || !captchaToken()"
                  class="flex-1 h-12 rounded-xl text-white font-semibold text-sm
                         flex items-center justify-center gap-2 transition-all duration-200
                         hover:brightness-110 active:scale-[0.99] disabled:opacity-55 disabled:cursor-not-allowed"
                  style="background:linear-gradient(135deg,#6366f1 0%,#4338ca 100%);box-shadow:0 8px 24px rgba(99,102,241,0.35)">
            @if (loading()) { <i class="pi pi-spin pi-spinner text-sm"></i> }
            Submit Application
          </button>
        </div>
      </form>
    }

    <!-- ── Step 3: Confirmation ───────────────────────────────────────── -->
    @if (step() === 'done') {
      <div class="text-center py-4">
        <div class="w-16 h-16 rounded-full flex items-center justify-center mx-auto mb-6"
             style="background:rgba(99,102,241,0.12)">
          <i class="pi pi-check-circle text-3xl text-indigo-500"></i>
        </div>
        <h3 class="text-xl font-bold text-slate-900 dark:text-white mb-3">Application Submitted!</h3>
        <p class="text-sm text-slate-500 dark:text-slate-400 mb-2">
          Your registration request for <strong>{{ shopForm.value.legalName }}</strong> has been submitted.
        </p>
        <p class="text-sm text-slate-500 dark:text-slate-400 mb-8">
          The platform admin will review your application and you will be notified at
          <strong>{{ accountForm.value.adminEmail }}</strong> once a decision is made.
        </p>
        <a [routerLink]="'/' + routes.login"
           class="inline-flex items-center gap-2 text-sm font-medium text-indigo-600 dark:text-indigo-400
                  hover:text-indigo-500 dark:hover:text-indigo-300 transition-colors">
          <i class="pi pi-arrow-left text-xs"></i> Back to Sign In
        </a>
      </div>
    }

    @if (step() !== 'done') {
      <p class="text-center text-xs mt-7 text-slate-400 dark:text-slate-600">
        Already have an account?
        <a [routerLink]="'/' + routes.login"
           class="text-indigo-600 dark:text-indigo-400 font-medium hover:underline">Sign in</a>
      </p>
    }
  `,
})
export class ShopRegisterComponent {
  protected readonly themeService    = inject(ThemeService);
  protected readonly routes          = AppRoutes;
  protected readonly messages        = AppMessages;
  protected readonly turnstileSiteKey = environment.turnstileSiteKey;

  private readonly http = inject(HttpClient);

  protected readonly step         = signal<Step>('shop');
  protected readonly loading      = signal(false);
  protected readonly error        = signal<string | null>(null);
  protected readonly showPwd      = signal(false);
  protected readonly captchaToken = signal('');

  private readonly turnstile = viewChild(TurnstileComponent);

  protected readonly shopForm = new FormGroup({
    shopCode:     new FormControl('', [Validators.required, Validators.maxLength(20), Validators.pattern(/^\S+$/)]),
    legalName:    new FormControl('', [Validators.required, Validators.maxLength(200)]),
    tradeName:    new FormControl(''),
    gstNumber:    new FormControl(''),
    contactPhone: new FormControl(''),
    notes:        new FormControl(''),
  });

  protected readonly accountForm = new FormGroup(
    {
      displayName:     new FormControl('', [Validators.required]),
      adminEmail:      new FormControl('', [Validators.required, Validators.email]),
      password:        new FormControl('', [Validators.required, Validators.minLength(AppConstants.password.minLength)]),
      confirmPassword: new FormControl('', [Validators.required]),
    },
    { validators: (g) => g.get('password')?.value === g.get('confirmPassword')?.value ? null : { mismatch: true } },
  );

  protected shopInvalid(field: string): boolean {
    const ctrl = this.shopForm.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  protected accountInvalid(field: string): boolean {
    const ctrl = this.accountForm.get(field);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  protected nextStep(): void {
    this.shopForm.markAllAsTouched();
    if (this.shopForm.invalid) return;
    this.error.set(null);
    this.step.set('account');
  }

  protected async submit(): Promise<void> {
    this.accountForm.markAllAsTouched();
    if (this.accountForm.invalid || !this.captchaToken()) return;

    this.loading.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(
        this.http.post(ApiEndpoints.shopRegistration.submit, {
          shopCode:          this.shopForm.value.shopCode,
          legalName:         this.shopForm.value.legalName,
          tradeName:         this.shopForm.value.tradeName || null,
          gstNumber:         this.shopForm.value.gstNumber || null,
          contactPhone:      this.shopForm.value.contactPhone || null,
          notes:             this.shopForm.value.notes || null,
          adminDisplayName:  this.accountForm.value.displayName,
          adminEmail:        this.accountForm.value.adminEmail,
          password:          this.accountForm.value.password,
        }, {
          headers: { 'cf-turnstile-response': this.captchaToken() },
        }),
      );
      this.step.set('done');
    } catch (err: unknown) {
      const msg = (err as { error?: { errors?: string[] } })?.error?.errors?.[0]
        ?? AppMessages.common.error;
      this.error.set(msg);
      this.captchaToken.set('');
      this.turnstile()?.reset();
    } finally {
      this.loading.set(false);
    }
  }

  protected inputStyle(field: string): string {
    const invalid = this.shopInvalid(field);
    return this.themeService.isDark()
      ? 'background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.13)'
      : `background:#fff;border:1px solid ${invalid ? 'rgba(239,68,68,0.5)' : 'rgba(99,102,241,0.25)'};box-shadow:0 1px 3px rgba(0,0,0,0.04)`;
  }

  protected accountInputStyle(field: string): string {
    const invalid = this.accountInvalid(field);
    return this.themeService.isDark()
      ? 'background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.13)'
      : `background:#fff;border:1px solid ${invalid ? 'rgba(239,68,68,0.5)' : 'rgba(99,102,241,0.25)'};box-shadow:0 1px 3px rgba(0,0,0,0.04)`;
  }
}
