import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { PortalAuthService } from '../../core/auth/portal-auth.service';

interface NavItem { label: string; icon: string; route: string; }

@Component({
  selector: 'app-portal-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen bg-gray-50 flex flex-col">

      <!-- Top bar -->
      <header class="bg-white border-b border-gray-200 sticky top-0 z-30">
        <div class="flex items-center justify-between px-4 h-14">

          <!-- Hamburger + logo -->
          <div class="flex items-center gap-3">
            <button class="lg:hidden p-2 rounded-md hover:bg-gray-100" (click)="toggleSidebar()">
              <i class="pi pi-bars text-gray-600"></i>
            </button>
            <span class="font-bold text-purple-700 text-lg tracking-tight">ERP Portal</span>
          </div>

          <!-- User menu -->
          <div class="flex items-center gap-3">
            <span class="hidden sm:block text-sm text-gray-600 font-medium">
              {{ firstName() }}
            </span>
            <button
              (click)="logout()"
              class="flex items-center gap-1.5 text-sm text-gray-500 hover:text-red-500 transition-colors px-3 py-1.5 rounded-md hover:bg-red-50">
              <i class="pi pi-sign-out text-xs"></i>
              <span class="hidden sm:inline">Sign out</span>
            </button>
          </div>
        </div>
      </header>

      <div class="flex flex-1 overflow-hidden">

        <!-- Sidebar overlay (mobile) -->
        @if (sidebarOpen()) {
          <div
            class="fixed inset-0 bg-black/40 z-20 lg:hidden"
            (click)="sidebarOpen.set(false)">
          </div>
        }

        <!-- Sidebar -->
        <aside
          class="fixed lg:static inset-y-0 left-0 z-20 w-60 bg-white border-r border-gray-200 flex flex-col transition-transform duration-200 lg:translate-x-0"
          [class.translate-x-0]="sidebarOpen()"
          [class.-translate-x-full]="!sidebarOpen()">

          <nav class="flex-1 overflow-y-auto py-4 px-2">
            @for (item of navItems; track item.route) {
              <a
                [routerLink]="item.route"
                routerLinkActive="bg-purple-50 text-purple-700 font-semibold"
                [routerLinkActiveOptions]="{ exact: item.route === '/' }"
                (click)="sidebarOpen.set(false)"
                class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition-colors text-sm mb-0.5">
                <i class="pi {{ item.icon }} text-base w-5 text-center"></i>
                <span>{{ item.label }}</span>
              </a>
            }
          </nav>

          <div class="p-4 border-t border-gray-100">
            <p class="text-xs text-gray-400 text-center">v1.0 · Customer Portal</p>
          </div>
        </aside>

        <!-- Main content -->
        <main class="flex-1 overflow-y-auto p-4 lg:p-6">
          <router-outlet />
        </main>
      </div>
    </div>
  `
})
export class PortalShellComponent {
  private readonly auth = inject(PortalAuthService);

  readonly sidebarOpen = signal(false);
  readonly firstName = computed(() => {
    const name = this.auth.currentUser()?.displayName ?? '';
    return name.split(' ')[0];
  });

  readonly navItems: NavItem[] = [
    { label: 'Dashboard',  icon: 'pi-home',          route: '/' },
    { label: 'Purchases',  icon: 'pi-receipt',        route: '/purchases' },
    { label: 'My Orders',  icon: 'pi-shopping-cart',  route: '/orders' },
    { label: 'My Shops',   icon: 'pi-shop',           route: '/shops' },
    { label: 'Inquiries',  icon: 'pi-comments',       route: '/inquiries' },
    { label: 'Insights',   icon: 'pi-chart-line',     route: '/insights' },
    { label: 'Wallet',     icon: 'pi-wallet',         route: '/wallet' },
    { label: 'Profile',    icon: 'pi-user',           route: '/profile' },
  ];

  toggleSidebar() { this.sidebarOpen.update(v => !v); }
  logout() { this.auth.logout(); }
}
