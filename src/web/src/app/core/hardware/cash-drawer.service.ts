import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { finalize } from 'rxjs';
import { ApiEndpoints } from '../../shared/messages/app-api';

@Injectable({ providedIn: 'root' })
export class CashDrawerService {
  private readonly http = inject(HttpClient);

  readonly popping = signal(false);

  pop(): void {
    if (this.popping()) return;
    this.popping.set(true);
    this.http
      .post(ApiEndpoints.hardware.cashDrawerPop, {})
      .pipe(finalize(() => this.popping.set(false)))
      .subscribe();
  }
}
