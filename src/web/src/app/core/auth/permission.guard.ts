import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export function permissionGuard(requiredPermission: string): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const user = auth.currentUser();

    if (user?.isPlatformAdmin) return true;
    if (user?.permissionCodes.includes(requiredPermission)) return true;
    return router.createUrlTree(['/unauthorized']);
  };
}
