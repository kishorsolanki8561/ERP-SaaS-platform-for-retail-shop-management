import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.erpsaas.mobile',
  appName: 'ERP SaaS',
  webDir: 'dist/web/browser',

  server: {
    // In development point to the Angular dev server.
    // In production this is unset — Capacitor serves the local build.
    // url: 'http://localhost:4200',
    cleartext: false,
  },

  plugins: {
    SplashScreen: {
      launchShowDuration: 2000,
      backgroundColor: '#1e293b',
      showSpinner: false,
    },
    PushNotifications: {
      presentationOptions: ['badge', 'sound', 'alert'],
    },
    Camera: {
      // Used for barcode scanning via BarcodeScanner plugin
    },
    Filesystem: {
      // Used for offline SQLite DB + receipt PDF export
    },
  },

  android: {
    minWebViewVersion: 95,
    backgroundColor: '#1e293b',
    allowMixedContent: false,
  },

  ios: {
    backgroundColor: '#1e293b',
    contentInset: 'automatic',
  },
};

export default config;
