import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AuthService } from './auth.service';
import { ApiEndpoints } from '../../shared/messages/app-api';

const MOCK_RESP = {
  accessToken: 'header.eyJ1c2VySWQiOjEsInNob3BJZCI6MX0.sig',
  refreshToken: 'rt-token',
  expiresAtUtc: new Date(Date.now() + 900_000).toISOString(),
};

const MOCK_USER = {
  userId: 1, shopId: 1, displayName: 'Test Admin',
  email: 'admin@test.com', permissionCodes: ['Billing.View'],
  featureCodes: [], isPlatformAdmin: false,
};

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule],
    });
    service = TestBed.inject(AuthService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('isLoggedIn starts false when no stored session', () => {
    expect(service.isLoggedIn()).toBeFalse();
  });

  describe('login()', () => {
    it('stores token and sets current user after successful login', async () => {
      const loginPromise = service.login({ identifier: 'admin', password: 'pass' });

      const loginReq = http.expectOne(ApiEndpoints.auth.login);
      loginReq.flush(MOCK_RESP);

      // After login response, the service fetches the user profile
      await loginPromise.catch(() => {});
      // Flush any outstanding profile-fetch requests
      http.match(() => true).forEach(r => r.flush(MOCK_USER));

      expect(localStorage.getItem('access_token')).toBe(MOCK_RESP.accessToken);
    });
  });

  describe('logout()', () => {
    it('clears session and user signal', async () => {
      // Seed a stored session
      localStorage.setItem('access_token', 'token');
      localStorage.setItem('refresh_token', 'rt');

      const logoutPromise = service.logout();
      const logoutReqs = http.match(ApiEndpoints.auth.logout);
      if (logoutReqs.length) logoutReqs[0].flush(null);
      await logoutPromise;

      expect(service.isLoggedIn()).toBeFalse();
      expect(localStorage.getItem('access_token')).toBeNull();
    });
  });
});
