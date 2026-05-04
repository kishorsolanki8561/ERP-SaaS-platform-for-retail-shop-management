import {
  ChangeDetectionStrategy, Component, HostListener, OnInit,
  computed, inject, signal
} from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MenuModule } from 'primeng/menu';
import { BadgeModule } from 'primeng/badge';
import { MessageService, MenuItem } from 'primeng/api';
import { AuthService } from '../../core/auth/auth.service';
import { MenuStore } from '../../core/menu/menu.store';
import { ThemeService, Theme } from '../../core/theme/theme.service';
import { BranchStore } from '../../core/branch/branch.store';
import { BranchSelectorComponent } from '../../shared/components/branch-selector/branch-selector.component';
import { OfflineBannerComponent } from '../../shared/components/offline-banner/offline-banner.component';
import { AppLabels } from '../../shared/messages/app-messages';

@Component({
  selector: 'app-app-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, CommonModule,
    ButtonModule, AvatarModule, ToastModule, TooltipModule,
    MenuModule, BadgeModule, BranchSelectorComponent, OfflineBannerComponent,
  ],
  providers: [MessageService],
  template: `
    <app-offline-banner />
    <p-toast position="top-right" />

    <!-- ─── Root shell ──────────────────────────────────────────── -->
    <div class="flex h-screen overflow-hidden bg-slate-50 dark:bg-slate-950">

      <!-- ─── Mobile backdrop ─────────────────────────────────────── -->
      @if (mobileOpen()) {
        <div
          class="fixed inset-0 z-40 bg-black/40 backdrop-blur-sm lg:hidden"
          (click)="closeMobile()">
        </div>
      }

      <!-- ─── Sidebar ──────────────────────────────────────────────── -->
      <aside
        class="flex flex-col shrink-0 bg-slate-950 z-50 transition-all duration-300 ease-in-out
               fixed inset-y-0 left-0 lg:relative lg:inset-auto"
        [class.w-64]="!collapsed()"
        [class.w-16]="collapsed() && !mobileOpen()"
        [class.-translate-x-full]="!mobileOpen() && isMobile()"
        [class.translate-x-0]="mobileOpen() || !isMobile()"
        [style.width]="isMobile() ? '260px' : undefined"
      >

        <!-- Logo row -->
        <div class="flex items-center h-16 px-4 border-b border-white/5 shrink-0 gap-3">
          <!-- Logo mark -->
          <div class="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center shrink-0
                      text-white font-bold text-sm select-none shadow-lg shadow-indigo-600/30">
            SE
          </div>
          <!-- App name — visible when expanded or on mobile -->
          @if (!collapsed() || mobileOpen()) {
            <span class="text-white font-semibold text-[15px] truncate flex-1 animate-fade-in">
              {{ labels.appName }}
            </span>
          }
          <!-- Desktop collapse toggle (chevron) -->
          <button
            class="hidden lg:flex ml-auto p-1.5 rounded-md text-slate-500 hover:text-slate-300
                   hover:bg-white/5 transition-colors shrink-0"
            (click)="toggleCollapse()"
            [pTooltip]="collapsed() ? 'Expand sidebar' : 'Collapse sidebar'"
            tooltipPosition="right">
            <i class="pi text-[11px]"
               [class.pi-chevron-left]="!collapsed()"
               [class.pi-chevron-right]="collapsed()"></i>
          </button>
          <!-- Mobile close -->
          <button
            class="lg:hidden ml-auto p-1.5 rounded-md text-slate-500 hover:text-slate-300
                   hover:bg-white/5 transition-colors"
            (click)="closeMobile()">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <!-- Navigation -->
        <nav class="flex-1 overflow-y-auto py-3 sidebar-scroll">
          @for (group of menuStore.tree(); track group.label) {
            <div class="mb-1">
              <!-- Group label -->
              @if (!collapsed() || mobileOpen()) {
                <div class="px-4 pt-4 pb-1.5 text-[10px] font-bold uppercase tracking-widest text-slate-600/80">
                  {{ group.label }}
                </div>
              } @else {
                <div class="px-3 py-2">
                  <div class="border-t border-white/5"></div>
                </div>
              }

              @for (item of group.children; track item.code) {
                <a
                  [routerLink]="item.route"
                  routerLinkActive="bg-indigo-500/10 text-indigo-300 border-indigo-500/70"
                  [routerLinkActiveOptions]="{ exact: false }"
                  class="flex items-center gap-3 mx-2 px-3 py-2.5 rounded-lg
                         text-[13px] text-slate-500 border border-transparent
                         hover:text-slate-200 hover:bg-white/5
                         transition-all duration-150 group"
                  [pTooltip]="(collapsed() && !mobileOpen()) ? item.label : ''"
                  tooltipPosition="right"
                  (click)="onNavClick()">
                  <i [class]="(item.icon ?? 'pi pi-circle') + ' text-[14px] shrink-0 transition-colors'"></i>
                  @if (!collapsed() || mobileOpen()) {
                    <span class="truncate font-medium animate-fade-in">{{ item.label }}</span>
                  }
                </a>
              }
            </div>
          }

          <!-- Menu loading skeleton -->
          @if (!menuStore.tree().length) {
            <div class="px-3 py-2 space-y-1.5">
              @for (i of [1,2,3,4,5]; track i) {
                <div class="h-9 rounded-lg bg-white/5 animate-pulse mx-1"></div>
              }
            </div>
          }
        </nav>

        <!-- User section -->
        <div class="shrink-0 border-t border-white/5 p-3">
          <button
            class="flex items-center gap-3 w-full px-2 py-2 rounded-lg
                   hover:bg-white/5 transition-colors text-left group"
            (click)="userMenu.toggle($event)">
            <p-avatar
              [label]="initials()"
              shape="circle"
              [style]="{ width: '32px', height: '32px', fontSize: '11px', background: '#4f46e5', color: '#fff', flexShrink: 0 }" />
            @if (!collapsed() || mobileOpen()) {
              <div class="flex-1 min-w-0 animate-fade-in">
                <div class="text-[13px] font-medium text-slate-300 truncate leading-snug">
                  {{ auth.currentUser()?.displayName }}
                </div>
                <div class="text-[11px] text-slate-600 truncate">
                  {{ auth.currentUser()?.email ?? '' }}
                </div>
              </div>
              <i class="pi pi-ellipsis-h text-[10px] text-slate-600
                        group-hover:text-slate-400 shrink-0 transition-colors"></i>
            }
          </button>
        </div>
      </aside>

      <!-- ─── Main content ─────────────────────────────────────────── -->
      <div class="flex-1 flex flex-col min-w-0 overflow-hidden">

        <!-- ─── Topbar ─────────────────────────────────────────────── -->
        <header class="h-16 flex items-center justify-between px-4 lg:px-5
                       bg-white dark:bg-slate-900
                       border-b border-slate-200 dark:border-slate-800
                       shrink-0 gap-3 z-30">

          <!-- Left: hamburger (visible on all screen sizes) -->
          <button
            class="w-9 h-9 flex items-center justify-center rounded-lg
                   text-slate-500 dark:text-slate-400
                   hover:bg-slate-100 dark:hover:bg-slate-800
                   transition-colors flex-shrink-0"
            (click)="toggleSidebar()"
            aria-label="Toggle menu">
            <i class="pi pi-bars text-[15px]"></i>
          </button>

          <!-- Branch selector -->
          <app-branch-selector />

          <div class="flex-1"></div>

          <!-- Right controls -->
          <div class="flex items-center gap-1">

            <!-- Theme toggle -->
            <div class="flex items-center bg-slate-100 dark:bg-slate-800 rounded-xl p-0.5 gap-0.5
                        border border-slate-200 dark:border-slate-700">
              @for (opt of themeOpts; track opt.value) {
                <button
                  class="w-8 h-7 flex items-center justify-center rounded-[10px]
                         text-xs transition-all duration-150"
                  [class.bg-white]="themeService.theme() === opt.value && !themeService.isDark()"
                  [class.shadow-sm]="themeService.theme() === opt.value"
                  [class.text-slate-900]="themeService.theme() === opt.value && !themeService.isDark()"
                  [class.text-slate-400]="themeService.theme() !== opt.value"
                  [pTooltip]="opt.label"
                  tooltipPosition="bottom"
                  (click)="themeService.set(opt.value)">
                  <i [class]="opt.icon"></i>
                </button>
              }
            </div>

            <!-- Notifications -->
            <button
              class="w-9 h-9 flex items-center justify-center rounded-xl relative
                     text-slate-500 dark:text-slate-400
                     hover:bg-slate-100 dark:hover:bg-slate-800
                     transition-colors"
              pTooltip="Notifications" tooltipPosition="bottom">
              <i class="pi pi-bell text-[14px]"></i>
            </button>

            <!-- User avatar -->
            <button
              class="flex items-center gap-2 px-2 py-1.5 rounded-xl
                     hover:bg-slate-100 dark:hover:bg-slate-800
                     transition-colors"
              (click)="userMenu.toggle($event)">
              <p-avatar
                [label]="initials()"
                shape="circle"
                [style]="{ width: '30px', height: '30px', fontSize: '11px', background: '#4f46e5', color: '#fff' }" />
              <i class="pi pi-angle-down text-[10px] text-slate-400 hidden sm:block"></i>
            </button>
          </div>
        </header>

        <!-- ─── Page content ─────────────────────────────────────── -->
        <main class="flex-1 overflow-y-auto">
          <router-outlet />
        </main>
      </div>
    </div>

    <!-- User popup menu -->
    <p-menu #userMenu [popup]="true" [model]="userMenuItems()" appendTo="body" />
  `,
  styles: [`
    /* Sidebar transition when it's position:fixed (mobile overlay) */
    aside {
      transition: transform 0.3s ease, width 0.3s ease;
    }
  `]
})
export class AppLayoutComponent implements OnInit {
  protected readonly auth         = inject(AuthService);
  protected readonly menuStore    = inject(MenuStore);
  protected readonly themeService = inject(ThemeService);
  protected readonly branchStore  = inject(BranchStore);
  protected readonly labels       = AppLabels;

