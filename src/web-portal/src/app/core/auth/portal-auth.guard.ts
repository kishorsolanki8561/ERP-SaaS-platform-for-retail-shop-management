import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PortalAuthService } from './portal-auth.service';

export const portalAuthGuard: CanActivateFn = () => {
  const auth = inject(PortalAuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? true : router.createUrlTree(['/login']);
};
