import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { PortalAuthService } from './portal-auth.service';

export const portalAuthInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(PortalAuthService).accessToken();
  if (!token) return next(req);

  return next(req.clone({
    setHeaders: { 'Authorization': `Bearer ${token}` }
  }));
};
