'use strict';
const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const Store = require('electron-store');
const { autoUpdater } = require('electron-updater');

const store = new Store({
  defaults: {
    apiBaseUrl: 'https://erp-api-staging.preptm.com',
    windowBounds: { width: 1280, height: 800 },
  },
});

let mainWindow = null;

function createWindow() {
  const { width, height } = store.get('windowBounds');

  mainWindow = new BrowserWindow({
    width,
    height,
    minWidth: 1024,
    minHeight: 600,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
    title: 'ERP SaaS',
    show: false,
  });

  // Persist window size on resize
  mainWindow.on('resize', () => {
    const [w, h] = mainWindow.getSize();
    store.set('windowBounds', { width: w, height: h });
  });

  // Load Angular build
  const angularBuild = path.join(__dirname, '../../dist/web/browser/index.html');
  mainWindow.loadFile(angularBuild).catch(() => {
    // Fall back to dev server when running in development
    mainWindow.loadURL('http://localhost:4200');
  });

  mainWindow.once('ready-to-show', () => mainWindow.show());

  mainWindow.on('closed', () => { mainWindow = null; });
}

app.whenReady().then(() => {
  createWindow();

  // Auto-updater: check for updates silently after app is ready.
  // Shows a dialog when an update is downloaded and ready to install.
  autoUpdater.checkForUpdatesAndNotify().catch(() => {
    // Ignore update errors in dev / no-network scenarios
  });

  autoUpdater.on('update-downloaded', () => {
    dialog.showMessageBox({
      type: 'info',
      title: 'Update Ready',
      message: 'A new version has been downloaded. Restart the app to apply the update.',
      buttons: ['Restart Now', 'Later'],
    }).then(({ response }) => {
      if (response === 0) autoUpdater.quitAndInstall();
    });
  });

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

// ── IPC handlers ────────────────────────────────────────────────────────────

ipcMain.handle('app:version', () => app.getVersion());
ipcMain.handle('app:apiBaseUrl', () => store.get('apiBaseUrl'));

ipcMain.handle('printer:list', async () => {
  return mainWindow?.webContents.getPrintersAsync() ?? [];
});

ipcMain.handle('printer:pdf', async (_event, pdfBase64) => {
  const pdfData = Buffer.from(pdfBase64, 'base64');
  return mainWindow?.webContents.print({
    silent: true,
    printBackground: false,
  });
});

ipcMain.handle('printer:raw', async (_event, { printerName, data }) => {
  // Raw ESC/POS bytes — forward to the thermal-print service in the renderer
  // via a helper process in production. For now, delegate back to Angular.
  return { ok: true, printerName };
});

ipcMain.handle('drawer:open', async (_event, printerName) => {
  // Send ESC/POS cash-drawer kick sequence (0x1B 0x70 0x00 0x19 0xFA)
  // In production this goes through the Node USB/serial layer.
  return { ok: true, printerName };
});
