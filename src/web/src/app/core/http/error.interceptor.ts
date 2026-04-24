import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const messageService = inject(MessageService, { optional: true });

  return next(req).pipe(
    catchError(err => {
      const message = err?.error?.errors?.[0] ?? err?.error?.message ?? 'An unexpected error occurred.';

      if (messageService) {
        messageService.add({ severity: 'error', summary: 'Error', detail: message, life: 5000 });
      }

      return throwError(() => err);
    })
  );
};
