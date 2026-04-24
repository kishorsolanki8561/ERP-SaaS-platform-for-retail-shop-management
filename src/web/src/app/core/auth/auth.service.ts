import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../../shared/messages/app-api';
import { AppRoutePaths } from '../../shared/messages/app-routes';

export interface AuthUser {
  userId: number;
  shopId: number;
  displayName: string;
  email?: string;
  permissionCodes: string[];
  featureCodes: string[];
}

export interface LoginRequest {
  identifier: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _user = signal<AuthUser | null>(this.loadStoredUser());
  private readonly _accessToken = signal<string | null>(localStorage.getItem('access_token'));

  readonly currentUser = this._user.asReadonly();
  readonly accessToken = this._accessToken.asReadonly();
  readonly isLoggedIn = computed(() => this._user() !== null);

  async login(request: LoginRequest): Promise<void> {
    const resp = await firstValueFrom(
      this.http.post<LoginResponse>(ApiEndpoints.auth.login, request)
    );
    this.storeSession(resp);
  }

  async refresh(): Promise<boolean> {
    const refreshToken = localStorage.getItem('refresh_token');
    if (!refreshToken) return false;

    try {
      const resp = await firstValueFrom(
        this.http.post<LoginResponse>(ApiEndpoints.auth.refresh, { refreshToken })
      );
      this.storeSession(resp);
      return true;
    } catch {
      this.clearSession();
      return false;
    }
  }

  async logout(): Promise<void> {
    const refreshToken = localStorage.getItem('refresh_token');
    if (refreshToken) {
      try {
        await firstValueFrom(this.http.post(ApiEndpoints.auth.logout, { refreshToken }));
      } catch { /* best-effort */ }
    }
    this.clearSession();
    await this.router.navigate([AppRoutePaths.login]);
  }

  private storeSession(resp: LoginResponse): void {
    localStorage.setItem('access_token', resp.accessToken);
    localStorage.setItem('refresh_token', resp.refreshToken);
    this._accessToken.set(resp.accessToken);

    const user = this.parseJwt(resp.accessToken);
    this._user.set(user);
    if (user) {
      localStorage.setItem('auth_user', JSON.stringify(user));
    }
  }

  private clearSession(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('auth_user');
    this._accessToken.set(null);
    this._user.set(null);
  }

  private loadStoredUser(): AuthUser | null {
    const raw = localStorage.getItem('auth_user');
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }

  private parseJwt(token: string): AuthUser | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        userId: Number(payload['sub']),
        shopId: Number(payload['shop_id'] ?? 0),
        displayName: payload['name'] ?? '',
        email: payload['email'],
        permissionCodes: (payload['perms'] ?? '').split(',').filter(Boolean),
        featureCodes: (payload['feats'] ?? '').split(',').filter(Boolean),
      };
    } catch {
      return null;
    }
  }
}
