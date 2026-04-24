import {
  ChangeDetectionStrategy, Component, OnInit,
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
import { AppLabels } from '../../shared/messages/app-messages';

@Component({
  selector: 'app-app-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive, CommonModule,
    ButtonModule, AvatarModule, ToastModule, TooltipModule,
    MenuModule, BadgeModule,
  ],
  providers: [MessageService],
  template: `
    <p-toast position="top-right" />

    <div class="flex h-screen overflow-hidden bg-slate-50 dark:bg-slate-950">

      <!-- ─── Sidebar ──────────────────────────────────────────────── -->
      <aside
        class="flex flex-col shrink-0 bg-slate-950 overflow-hidden transition-[width] duration-300 ease-in-out z-30"
        [style.width]="collapsed() ? '64px' : '260px'">

        <!-- Logo -->
        <div class="flex items-center h-16 px-4 border-b border-white/5 shrink-0">
          <div class="flex items-center gap-3 min-w-0 flex-1">
            <div class="w-8 h-8 rounded-lg bg-indigo-600 flex items-center justify-center shrink-0 text-white font-bold text-sm select-none">
              SE
            </div>
            @if (!collapsed()) {
              <span class="text-white font-semibold text-[15px] truncate animate-fade-in">
                {{ labels.appName }}
              </span>
            }
          </div>
          <button
            class="ml-auto p-1.5 rounded-md text-slate-500 hover:text-slate-300 hover:bg-white/5 transition-colors shrink-0"
            (click)="toggleCollapse()"
            [pTooltip]="collapsed() ? 'Expand' : 'Collapse'"
            tooltipPosition="right">
            <i class="pi text-xs"
               [class.pi-chevron-left]="!collapsed()"
               [class.pi-chevron-right]="collapsed()"></i>
          </button>
        </div>

        <!-- Navigation -->
        <nav class="flex-1 overflow-y-auto py-2 sidebar-scroll">
          @for (group of menuStore.tree(); track group.label) {
            <div class="mb-1">
              @if (!collapsed()) {
                <div class="px-4 pt-4 pb-1 text-[10px] font-semibold uppercase tracking-widest text-slate-600">
                  {{ group.label }}
                </div>
              } @else {
                <div class="px-3 pt-3 pb-1">
                  <div class="border-t border-white/5"></div>
                </div>
              }
              @for (item of group.children; track item.code) {
                <a
                  [routerLink]="item.route"
                  routerLinkActive="!text-indigo-300 !bg-indigo-500/10 border-r-2 border-indigo-500"
                  class="flex items-center gap-3 mx-2 px-3 py-2.5 rounded-lg text-[13px] text-slate-500 hover:text-slate-200 hover:bg-white/5 transition-all duration-150 group border-r-2 border-transparent"
                  [pTooltip]="collapsed() ? item.label : ''"
                  tooltipPosition="right">
                  <i [class]="item.icon ?? 'pi pi-circle'"
                     class="text-sm shrink-0 transition-colors"></i>
                  @if (!collapsed()) {
                    <span class="truncate font-medium animate-fade-in">{{ item.label }}</span>
                  }
                </a>
              }
            </div>
          }

          <!-- Loading skeleton when menu is empty -->
          @if (!menuStore.tree().length) {
            <div class="px-3 py-2 space-y-1">
              @for (i of [1,2,3]; track i) {
                <div class="h-9 rounded-lg bg-white/5 animate-pulse mx-1"></div>
              }
            </div>
          }
        </nav>

        <!-- User section -->
        <div class="shrink-0 border-t border-white/5 p-3">
          <button
            class="flex items-center gap-3 w-full px-2 py-2 rounded-lg hover:bg-white/5 transition-colors text-left group"
            (click)="userMenu.toggle($event)">
            <p-avatar
              [label]="initials()"
              shape="circle"
              [style]="{ width: '32px', height: '32px', fontSize: '11px', background: '#4f46e5', color: '#fff' }" />
            @if (!collapsed()) {
              <div class="flex-1 min-w-0 animate-fade-in">
                <div class="text-[13px] font-medium text-slate-300 truncate">
                  {{ auth.currentUser()?.displayName }}
                </div>
                <div class="text-[11px] text-slate-600 truncate">
                  {{ auth.currentUser()?.email ?? '' }}
                </div>
              </div>
              <i class="pi pi-ellipsis-h text-[10px] text-slate-600 group-hover:text-slate-400 shrink-0 transition-colors"></i>
            }
          </button>
        </div>
      </aside>

      <!-- ─── Content area ──────────────────────────────────────────── -->
      <div class="flex-1 flex flex-col min-w-0 overflow-hidden">

        <!-- Topbar -->
        <header class="h-16 flex items-center justify-between px-5 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 shrink-0 gap-4">

          <!-- Left: mobile sidebar toggle -->
          <button
            class="p-2 rounded-lg text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors lg:hidden"
            (click)="toggleCollapse()">
            <i class="pi pi-bars text-sm"></i>
          </button>

          <div class="flex-1"></div>

          <!-- Right: controls -->
          <div class="flex items-center gap-1.5">

            <!-- Theme toggle pills -->
            <div class="flex items-center bg-slate-100 dark:bg-slate-800 rounded-xl p-0.5 gap-0.5 border border-slate-200 dark:border-slate-700">
              @for (opt of themeOpts; track opt.value) {
                <button
                  class="w-8 h-7 flex items-center justify-center rounded-[10px] text-xs transition-all duration-150"
                  [class.bg-white]="themeService.theme() === opt.value && !themeService.isDark()"
                  [class.dark:bg-slate-700]="themeService.theme() === opt.value"
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

            <!-- Notifications -->
            <button
              class="w-9 h-9 flex items-center justify-center rounded-xl text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors relative"
              pTooltip="Notifications" tooltipPosition="bottom">
              <i class="pi pi-bell text-sm"></i>
            </button>

            <!-- User avatar -->
            <button
              class="flex items-center gap-2 px-2 py-1.5 rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              (click)="userMenu.toggle($event)">
              <p-avatar
                [label]="initials()"
                shape="circle"
                [style]="{ width: '30px', height: '30px', fontSize: '11px', background: '#4f46e5', color: '#fff' }" />
              <i class="pi pi-angle-down text-[10px] text-slate-400"></i>
            </button>
          </div>
        </header>

        <!-- Page content -->
        <main class="flex-1 overflow-y-auto">
          <router-outlet />
        </main>
      </div>
    </div>

    <!-- User popup menu -->
    <p-menu #userMenu [popup]="true" [model]="userMenuItems()" appendTo="body" />
  `
})
export class AppLayoutComponent implements OnInit {
  protected readonly auth = inject(AuthService);
  protected readonly menuStore = inject(MenuStore);
  protected readonly themeService = inject(ThemeService);
  protected readonly labels = AppLabels;

  protected readonly collapsed = signal(
    localStorage.getItem('sidebar-collapsed') === 'true'
  );

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

  ngOnInit(): void {
    this.menuStore.load();
  }

  protected toggleCollapse(): void {
    const next = !this.collapsed();
    this.collapsed.set(next);
    localStorage.setItem('sidebar-collapsed', String(next));
  }

  protected async logout(): Promise<void> {
    this.menuStore.clear();
    await this.auth.logout();
  }
}
