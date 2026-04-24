import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export function featureGuard(requiredFeature: string): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const user = auth.currentUser();

    if (user?.featureCodes.includes(requiredFeature)) return true;
    return router.createUrlTree(['/feature-unavailable']);
  };
}
