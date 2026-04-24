import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';
import { AppMessages } from '../../shared/messages/app-messages';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const messageService = inject(MessageService, { optional: true });

  return next(req).pipe(
    catchError(err => {
      const message = err?.error?.errors?.[0] ?? err?.error?.message ?? AppMessages.common.error;

      if (messageService) {
        messageService.add({ severity: 'error', summary: AppMessages.toast.errorSummary, detail: message, life: 5000 });
      }

      return throwError(() => err);
    })
  );
};
