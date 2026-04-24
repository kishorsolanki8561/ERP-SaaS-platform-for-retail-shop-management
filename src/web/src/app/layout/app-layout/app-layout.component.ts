import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PanelMenuModule } from 'primeng/panelmenu';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../core/auth/auth.service';
import { MenuStore, MenuItemDto } from '../../core/menu/menu.store';
import { AppLabels } from '../../shared/messages/app-messages';

@Component({
  selector: 'app-app-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, CommonModule, PanelMenuModule, ButtonModule, ToastModule],
  providers: [MessageService],
  template: `
    <p-toast />
    <div class="flex h-screen overflow-hidden">
      <!-- Sidebar -->
      <aside class="w-64 bg-surface-900 text-white flex flex-col shrink-0">
        <div class="p-4 text-xl font-semibold border-b border-surface-700">
          {{ labels.appName }}
        </div>
        <nav class="flex-1 overflow-y-auto p-2">
          <ng-container *ngFor="let group of menuStore.tree()">
            <div class="px-2 pt-4 pb-1 text-xs font-bold text-surface-400 uppercase tracking-wider">
              {{ group.label }}
            </div>
            <ng-container *ngFor="let item of group.children">
              <a [href]="item.route"
                 class="flex items-center gap-2 px-3 py-2 rounded hover:bg-surface-700 text-sm text-surface-100">
                <i [class]="item.icon"></i>
                {{ item.label }}
              </a>
            </ng-container>
          </ng-container>
        </nav>
        <div class="p-4 border-t border-surface-700">
          <div class="text-sm text-surface-300 mb-2">{{ auth.currentUser()?.displayName }}</div>
          <button pButton [label]="labels.layout.logout" severity="secondary" size="small"
                  class="w-full" (click)="logout()"></button>
        </div>
      </aside>

      <!-- Main content -->
      <main class="flex-1 overflow-y-auto bg-surface-50 p-6">
        <router-outlet />
      </main>
    </div>
  `
})
export class AppLayoutComponent implements OnInit {
  protected readonly auth = inject(AuthService);
  protected readonly menuStore = inject(MenuStore);
  protected readonly labels = AppLabels;

  ngOnInit(): void {
    this.menuStore.load();
  }

  protected async logout(): Promise<void> {
    this.menuStore.clear();
    await this.auth.logout();
  }
}