  /** Desktop collapse state (persisted) */
  protected readonly collapsed = signal(
    localStorage.getItem('sidebar-collapsed') === 'true'
  );
  /** Mobile open state (never persisted) */
  protected readonly mobileOpen = signal(false);
  /** Whether we're currently on a mobile-sized viewport */
  protected readonly isMobile = signal(window.innerWidth < 1024);

  protected readonly initials = computed(() => {
    const name = this.auth.currentUser()?.displayName ?? '';
    return name.split(' ').filter(Boolean).map(p => p[0]).join('').toUpperCase().slice(0, 2) || 'U';
  });

  protected readonly themeOpts: { value: Theme; icon: string; label: string }[] = [
    { value: 'light',  icon: 'pi pi-sun',     label: 'Light'  },
    { value: 'dark',   icon: 'pi pi-moon',    label: 'Dark'   },
    { value: 'system', icon: 'pi pi-desktop', label: 'System' },
  ];

  protected readonly userMenuItems = computed<MenuItem[]>(() => [
    {
      label: this.auth.currentUser()?.displayName ?? 'Account',
      items: [
        {
          label: this.labels.layout.logout,
          icon: 'pi pi-sign-out',
          styleClass: 'text-red-500',
          command: () => this.logout(),
        },
      ],
    },
  ]);

  @HostListener('window:resize')
  onResize(): void {
    const mobile = window.innerWidth < 1024;
    this.isMobile.set(mobile);
    if (!mobile) this.mobileOpen.set(false);
  }

  ngOnInit(): void {
    this.menuStore.load();
    this.branchStore.load();
  }

  /** Single toggle button in the topbar handles both mobile and desktop */
  protected toggleSidebar(): void {
    if (this.isMobile()) {
      this.mobileOpen.set(!this.mobileOpen());
    } else {
      this.toggleCollapse();
    }
  }

  protected toggleCollapse(): void {
    const next = !this.collapsed();
    this.collapsed.set(next);
    localStorage.setItem('sidebar-collapsed', String(next));
  }

  protected closeMobile(): void {
    this.mobileOpen.set(false);
  }

  protected onNavClick(): void {
    if (this.isMobile()) this.mobileOpen.set(false);
  }

  protected async logout(): Promise<void> {
    this.menuStore.clear();
    await this.auth.logout();
  }
}
