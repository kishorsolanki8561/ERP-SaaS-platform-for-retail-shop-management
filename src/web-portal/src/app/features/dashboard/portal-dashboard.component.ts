import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PortalAuthService } from '../../core/auth/portal-auth.service';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule],
  template: `
    <div class="p-6">
      <h1 class="text-2xl font-semibold text-surface-800">Welcome, {{ auth.currentUser()?.displayName }}</h1>
      <p class="text-surface-500 mt-1">Your orders and account details will appear here.</p>
      <p-button label="Logout" severity="secondary" size="small" class="mt-4" (onClick)="auth.logout()" />
    </div>
  `
})
export class PortalDashboardComponent {
  protected readonly auth = inject(PortalAuthService);
}
