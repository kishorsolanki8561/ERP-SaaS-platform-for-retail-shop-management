import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import { PortalAuthService } from '../../core/auth/portal-auth.service';
import { ThemeService, Theme } from '../../core/theme/theme.service';

type Step = 'identifier' | 'otp';

@Component({
  selector: 'app-otp-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, TooltipModule],
  template: `
    <div [class.app-dark]="themeService.isDark()">
      <div class="min-h-screen flex relative overflow-hidden transition-colors duration-300"
           [style]="themeService.isDark()
             ? 'background:linear-gradient(135deg,#05040f 0%,#0e0820 55%,#060410 100%)'
             : 'background:linear-gradient(135deg,#faf8ff 0%,#f3eeff 60%,#faf5ff 100%)'">

        <!-- Ambient glows -->
        <div class="pointer-events-none fixed inset-0 overflow-hidden">
          <div class="absolute -top-40 -right-20 w-[800px] h-[800px] rounded-full"
               style="background:radial-gradient(circle at center,rgba(139,92,246,0.14) 0%,transparent 65%)"></div>
          <div class="absolute -bottom-40 -left-20 w-[700px] h-[700px] rounded-full"
               style="background:radial-gradient(circle at center,rgba(99,102,241,0.09) 0%,transparent 65%)"></div>
          <div class="absolute inset-0"
               [style]="themeService.isDark()
                 ? 'opacity:0.025;background-image:radial-gradient(circle at 1px 1px,white 1px,transparent 0);background-size:32px 32px'
                 : 'opacity:0.04;background-image:radial-gradient(circle at 1px 1px,rgba(139,92,246,0.6) 1px,transparent 0);background-size:32px 32px'">
          </div>
        </div>

        <!-- Theme toggle -->
        <div class="fixed top-5 right-5 z-50">
          <div class="flex items-center rounded-xl p-1 gap-0.5 transition-all"
               [style]="themeService.isDark()
                 ? 'background:rgba(255,255,255,0.06);border:1px solid rgba(255,255,255,0.10)'
                 : 'background:rgba(139,92,246,0.07);border:1px solid rgba(139,92,246,0.18)'">
            @for (opt of themeOpts; track opt.value) {
              <button
                class="w-8 h-7 flex items-center justify-center rounded-[9px] text-xs transition-all duration-200"
                [class]="themeService.theme() === opt.value
                  ? (themeService.isDark() ? 'bg-white/15 text-white' : 'bg-white text-violet-700 shadow-sm')
                  : (themeService.isDark() ? 'text-white/30 hover:text-white/60' : 'text-slate-400 hover:text-slate-600')"
                [pTooltip]="opt.label"
                tooltipPosition="bottom"
                (click)="themeService.set(opt.value)">
                <i [class]="opt.icon"></i>
              </button>
            }
          </div>
        </div>

        <!-- Left branding panel — always dark -->
        <div class="hidden lg:flex flex-col justify-between w-[500px] shrink-0 px-12 py-14 relative z-10"
             style="background:linear-gradient(160deg,#07040f 0%,#110922 100%)">
          <div class="flex items-center gap-3.5">
            <div class="w-11 h-11 rounded-2xl flex items-center justify-center font-bold text-white text-sm shadow-xl shadow-violet-600/30"
                 style="background:linear-gradient(135deg,#8b5cf6,#6d28d9)">SE</div>
            <div>
              <div class="text-white font-bold text-[17px] tracking-tight leading-none">ShopEarth</div>
              <div class="text-violet-400 text-[11px] font-semibold tracking-[0.15em] uppercase mt-0.5">Customer Portal</div>
            </div>
          </div>

          <div class="space-y-7">
            <div class="inline-flex items-center gap-2 bg-violet-500/10 border border-violet-500/20 rounded-full px-4 py-1.5">
              <span class="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse inline-block"></span>
              <span class="text-violet-300 text-xs font-medium">Your personal shop dashboard</span>
            </div>
            <div>
              <h1 class="text-[40px] font-extrabold leading-[1.12] tracking-tight text-white">
                Your orders,<br />invoices &amp;<br />
                <span style="background:linear-gradient(90deg,#a78bfa,#e879f9,#a78bfa);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text">
                  rewards — here.
                </span>
              </h1>
              <p class="text-slate-400 text-[15px] leading-relaxed mt-4 max-w-[340px]">
                Sign in with your mobile or email. No password needed — just a quick OTP.
              </p>
            </div>
            <ul class="space-y-3.5">
              @for (feat of features; track feat.text) {
                <li class="flex items-center gap-3.5">
                  <div class="w-9 h-9 rounded-xl bg-violet-500/10 border border-violet-500/15 flex items-center justify-center shrink-0">
                    <i [class]="feat.icon + ' text-violet-400 text-sm'"></i>
                  </div>
                  <span class="text-slate-300 text-sm">{{ feat.text }}</span>
                </li>
              }
            </ul>
            <div class="grid grid-cols-3 gap-3 pt-1">
              @for (badge of badges; track badge.label) {
                <div class="rounded-2xl px-4 py-3.5 text-center"
                     style="background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.06)">
                  <div class="text-lg font-bold text-white mb-0.5">{{ badge.value }}</div>
                  <div class="text-[11px] text-slate-500 leading-tight">{{ badge.label }}</div>
                </div>
              }
            </div>
          </div>
          <p class="text-slate-700 text-xs">© 2025 ShopEarth Technologies Pvt. Ltd.</p>
        </div>

        <!-- Divider -->
        <div class="hidden lg:block w-px self-stretch my-10 shrink-0"
             [style]="themeService.isDark()
               ? 'background:linear-gradient(to bottom,transparent,rgba(255,255,255,0.06),transparent)'
               : 'background:linear-gradient(to bottom,transparent,rgba(139,92,246,0.2),transparent)'"></div>

        <!-- Right: form area -->
        <div class="flex-1 flex flex-col items-center justify-center p-6 relative z-10">

          <!-- Mobile logo -->
          <div class="lg:hidden mb-10 text-center">
            <div class="w-14 h-14 rounded-2xl flex items-center justify-center font-bold text-white text-xl shadow-2xl shadow-violet-600/40 mx-auto mb-3"
                 style="background:linear-gradient(135deg,#8b5cf6,#6d28d9)">SE</div>
            <div class="font-bold text-lg text-slate-900 dark:text-white">ShopEarth Portal</div>
            <div class="text-violet-500 dark:text-violet-400 text-xs font-medium tracking-widest uppercase mt-1">Customer Login</div>
          </div>

          <!-- Card -->
          <div class="w-full max-w-[420px] transition-all duration-300"
               [style]="themeService.isDark()
                 ? 'background:rgba(10,8,25,0.85);backdrop-filter:blur(32px);-webkit-backdrop-filter:blur(32px);border:1px solid rgba(139,92,246,0.22);border-radius:24px;padding:40px;box-shadow:0 32px 64px rgba(0,0,0,0.6)'
                 : 'background:rgba(255,255,255,0.97);border:1px solid rgba(139,92,246,0.18);border-radius:24px;padding:40px;box-shadow:0 20px 60px rgba(139,92,246,0.10),0 4px 16px rgba(0,0,0,0.06)'">

            <!-- Step indicator -->
            <div class="flex items-center gap-2 mb-8">
              <div class="flex items-center gap-2">
                <div class="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold"
                     style="background:linear-gradient(135deg,#8b5cf6,#6d28d9);color:white;box-shadow:0 0 14px rgba(139,92,246,0.4)">
                  @if (step() === 'otp') { <i class="pi pi-check text-[10px]"></i> } @else { 1 }
                </div>
                <span class="text-xs font-semibold"
                      [class]="step() === 'identifier'
                        ? 'text-slate-900 dark:text-white'
                        : 'text-slate-400 dark:text-slate-600'">Identify</span>
              </div>
              <div class="flex-1 h-px mx-1"
                   [style]="themeService.isDark()
                     ? 'background:linear-gradient(to right,rgba(139,92,246,0.5),rgba(255,255,255,0.06))'
                     : 'background:linear-gradient(to right,rgba(139,92,246,0.4),rgba(139,92,246,0.1))'"></div>
              <div class="flex items-center gap-2">
                <div class="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold transition-all"
                     [style]="step() === 'otp'
                       ? 'background:linear-gradient(135deg,#8b5cf6,#6d28d9);color:white;box-shadow:0 0 14px rgba(139,92,246,0.4)'
                       : (themeService.isDark() ? 'background:rgba(255,255,255,0.08);color:rgba(255,255,255,0.3);border:1px solid rgba(255,255,255,0.1)' : 'background:rgba(139,92,246,0.08);color:rgba(139,92,246,0.4);border:1px solid rgba(139,92,246,0.15)')">2</div>
                <span class="text-xs font-semibold"
                      [class]="step() === 'otp'
                        ? 'text-slate-900 dark:text-white'
                        : 'text-slate-400 dark:text-slate-600'">Verify OTP</span>
              </div>
            </div>

            <!-- Step 1 -->
            @if (step() === 'identifier') {
              <div class="animate-fade-in">
                <h2 class="text-2xl font-bold tracking-tight mb-1.5 text-slate-900 dark:text-white">Sign in</h2>
                <p class="text-sm mb-7 text-slate-500 dark:text-slate-400">Enter your email or mobile number to receive an OTP.</p>

                @if (error()) {
                  <div class="mb-5 flex items-start gap-3 rounded-xl px-4 py-3 text-sm animate-fade-in"
                       style="background:rgba(239,68,68,0.10);border:1px solid rgba(239,68,68,0.28)">
                    <i class="pi pi-exclamation-circle text-red-400 mt-0.5 shrink-0"></i>
                    <span class="text-red-400">{{ error() }}</span>
                  </div>
                }

                <form (ngSubmit)="requestOtp()" class="space-y-5">
                  <div>
                    <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">Email / Mobile Number</label>
                    <input type="text" [(ngModel)]="identifier" name="identifier"
                           placeholder="you@example.com or 9876543210"
                           autocomplete="username" required
                           class="w-full h-11 px-4 rounded-xl text-sm outline-none transition-all duration-200
                                  text-slate-900 dark:text-white placeholder:text-slate-400 dark:placeholder:text-white/30"
                           [style]="themeService.isDark()
                             ? 'background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.13)'
                             : 'background:#fff;border:1px solid rgba(139,92,246,0.25);box-shadow:0 1px 3px rgba(0,0,0,0.04)'" />
                  </div>
                  <button type="submit" [disabled]="loading() || !identifier"
                          class="w-full h-12 rounded-xl text-white font-semibold text-sm flex items-center justify-center gap-2 transition-all duration-200 hover:brightness-110 active:scale-[0.99] disabled:opacity-55 disabled:cursor-not-allowed"
                          style="background:linear-gradient(135deg,#8b5cf6 0%,#6d28d9 100%);box-shadow:0 8px 24px rgba(139,92,246,0.38)">
                    @if (loading()) { <i class="pi pi-spin pi-spinner text-sm"></i> }
                    Send OTP
                  </button>
                </form>
              </div>

            <!-- Step 2 -->
            } @else {
              <div class="animate-fade-in">
                <button class="inline-flex items-center gap-1.5 text-sm mb-6 transition-colors
                               text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200"
                        type="button" (click)="step.set('identifier')">
                  <i class="pi pi-arrow-left text-xs"></i> Change identifier
                </button>

                <h2 class="text-2xl font-bold tracking-tight mb-1.5 text-slate-900 dark:text-white">Enter OTP</h2>
                <p class="text-sm mb-7 text-slate-500 dark:text-slate-400">
                  We sent a 6-digit code to <span class="font-semibold text-slate-800 dark:text-slate-200">{{ identifier }}</span>.
                </p>

                @if (error()) {
                  <div class="mb-5 flex items-start gap-3 rounded-xl px-4 py-3 text-sm animate-fade-in"
                       style="background:rgba(239,68,68,0.10);border:1px solid rgba(239,68,68,0.28)">
                    <i class="pi pi-exclamation-circle text-red-400 mt-0.5 shrink-0"></i>
                    <span class="text-red-400">{{ error() }}</span>
                  </div>
                }

                <form (ngSubmit)="verifyOtp()" class="space-y-5">
                  <div>
                    <label class="block text-[13px] font-medium mb-1.5 text-slate-700 dark:text-slate-300">One-Time Password</label>
                    <input type="text" inputmode="numeric" [(ngModel)]="otp" name="otp"
                           placeholder="0  0  0  0  0  0" maxlength="6"
                           autocomplete="one-time-code" required
                           class="w-full h-14 rounded-xl text-center text-2xl font-bold outline-none transition-all duration-200 tracking-[0.4em]
                                  text-slate-900 dark:text-white placeholder:text-slate-300 dark:placeholder:text-white/15"
                           [style]="themeService.isDark()
                             ? 'background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.13)'
                             : 'background:#fff;border:1px solid rgba(139,92,246,0.25);box-shadow:0 1px 3px rgba(0,0,0,0.04)'" />
                  </div>
                  <button type="submit" [disabled]="loading() || otp.length < 6"
                          class="w-full h-12 rounded-xl text-white font-semibold text-sm flex items-center justify-center gap-2 transition-all duration-200 hover:brightness-110 active:scale-[0.99] disabled:opacity-55 disabled:cursor-not-allowed"
                          style="background:linear-gradient(135deg,#8b5cf6 0%,#6d28d9 100%);box-shadow:0 8px 24px rgba(139,92,246,0.38)">
                    @if (loading()) { <i class="pi pi-spin pi-spinner text-sm"></i> }
                    Verify &amp; Sign In
                  </button>
                  <div class="text-center pt-1">
                    <button type="button"
                            class="text-xs transition-colors hover:underline text-violet-600 dark:text-violet-400 hover:text-violet-500 dark:hover:text-violet-300"
                            (click)="requestOtp()">Didn't receive the code? Resend</button>
                  </div>
                </form>
              </div>
            }

          </div>

          <p class="lg:hidden text-xs mt-8 text-slate-400 dark:text-slate-700">© 2025 ShopEarth Technologies</p>
        </div>

      </div>
    </div>
  `
})
export class OtpLoginComponent {
  protected readonly auth         = inject(PortalAuthService);
  protected readonly themeService = inject(ThemeService);

  protected identifier = '';
  protected otp        = '';
  protected step       = signal<Step>('identifier');
  protected loading    = signal(false);
  protected error      = signal<string | null>(null);

  protected readonly features = [
    { icon: 'pi pi-shopping-cart', text: 'View your full order history'           },
    { icon: 'pi pi-file-edit',     text: 'Download GST-compliant invoices'        },
    { icon: 'pi pi-map-marker',    text: 'Track deliveries in real time'          },
    { icon: 'pi pi-star-fill',     text: 'Manage loyalty points & wallet balance' },
  ];

  protected readonly badges = [
    { value: 'OTP',  label: 'No password' },
    { value: '100%', label: 'Secure'      },
    { value: '24/7', label: 'Available'   },
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
