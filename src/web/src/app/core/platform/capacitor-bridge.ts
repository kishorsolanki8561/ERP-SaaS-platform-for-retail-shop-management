/**
 * Capacitor runtime bridge — registered in main.ts when running inside Capacitor.
 *
 * Call `registerCapacitorBridge()` early in main.ts; it populates `window.__erpPlatform`
 * so PlatformService can detect the mobile shell without importing Capacitor directly.
 *
 * All plugin calls use dynamic imports wrapped in try/catch so the bridge degrades
 * gracefully in web/Electron builds where native Capacitor APIs are unavailable.
 */
export function registerCapacitorBridge(): void {
  if (typeof window === 'undefined') return;

  window.__erpPlatform = {
    isElectron: false,
    isCapacitor: true,

    listPrinters: async () => {
      // BLE thermal printer discovery deferred to native build phase
      return [];
    },

    printRaw: async (data) => {
      // ESC/POS via BLE thermal printer deferred to native build phase
      console.warn('[Capacitor] printRaw not yet implemented', data);
      return { ok: false, reason: 'not-implemented' };
    },

    printPdf: async (pdfBase64) => {
      try {
        const { Filesystem, Directory } = await import('@capacitor/filesystem');
        const { Share } = await import('@capacitor/share');
        const fileName = `invoice_${Date.now()}.pdf`;
        await Filesystem.writeFile({
          path: fileName,
          data: pdfBase64,
          directory: Directory.Cache,
          recursive: true,
        });
        const { uri } = await Filesystem.getUri({ path: fileName, directory: Directory.Cache });
        await Share.share({ title: 'Invoice', url: uri, dialogTitle: 'Share or Print Invoice' });
        return { ok: true };
      } catch (e) {
        console.warn('[Capacitor] printPdf error', e);
        return { ok: false, reason: String(e) };
      }
    },

    openCashDrawer: async () => {
      // Cash drawers are not applicable on mobile
      return { ok: false, reason: 'not-applicable' };
    },

    onBarcode: (callback) => {
      let active = true;

      // Attempt native MLKit camera scanning
      (async () => {
        try {
          const { BarcodeScanner } = await import('@capacitor-mlkit/barcode-scanning');
          while (active) {
            const { barcodes } = await BarcodeScanner.scan();
            if (!active) break;
            barcodes.forEach(b => callback(b.displayValue));
          }
        } catch {
          // Native scanner unavailable (web build or no camera permission) — fall back silently
        }
      })();

      // Also listen for DOM events so software barcode simulators work
      const handler = (e: CustomEvent<{ value: string }>) => callback(e.detail.value);
      window.addEventListener('barcodeScanned', handler as EventListener);

      return () => {
        active = false;
        window.removeEventListener('barcodeScanned', handler as EventListener);
      };
    },

    getAppVersion: async () => {
      try {
        const { App } = await import('@capacitor/app');
        const info = await App.getInfo();
        return info.version;
      } catch {
        return 'capacitor';
      }
    },

    getApiBaseUrl: async () => {
      try {
        const { Preferences } = await import('@capacitor/preferences');
        const { value } = await Preferences.get({ key: 'api_base_url' });
        return value ?? 'https://erp-api-staging.preptm.com';
      } catch {
        return 'https://erp-api-staging.preptm.com';
      }
    },
  };
}
