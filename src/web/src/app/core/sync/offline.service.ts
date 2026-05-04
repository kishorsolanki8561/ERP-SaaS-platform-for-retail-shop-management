import { Injectable, Signal, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class OfflineService {
  private readonly _online = signal(typeof navigator !== 'undefined' ? navigator.onLine : true);

  readonly isOnline: Signal<boolean> = this._online.asReadonly();
  readonly isOffline = computed(() => !this._online());

  constructor() {
    if (typeof window === 'undefined') return;
    window.addEventListener('online',  () => this._online.set(true));
    window.addEventListener('offline', () => this._online.set(false));
  }
}
