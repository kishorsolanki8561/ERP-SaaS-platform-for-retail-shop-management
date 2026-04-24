import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService, Theme } from '../../core/theme/theme.service';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, TooltipModule],
  template: `
    <div class="min-h-screen flex bg-white dark:bg-slate-950">

      <!-- ─── Left branding panel ──────────────────────────────────── -->
      <div class="hidden lg:flex flex-col justify-between w-[480px] shrink-0
                  bg-gradient-to-br from-indigo-950 via-indigo-900 to-slate-900
                  p-10 relative overflow-hidden">

        <!-- Background decoration -->
        <div class="absolute inset-0 pointer-events-none">
          <div class="absolute top-[-120px] left-[-80px] w-[400px] h-[400px] rounded-full bg-indigo-500/10 blur-3xl"></div>
          <div class="absolute bottom-[-80px] right-[-60px] w-[300px] h-[300px] rounded-full bg-violet-500/10 blur-3xl"></div>
          <div class="absolute inset-0"
               style="background-image: radial-gradient(circle at 2px 2px, rgba(255,255,255,0.04) 1px, transparent 0); background-size: 40px 40px;"></div>
        </div>

        <!-- Logo -->
        <div class="relative z-10">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-white/10 backdrop-blur flex items-center justify-center font-bold text-white text-base border border-white/10">
              SE
            </div>
            <span class="text-white font-semibold text-lg">ShopEarth ERP</span>
          </div>
        </div>

        <!-- Tagline + features -->
        <div class="relative z-10">
          <h1 class="text-3xl font-bold text-white leading-snug mb-3">
            Manage your shop<br />with confidence.
          </h1>
          <p class="text-indigo-300 text-sm mb-8 leading-relaxed">
            The complete ERP solution for modern retail and wholesale businesses.
          </p>
          <ul class="space-y-3">
            @for (feat of features; track feat) {
              <li class="flex items-center gap-3 text-sm text-indigo-200">
                <span class="w-5 h-5 rounded-full bg-indigo-500/30 flex items-center justify-center shrink-0">
                  <i class="pi pi-check text-[9px] text-indigo-300"></i>
                </span>
                {{ feat }}
              </li>
            }
          </ul>
        </div>

        <!-- Footer -->
        <div class="relative z-10">
          <p class="text-indigo-600 text-xs">© 2025 ShopEarth Technologies</p>
        </div>
      </div>

      <!-- ─── Right form panel ──────────────────────────────────────── -->
      <div class="flex-1 flex flex-col">

        <!-- Theme toggle in top-right corner -->
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

        <!-- Centered form -->
        <div class="flex-1 flex items-center justify-center p-6">
          <div class="w-full max-w-[400px]">
            <router-outlet />
          </div>
        </div>
      </div>
    </div>
  `
})
export class AuthLayoutComponent {
  protected readonly themeService = inject(ThemeService);

  protected readonly features = [
    'GST-compliant invoicing & e-way bills',
    'Real-time inventory across locations',
    'Customer CRM with loyalty programs',
    'Integrated payment & wallet',
    'Multi-user with role-based access',
  ];

  protected readonly themeOpts: { value: Theme; icon: string; label: string }[] = [
    { value: 'light',  icon: 'pi pi-sun',     label: 'Light'  },
    { value: 'dark',   icon: 'pi pi-moon',    label: 'Dark'   },
    { value: 'system', icon: 'pi pi-desktop', label: 'System' },
  ];
}
