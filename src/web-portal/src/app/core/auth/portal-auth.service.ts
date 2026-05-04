import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiResult } from '../services/portal-api.service';

export interface PortalUser {
  customerId: number;
  displayName: string;
  email?: string;
  phone?: string;
}

interface TokenResult {
  accessToken: string;
  refreshToken: string;
  customerId: number;
  displayName: string;
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
      this.http.post('/api/portal/auth/signup-otp', { identifier })
    );
  }

  async verifyOtp(identifier: string, otp: string): Promise<void> {
    const resp = await firstValueFrom(
      this.http.post<ApiResult<TokenResult>>(
        '/api/portal/auth/verify-otp',
        { identifier, otp, deviceFingerprint: null }
      )
    );
    if (!resp.isSuccess) throw new Error(resp.errors[0] ?? 'Verification failed');

    const { accessToken, refreshToken, customerId, displayName } = resp.value;
    localStorage.setItem('portal_token', accessToken);
    localStorage.setItem('portal_refresh', refreshToken);
    const user: PortalUser = { customerId, displayName };
    localStorage.setItem('portal_user', JSON.stringify(user));
    this._token.set(accessToken);
    this._user.set(user);
    await this.router.navigate(['/']);
  }

  async refreshToken(): Promise<boolean> {
    const refresh = localStorage.getItem('portal_refresh');
    if (!refresh) return false;
    try {
      const resp = await firstValueFrom(
        this.http.post<ApiResult<TokenResult>>('/api/portal/auth/refresh', { refreshToken: refresh })
      );
      if (!resp.isSuccess) return false;
      localStorage.setItem('portal_token', resp.value.accessToken);
      localStorage.setItem('portal_refresh', resp.value.refreshToken);
      this._token.set(resp.value.accessToken);
      return true;
    } catch {
      return false;
    }
  }

  logout(): void {
    const refresh = localStorage.getItem('portal_refresh');
    if (refresh) {
      this.http.post('/api/portal/auth/logout', { refreshToken: refresh }).subscribe();
    }
    localStorage.removeItem('portal_token');
    localStorage.removeItem('portal_refresh');
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
