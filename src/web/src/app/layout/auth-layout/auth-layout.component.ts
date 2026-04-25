import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TooltipModule } from 'primeng/tooltip';
import { ThemeService, Theme } from '../../core/theme/theme.service';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, TooltipModule],
  template: `
    <!-- .app-dark class is toggled by ThemeService; Tailwind dark: variants react to it -->
    <div [class.app-dark]="themeService.isDark()">
      <div class="min-h-screen flex relative overflow-hidden transition-colors duration-300"
           [style]="themeService.isDark()
             ? 'background:linear-gradient(135deg,#05050e 0%,#0c0c1e 55%,#07070f 100%)'
             : 'background:linear-gradient(135deg,#f8faff 0%,#eef2ff 60%,#f0f4ff 100%)'">

        <!-- Ambient glows -->
        <div class="pointer-events-none fixed inset-0 overflow-hidden">
          <div class="absolute -top-40 -right-20 w-[800px] h-[800px] rounded-full"
               style="background:radial-gradient(circle at center,rgba(99,102,241,0.14) 0%,transparent 65%)"></div>
          <div class="absolute -bottom-40 -left-20 w-[700px] h-[700px] rounded-full"
               style="background:radial-gradient(circle at center,rgba(139,92,246,0.09) 0%,transparent 65%)"></div>
          <div class="absolute inset-0"
               [style]="themeService.isDark()
                 ? 'opacity:0.03;background-image:radial-gradient(circle at 1px 1px,white 1px,transparent 0);background-size:32px 32px'
                 : 'opacity:0.04;background-image:radial-gradient(circle at 1px 1px,rgba(99,102,241,0.6) 1px,transparent 0);background-size:32px 32px'">
          </div>
        </div>

        <!-- Theme toggle — top right -->
        <div class="fixed top-5 right-5 z-50">
          <div class="flex items-center rounded-xl p-1 gap-0.5 transition-all"
               [style]="themeService.isDark()
                 ? 'background:rgba(255,255,255,0.06);border:1px solid rgba(255,255,255,0.10)'
                 : 'background:rgba(99,102,241,0.07);border:1px solid rgba(99,102,241,0.18)'">
            @for (opt of themeOpts; track opt.value) {
              <button
                class="w-8 h-7 flex items-center justify-center rounded-[9px] text-xs transition-all duration-200"
                [class]="themeService.theme() === opt.value
                  ? (themeService.isDark() ? 'bg-white/15 text-white' : 'bg-white text-indigo-700 shadow-sm')
                  : (themeService.isDark() ? 'text-white/30 hover:text-white/60' : 'text-slate-400 hover:text-slate-600')"
                [pTooltip]="opt.label"
                tooltipPosition="bottom"
                (click)="themeService.set(opt.value)">
                <i [class]="opt.icon"></i>
              </button>
            }
          </div>
        </div>

        <!-- Left branding panel — desktop, always dark -->
        <div class="hidden lg:flex flex-col justify-between w-[500px] shrink-0 px-12 py-14 relative z-10"
             style="background:linear-gradient(160deg,#06061a 0%,#0d0d24 100%)">

          <div class="flex items-center gap-3.5">
            <div class="w-11 h-11 rounded-2xl flex items-center justify-center font-bold text-white text-sm shadow-xl shadow-indigo-600/30"
                 style="background:linear-gradient(135deg,#6366f1,#4338ca)">SE</div>
            <div>
              <div class="text-white font-bold text-[17px] tracking-tight leading-none">ShopEarth</div>
              <div class="text-indigo-400 text-[11px] font-semibold tracking-[0.15em] uppercase mt-0.5">ERP Platform</div>
            </div>
          </div>

          <div class="space-y-7">
            <div class="inline-flex items-center gap-2 bg-indigo-500/10 border border-indigo-500/20 rounded-full px-4 py-1.5">
              <span class="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse inline-block"></span>
              <span class="text-indigo-300 text-xs font-medium">Built for Indian retail &amp; wholesale</span>
            </div>
            <div>
              <h1 class="text-[40px] font-extrabold leading-[1.12] tracking-tight text-white">
                Your entire shop,<br />
                <span style="background:linear-gradient(90deg,#818cf8,#c084fc,#818cf8);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text">
                  brilliantly managed.
                </span>
              </h1>
              <p class="text-slate-400 text-[15px] leading-relaxed mt-4 max-w-[340px]">
                GST billing, inventory, CRM, payments, and analytics — everything a modern shop needs in one platform.
              </p>
            </div>
            <ul class="space-y-3.5">
              @for (feat of features; track feat.text) {
                <li class="flex items-center gap-3.5">
                  <div class="w-9 h-9 rounded-xl bg-indigo-500/10 border border-indigo-500/15 flex items-center justify-center shrink-0">
                    <i [class]="feat.icon + ' text-indigo-400 text-sm'"></i>
                  </div>
                  <span class="text-slate-300 text-sm">{{ feat.text }}</span>
                </li>
              }
            </ul>
            <div class="grid grid-cols-3 gap-3 pt-1">
              @for (stat of stats; track stat.label) {
                <div class="rounded-2xl px-4 py-3.5"
                     style="background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.06)">
                  <div class="text-xl font-bold text-white mb-0.5">{{ stat.value }}</div>
                  <div class="text-[11px] text-slate-500 leading-tight">{{ stat.label }}</div>
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
               : 'background:linear-gradient(to bottom,transparent,rgba(99,102,241,0.2),transparent)'"></div>

        <!-- Right: form area -->
        <div class="flex-1 flex flex-col items-center justify-center p-6 relative z-10">

          <!-- Mobile logo -->
          <div class="lg:hidden mb-10 text-center">
            <div class="w-14 h-14 rounded-2xl flex items-center justify-center font-bold text-white text-xl shadow-2xl shadow-indigo-600/40 mx-auto mb-3"
                 style="background:linear-gradient(135deg,#6366f1,#4338ca)">SE</div>
            <div class="text-slate-900 dark:text-white font-bold text-lg">ShopEarth ERP</div>
            <div class="text-indigo-500 dark:text-indigo-400 text-xs font-medium tracking-widest uppercase mt-1">Staff Portal</div>
          </div>

          <!-- Card — light/dark aware -->
          <div class="w-full max-w-[420px] transition-all duration-300"
               [style]="themeService.isDark()
                 ? 'background:rgba(10,10,22,0.85);backdrop-filter:blur(32px);-webkit-backdrop-filter:blur(32px);border:1px solid rgba(99,102,241,0.22);border-radius:24px;padding:40px;box-shadow:0 32px 64px rgba(0,0,0,0.6)'
                 : 'background:rgba(255,255,255,0.97);border:1px solid rgba(99,102,241,0.18);border-radius:24px;padding:40px;box-shadow:0 20px 60px rgba(99,102,241,0.1),0 4px 16px rgba(0,0,0,0.06)'">
            <router-outlet />
          </div>

          <p class="lg:hidden text-slate-400 dark:text-slate-700 text-xs mt-8">© 2025 ShopEarth Technologies</p>
        </div>

      </div>
    </div>
  `
})
export class AuthLayoutComponent {
  protected readonly themeService = inject(ThemeService);

  protected readonly features = [
    { icon: 'pi pi-file-edit', text: 'GST-compliant billing & e-way bills'  },
    { icon: 'pi pi-box',       text: 'Real-time inventory management'        },
    { icon: 'pi pi-users',     text: 'Customer CRM & loyalty programs'       },
    { icon: 'pi pi-wallet',    text: 'Integrated payments & digital wallet'  },
    { icon: 'pi pi-shield',    text: 'Role-based access & full audit trails' },
  ];

  protected readonly stats = [
    { value: '10K+',  label: 'Daily transactions' },
    { value: '99.9%', label: 'Uptime SLA'         },
    { value: '28',    label: 'Business modules'   },
  ];

  protected readonly themeOpts: { value: Theme; icon: string; label: string }[] = [
    { value: 'light',  icon: 'pi pi-sun',     label: 'Light'  },
    { value: 'dark',   icon: 'pi pi-moon',    label: 'Dark'   },
    { value: 'system', icon: 'pi pi-desktop', label: 'System' },
  ];
}
