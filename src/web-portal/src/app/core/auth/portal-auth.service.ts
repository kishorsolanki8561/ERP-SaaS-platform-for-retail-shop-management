import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

export interface PortalUser {
  customerId: number;
  displayName: string;
  email?: string;
  phone?: string;
}

@Injectable({ providedIn: 'root' })
export class PortalAuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _user = signal<PortalUser | null>(this.loadStoredUser());
  private readonly _token = signal<string | null>(localStorage.getItem('portal_token'));

  readonly currentUser = this._user.asReadonly();
  readonly accessToken = this._token.asReadonly();
  readonly isLoggedIn = computed(() => this._user() !== null);

  async requestOtp(identifier: string): Promise<void> {
    await firstValueFrom(
      this.http.post('/api/portal/auth/request-otp', { identifier })
    );
  }

  async verifyOtp(identifier: string, otp: string): Promise<void> {
    const resp = await firstValueFrom(
      this.http.post<{ accessToken: string; customer: PortalUser }>(
        '/api/portal/auth/verify-otp', { identifier, otp }
      )
    );
    localStorage.setItem('portal_token', resp.accessToken);
    localStorage.setItem('portal_user', JSON.stringify(resp.customer));
    this._token.set(resp.accessToken);
    this._user.set(resp.customer);
    await this.router.navigate(['/']);
  }

  logout(): void {
    localStorage.removeItem('portal_token');
    localStorage.removeItem('portal_user');
    this._token.set(null);
    this._user.set(null);
    this.router.navigate(['/login']);
  }

  private loadStoredUser(): PortalUser | null {
    const raw = localStorage.getItem('portal_user');
    if (!raw) return null;
    try { return JSON.parse(raw) as PortalUser; }
    catch { return null; }
  }
}
