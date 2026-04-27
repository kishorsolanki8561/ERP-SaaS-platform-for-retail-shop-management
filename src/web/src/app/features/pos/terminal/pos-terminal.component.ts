import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { BarcodeListenerService, CashDrawerService, ThermalPrintService } from '../../../core/hardware';
import { AppLabels } from '../../../shared/messages/app-messages';

@Component({
  selector: 'app-pos-terminal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, PageHeaderComponent],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.terminalTitle"
        [subtitle]="labels.terminalSubtitle"
      />

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <!-- Barcode input -->
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 space-y-4">
          <div class="font-semibold text-slate-900 dark:text-white">{{ labels.scanBarcode }}</div>
          <div class="flex gap-2">
            <input
              pInputText
              class="flex-1"
              [placeholder]="labels.scanBarcode"
              [(ngModel)]="manualBarcode"
              (keydown.enter)="onManualBarcode()"
            />
            <p-button
              label="Search"
              icon="pi pi-search"
              (onClick)="onManualBarcode()"
            />
          </div>
          @if (lastScanned()) {
            <div class="flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400
                        bg-slate-50 dark:bg-slate-800 rounded-lg px-3 py-2">
              <i class="pi pi-barcode text-indigo-500"></i>
              Last scanned: <strong class="text-slate-700 dark:text-slate-200">{{ lastScanned() }}</strong>
            </div>
          }
        </div>

        <!-- Hardware actions -->
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 space-y-4">
          <div class="font-semibold text-slate-900 dark:text-white">Hardware</div>
          <div class="flex flex-col gap-3">
            @if (lastInvoiceId()) {
              <p-button
                [label]="labels.printReceipt"
                icon="pi pi-print"
                styleClass="w-full justify-center"
                (onClick)="printReceipt()"
              />
            } @else {
              <div class="flex items-center gap-2 text-sm text-slate-400 bg-slate-50 dark:bg-slate-800 rounded-lg px-3 py-2">
                <i class="pi pi-info-circle"></i>
                {{ labels.noInvoice }}
              </div>
            }
            <p-button
              [label]="labels.popDrawer"
              icon="pi pi-inbox"
              severity="secondary"
              styleClass="w-full justify-center"
              [loading]="drawerPopping()"
              (onClick)="popDrawer()"
            />
          </div>
        </div>
      </div>
    </div>
  `,
})
export class PosTerminalComponent implements OnInit, OnDestroy {
  private readonly barcodeListener = inject(BarcodeListenerService);
  private readonly thermalPrint    = inject(ThermalPrintService);
  private readonly cashDrawer      = inject(CashDrawerService);
  private readonly toast           = inject(MessageService);

  readonly labels       = { ...AppLabels.pos };
  readonly drawerPopping = this.cashDrawer.popping;
  readonly lastScanned  = this.barcodeListener.scanned;

  lastInvoiceId = signal<number | null>(null);
  manualBarcode = '';

  constructor() {
    effect(() => {
      const code = this.barcodeListener.scanned();
      if (code) {
        this.onBarcodeScanned(code);
        this.barcodeListener.clear();
      }
    });
  }

  ngOnInit(): void {
    this.barcodeListener.enable();
  }

  ngOnDestroy(): void {
    this.barcodeListener.disable();
  }

  onManualBarcode(): void {
    const code = this.manualBarcode.trim();
    if (!code) return;
    this.onBarcodeScanned(code);
    this.manualBarcode = '';
  }

  onBarcodeScanned(code: string): void {
    this.toast.add({
      severity: 'info',
      summary: 'Barcode',
      detail: `Scanned: ${code}`,
      life: 3000,
    });
  }

  printReceipt(): void {
    const id = this.lastInvoiceId();
    if (!id) return;
    this.thermalPrint.printInvoice(id, 'Thermal80mm');
  }

  popDrawer(): void {
    this.cashDrawer.pop();
  }
}
