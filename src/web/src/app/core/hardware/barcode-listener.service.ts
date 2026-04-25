import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { fromEvent } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BarcodeListenerService {
  // Barcode scanners (keyboard-wedge) fire chars < 50ms apart and terminate with Enter
  private readonly THRESHOLD_MS = 50;
  private readonly MIN_LENGTH = 4;

  private _buf = '';
  private _t0 = 0;
  private _active = false;

  readonly scanned = signal<string | null>(null);

  constructor() {
    const dr = inject(DestroyRef);
    const sub = fromEvent<KeyboardEvent>(document, 'keydown').subscribe(e => this._handle(e));
    dr.onDestroy(() => sub.unsubscribe());
  }

  enable(): void {
    this._active = true;
    this._buf = '';
  }

  disable(): void {
    this._active = false;
  }

  clear(): void {
    this.scanned.set(null);
  }

  private _handle(e: KeyboardEvent): void {
    if (!this._active) return;

    const now = Date.now();

    if (e.key === 'Enter') {
      if (this._buf.length >= this.MIN_LENGTH) {
        this.scanned.set(this._buf);
      }
      this._buf = '';
      return;
    }

    if (e.key.length !== 1) return;

    // Large gap between keystrokes = manual typing, discard incomplete buffer
    if (this._buf.length > 0 && now - this._t0 > this.THRESHOLD_MS) {
      this._buf = '';
    }

    this._buf += e.key;
    this._t0 = now;
  }
}
