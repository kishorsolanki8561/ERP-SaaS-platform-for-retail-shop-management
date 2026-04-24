import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CardModule } from 'primeng/card';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardModule],
  template: `
    <div>
      <h2 class="text-2xl font-semibold mb-6">Dashboard</h2>
      <p-card>
        <p class="text-surface-600">
          Welcome, <strong>{{ auth.currentUser()?.displayName }}</strong>.
          Your shop dashboard will appear here in Phase 1.
        </p>
      </p-card>
    </div>
  `
})
export class DashboardComponent {
  protected readonly auth = inject(AuthService);
}
