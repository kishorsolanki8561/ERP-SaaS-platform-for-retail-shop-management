import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { BranchStore } from '../branch/branch.store';

export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const branchStore = inject(BranchStore);
  const shopId = auth.currentUser()?.shopId;

  if (!shopId) return next(req);

  const headers: Record<string, string> = { 'X-Shop-Id': String(shopId) };
  const branchId = branchStore.activeBranchId();
  if (branchId) headers['X-Branch-Id'] = String(branchId);

  return next(req.clone({ setHeaders: headers }));
};
