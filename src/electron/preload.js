'use strict';
const { contextBridge, ipcRenderer } = require('electron');

// Expose a safe IPC bridge to the Angular app.
// Angular PlatformService reads window.__erpPlatform to detect Electron.
contextBridge.exposeInMainWorld('__erpPlatform', {
  isElectron: true,
  isCapacitor: false,

  // ── Printer IPC ────────────────────────────────────────────────────────────
  printRaw: (data) => ipcRenderer.invoke('printer:raw', data),
  printPdf: (pdfBase64) => ipcRenderer.invoke('printer:pdf', pdfBase64),
  listPrinters: () => ipcRenderer.invoke('printer:list'),

  // ── Barcode scanner IPC ───────────────────────────────────────────────────
  onBarcode: (callback) => {
    ipcRenderer.on('barcode:scan', (_event, value) => callback(value));
    return () => ipcRenderer.removeAllListeners('barcode:scan');
  },

  // ── Cash drawer IPC ───────────────────────────────────────────────────────
  openCashDrawer: (printerName) => ipcRenderer.invoke('drawer:open', printerName),

  // ── App info ──────────────────────────────────────────────────────────────
  getAppVersion: () => ipcRenderer.invoke('app:version'),
  getApiBaseUrl: () => ipcRenderer.invoke('app:apiBaseUrl'),
});
