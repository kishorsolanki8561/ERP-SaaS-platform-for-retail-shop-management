import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const shopId = auth.currentUser()?.shopId;

  if (!shopId) return next(req);

  return next(req.clone({
    setHeaders: { 'X-Shop-Id': String(shopId) }
  }));
};
