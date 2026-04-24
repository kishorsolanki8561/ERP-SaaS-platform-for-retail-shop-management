import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LoginComponent } from '../../features/auth/login/login.component';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LoginComponent],
  template: `
    <div class="flex items-center justify-center min-h-screen bg-surface-50">
      <app-login />
    </div>
  `
})
export class AuthLayoutComponent {}
