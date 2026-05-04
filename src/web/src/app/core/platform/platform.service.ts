import { Injectable, Signal, computed, signal } from '@angular/core';

/** Runtime platform injected by preload.js (Electron) or Capacitor. */
declare global {
  interface Window {
    __erpPlatform?: {
      isElectron: boolean;
      isCapacitor: boolean;
      printRaw?: (data: unknown) => Promise<unknown>;
      printPdf?: (pdfBase64: string) => Promise<unknown>;
      listPrinters?: () => Promise<string[]>;
      onBarcode?: (cb: (value: string) => void) => () => void;
      openCashDrawer?: (printerName: string) => Promise<unknown>;
      getAppVersion?: () => Promise<string>;
      getApiBaseUrl?: () => Promise<string>;
    };
  }
}

@Injectable({ providedIn: 'root' })
export class PlatformService {
  private readonly _platform = typeof window !== 'undefined'
    ? (window.__erpPlatform ?? null)
    : null;

  /** Running inside Electron desktop shell */
  readonly isElectron: Signal<boolean> = signal(this._platform?.isElectron ?? false);
  /** Running inside Capacitor mobile shell */
  readonly isCapacitor: Signal<boolean> = signal(this._platform?.isCapacitor ?? false);
  /** Running as a plain web browser */
  readonly isWeb = computed(() => !this.isElectron() && !this.isCapacitor());

  /** Whether native hardware (printer, cash drawer) IPC is available */
  readonly hasNativeHardware = computed(() => this.isElectron() || this.isCapacitor());

  listPrinters(): Promise<string[]> {
    return this._platform?.listPrinters?.() ?? Promise.resolve([]);
  }

  printRaw(data: unknown): Promise<unknown> {
    return this._platform?.printRaw?.(data) ?? Promise.resolve({ ok: false, reason: 'not-desktop' });
  }

  printPdf(pdfBase64: string): Promise<unknown> {
    return this._platform?.printPdf?.(pdfBase64) ?? Promise.resolve({ ok: false, reason: 'not-desktop' });
  }

  openCashDrawer(printerName: string): Promise<unknown> {
    return this._platform?.openCashDrawer?.(printerName) ?? Promise.resolve({ ok: false, reason: 'not-desktop' });
  }

  onBarcode(callback: (value: string) => void): () => void {
    return this._platform?.onBarcode?.(callback) ?? (() => {});
  }

  getAppVersion(): Promise<string> {
    return this._platform?.getAppVersion?.() ?? Promise.resolve('web');
  }
}
