import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();

  if (!token) return next(req);

  const user = auth.currentUser();
  const headers: Record<string, string> = { Authorization: `Bearer ${token}` };
  if (user?.shopId) {
    headers['X-Shop-Id'] = String(user.shopId);
  }

  return next(req.clone({ setHeaders: headers }));
};
