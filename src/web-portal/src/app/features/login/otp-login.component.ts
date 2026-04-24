import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { TooltipModule } from 'primeng/tooltip';
import { PortalAuthService } from '../../core/auth/portal-auth.service';
import { ThemeService, Theme } from '../../core/theme/theme.service';

type Step = 'identifier' | 'otp';

@Component({
  selector: 'app-otp-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, InputTextModule, ButtonModule, ToastModule, TooltipModule],
  providers: [MessageService],
  template: `
    <p-toast position="top-right" />

    <div class="min-h-screen flex bg-white dark:bg-slate-950">

      <!-- Left branding (hidden on mobile) -->
      <div class="hidden lg:flex flex-col justify-between w-[420px] shrink-0
                  bg-gradient-to-br from-violet-950 via-purple-900 to-slate-900
                  p-10 relative overflow-hidden">
        <div class="absolute inset-0 pointer-events-none">
          <div class="absolute top-[-100px] left-[-60px] w-[360px] h-[360px] rounded-full bg-violet-500/10 blur-3xl"></div>
          <div class="absolute bottom-[-80px] right-[-60px] w-[280px] h-[280px] rounded-full bg-purple-500/10 blur-3xl"></div>
          <div class="absolute inset-0"
               style="background-image: radial-gradient(circle at 2px 2px, rgba(255,255,255,0.04) 1px, transparent 0); background-size: 40px 40px;"></div>
        </div>

        <div class="relative z-10">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-white/10 backdrop-blur flex items-center justify-center font-bold text-white text-base border border-white/10">
              SE
            </div>
            <span class="text-white font-semibold text-lg">ShopEarth Portal</span>
          </div>
        </div>

        <div class="relative z-10">
          <h1 class="text-3xl font-bold text-white leading-snug mb-3">
            Your shop,<br />at your fingertips.
          </h1>
          <p class="text-violet-300 text-sm leading-relaxed mb-8">
            Track orders, invoices, and your account — all in one place.
          </p>
          <ul class="space-y-3">
            @for (feat of features; track feat) {
              <li class="flex items-center gap-3 text-sm text-violet-200">
                <span class="w-5 h-5 rounded-full bg-violet-500/30 flex items-center justify-center shrink-0">
                  <i class="pi pi-check text-[9px] text-violet-300"></i>
                </span>
                {{ feat }}
              </li>
            }
          </ul>
        </div>

        <div class="relative z-10">
          <p class="text-violet-700 text-xs">© 2025 ShopEarth Technologies</p>
        </div>
      </div>

      <!-- Right form panel -->
      <div class="flex-1 flex flex-col">

        <!-- Theme toggle -->
        <div class="flex justify-end p-4">
          <div class="flex items-center bg-slate-100 dark:bg-slate-800 rounded-xl p-0.5 gap-0.5 border border-slate-200 dark:border-slate-700">
            @for (opt of themeOpts; track opt.value) {
              <button
                class="w-8 h-7 flex items-center justify-center rounded-[10px] text-xs transition-all duration-150"
                [class.bg-white]="themeService.theme() === opt.value && !themeService.isDark()"
                [class.dark:bg-slate-700]="themeService.theme() === opt.value && themeService.isDark()"
                [class.shadow-sm]="themeService.theme() === opt.value"
                [class.text-slate-900]="themeService.theme() === opt.value && !themeService.isDark()"
                [class.dark:text-white]="themeService.theme() === opt.value && themeService.isDark()"
                [class.text-slate-400]="themeService.theme() !== opt.value"
                [pTooltip]="opt.label"
                tooltipPosition="bottom"
                (click)="themeService.set(opt.value)">
                <i [class]="opt.icon"></i>
              </button>
            }
          </div>
        </div>

        <div class="flex-1 flex items-center justify-center p-6">
          <div class="w-full max-w-[380px]">

            <!-- Step indicator -->
            <div class="flex items-center gap-2 mb-8">
              <div class="flex items-center gap-2">
                <div class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-semibold"
                     [class.bg-violet-600]="true"
                     [class.text-white]="true">
                  @if (step() === 'otp') {
                    <i class="pi pi-check text-[10px]"></i>
                  } @else { 1 }
                </div>
                <span class="text-xs font-medium" [class.text-slate-900]="step() === 'identifier'" [class.dark:text-white]="step() === 'identifier'" [class.text-slate-400]="step() !== 'identifier'">Identify</span>
              </div>
              <div class="flex-1 h-px bg-slate-200 dark:bg-slate-700 mx-1"></div>
              <div class="flex items-center gap-2">
                <div class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-semibold"
                     [class.bg-violet-600]="step() === 'otp'"
                     [class.text-white]="step() === 'otp'"
                     [class.bg-slate-100]="step() !== 'otp'"
                     [class.dark:bg-slate-800]="step() !== 'otp'"
                     [class.text-slate-400]="step() !== 'otp'">
                  2
                </div>
                <span class="text-xs font-medium" [class.text-slate-900]="step() === 'otp'" [class.dark:text-white]="step() === 'otp'" [class.text-slate-400]="step() !== 'otp'">Verify OTP</span>
              </div>
            </div>

            @if (step() === 'identifier') {
              <div class="animate-fade-in">
                <div class="mb-7">
                  <div class="lg:hidden flex items-center gap-2 mb-5">
                    <div class="w-8 h-8 rounded-lg bg-violet-600 flex items-center justify-center font-bold text-white text-sm">SE</div>
                    <span class="font-semibold text-slate-900 dark:text-white">ShopEarth Portal</span>
                  </div>
                  <h2 class="text-2xl font-bold text-slate-900 dark:text-white mb-1">Sign in</h2>
                  <p class="text-slate-500 dark:text-slate-400 text-sm">Enter your email or mobile number to receive an OTP.</p>
                </div>

                @if (error()) {
                  <div class="mb-5 flex items-start gap-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800/60 text-red-700 dark:text-red-400 rounded-xl px-4 py-3 text-sm animate-fade-in">
                    <i class="pi pi-exclamation-circle mt-0.5 shrink-0"></i>
                    <span>{{ error() }}</span>
                  </div>
                }

                <form (ngSubmit)="requestOtp()" class="space-y-4">
                  <div class="space-y-1.5">
                    <label class="block text-sm font-medium text-slate-700 dark:text-slate-300">Email / Mobile Number</label>
                    <input pInputText [(ngModel)]="identifier" name="identifier"
                           placeholder="you@example.com or 9876543210"
                           class="w-full" autocomplete="username" required />
                  </div>
                  <p-button type="submit" label="Send OTP"
                            [loading]="loading()" styleClass="w-full" size="large" />
                </form>
              </div>

            } @else {
              <div class="animate-fade-in">
                <div class="mb-7">
                  <button class="inline-flex items-center gap-1.5 text-sm text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition-colors mb-5"
                          type="button" (click)="step.set('identifier')">
                    <i class="pi pi-arrow-left text-xs"></i>
                    Change identifier
                  </button>
                  <h2 class="text-2xl font-bold text-slate-900 dark:text-white mb-1">Enter OTP</h2>
                  <p class="text-slate-500 dark:text-slate-400 text-sm">
                    We sent a 6-digit code to <strong class="text-slate-700 dark:text-slate-300">{{ identifier }}</strong>.
                  </p>
                </div>

                @if (error()) {
                  <div class="mb-5 flex items-start gap-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800/60 text-red-700 dark:text-red-400 rounded-xl px-4 py-3 text-sm animate-fade-in">
                    <i class="pi pi-exclamation-circle mt-0.5 shrink-0"></i>
                    <span>{{ error() }}</span>
                  </div>
                }

                <form (ngSubmit)="verifyOtp()" class="space-y-4">
                  <div class="space-y-1.5">
                    <label class="block text-sm font-medium text-slate-700 dark:text-slate-300">One-Time Password</label>
                    <input pInputText [(ngModel)]="otp" name="otp"
                           placeholder="• • • • • •" maxlength="6"
                           class="w-full text-center tracking-[0.4rem] text-xl font-semibold"
                           autocomplete="one-time-code" required />
                  </div>
                  <p-button type="submit" label="Verify & Sign In"
                            [loading]="loading()" styleClass="w-full" size="large" />
                  <div class="text-center">
                    <button type="button" class="text-xs text-violet-600 dark:text-violet-400 hover:underline"
                            (click)="requestOtp()">
                      Didn't receive the code? Resend
                    </button>
                  </div>
                </form>
              </div>
            }

          </div>
        </div>
      </div>
    </div>
  `
})
export class OtpLoginComponent {
  protected readonly auth = inject(PortalAuthService);
  protected readonly themeService = inject(ThemeService);

  protected identifier = '';
  protected otp        = '';
  protected step       = signal<Step>('identifier');
  protected loading    = signal(false);
  protected error      = signal<string | null>(null);

  protected readonly features = [
    'View your order history & invoices',
    'Track deliveries in real time',
    'Download GST-compliant receipts',
    'Manage loyalty points & wallet',
  ];

  protected readonly themeOpts: { value: Theme; icon: string; label: string }[] = [
    { value: 'light',  icon: 'pi pi-sun',     label: 'Light'  },
    { value: 'dark',   icon: 'pi pi-moon',    label: 'Dark'   },
    { value: 'system', icon: 'pi pi-desktop', label: 'System' },
  ];

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
