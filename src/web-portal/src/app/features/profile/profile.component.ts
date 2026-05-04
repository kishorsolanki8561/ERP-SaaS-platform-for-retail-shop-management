import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { SlicePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PortalAuthService } from '../../core/auth/portal-auth.service';
import { PortalApiService, CustomerProfile } from '../../core/services/portal-api.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, SlicePipe],
  template: `
    <div class="space-y-4 max-w-lg">

      <h1 class="text-xl font-bold text-gray-900">Profile</h1>

      <div class="bg-white rounded-xl border border-gray-200 p-6 space-y-5">

        @if (loading()) {
          <div class="animate-pulse space-y-4">
            <div class="h-5 bg-gray-100 rounded w-1/3"></div>
            <div class="h-10 bg-gray-100 rounded"></div>
            <div class="h-10 bg-gray-100 rounded"></div>
          </div>
        } @else {
          <!-- Avatar initials -->
          <div class="flex items-center gap-4">
            <div class="w-14 h-14 rounded-full bg-purple-600 flex items-center justify-center text-white text-xl font-bold">
              {{ initials() }}
            </div>
            <div>
              <p class="font-semibold text-gray-800">{{ profile()?.displayName }}</p>
              <p class="text-xs text-gray-400 mt-0.5">Customer since {{ profile()?.createdAtUtc | slice:0:10 }}</p>
            </div>
          </div>

          <hr class="border-gray-100" />

          <div class="space-y-4">
            <div>
              <label class="text-xs font-medium text-gray-600 mb-1.5 block">Display Name</label>
              <input type="text" [(ngModel)]="form.displayName"
                class="w-full border border-gray-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-purple-300" />
            </div>

            <div>
              <label class="text-xs font-medium text-gray-600 mb-1.5 block">Email Address</label>
              <input type="email" [(ngModel)]="form.email"
                placeholder="your@email.com (optional)"
                class="w-full border border-gray-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-purple-300" />
            </div>

            <div>
              <label class="text-xs font-medium text-gray-600 mb-1.5 block">Phone / Identifier</label>
              <input type="text" [value]="profile()?.phone ?? ''" readonly
                class="w-full border border-gray-100 bg-gray-50 rounded-lg px-3 py-2.5 text-sm text-gray-500 cursor-not-allowed" />
              <p class="text-xs text-gray-400 mt-1">Your phone / email used for login cannot be changed.</p>
            </div>
          </div>

          @if (saveError()) {
            <p class="text-xs text-red-500">{{ saveError() }}</p>
          }
          @if (saveSuccess()) {
            <p class="text-xs text-green-600">Profile updated successfully.</p>
          }

          <div class="pt-1">
            <button (click)="save()"
              [disabled]="saving()"
              class="bg-purple-600 hover:bg-purple-700 text-white text-sm font-medium px-6 py-2.5 rounded-lg transition-colors disabled:opacity-60">
              {{ saving() ? 'Saving...' : 'Save Changes' }}
            </button>
          </div>
        }
      </div>

      <!-- Danger zone -->
      <div class="bg-white rounded-xl border border-red-100 p-6">
        <h2 class="font-semibold text-gray-800 mb-1 text-sm">Sign out</h2>
        <p class="text-xs text-gray-400 mb-4">You will be redirected to the login page.</p>
        <button (click)="auth.logout()"
          class="text-sm text-red-500 hover:text-red-600 border border-red-200 hover:border-red-300 px-4 py-2 rounded-lg transition-colors">
          Sign out
        </button>
      </div>
    </div>
  `
})
export class ProfileComponent implements OnInit {
  readonly auth = inject(PortalAuthService);
  private readonly api = inject(PortalApiService);

  readonly profile = signal<CustomerProfile | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly saveError = signal('');
  readonly saveSuccess = signal(false);

  form = { displayName: '', email: '' };

  readonly initials = () => {
    const name = this.profile()?.displayName ?? '';
    return name.split(' ').map((w: string) => w[0] ?? '').slice(0, 2).join('').toUpperCase();
  };

  async ngOnInit() {
    try {
      const r = await this.api.getMe();
      if (r.isSuccess) {
        this.profile.set(r.value);
        this.form.displayName = r.value.displayName;
        this.form.email = r.value.email ?? '';
      }
    } finally {
      this.loading.set(false);
    }
  }

  async save() {
    if (!this.form.displayName.trim()) {
      this.saveError.set('Display name is required.');
      return;
    }
    this.saving.set(true);
    this.saveError.set('');
    this.saveSuccess.set(false);
    try {
      const r = await this.api.updateMe(this.form.displayName, this.form.email || null);
      if (r.isSuccess) {
        this.saveSuccess.set(true);
        const updated = await this.api.getMe();
        if (updated.isSuccess) this.profile.set(updated.value);
      } else {
        this.saveError.set(r.errors?.[0] ?? 'Update failed.');
      }
    } finally {
      this.saving.set(false);
    }
  }
}
