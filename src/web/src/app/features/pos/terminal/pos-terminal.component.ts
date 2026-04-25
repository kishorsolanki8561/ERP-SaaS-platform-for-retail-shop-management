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
import { CardModule } from 'primeng/card';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { BarcodeListenerService, CashDrawerService, ThermalPrintService } from '../../../core/hardware';
import { AppLabels } from '../../../shared/messages/app-messages';

@Component({
  selector: 'app-pos-terminal',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, CardModule, PageHeaderComponent],
  template: `
    <app-page-header
      [title]="labels.terminalTitle"
      [subtitle]="labels.terminalSubtitle"
    />

    <div class="grid mt-3">
      <!-- Barcode input -->
      <div class="col-12 md:col-6">
        <p-card>
          <ng-template pTemplate="header">
            <div class="px-3 pt-3 font-semibold">{{ labels.scanBarcode }}</div>
          </ng-template>
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
            <div class="mt-2 text-sm text-color-secondary">
              Last scanned: <strong>{{ lastScanned() }}</strong>
            </div>
          }
        </p-card>
      </div>

      <!-- Hardware actions -->
      <div class="col-12 md:col-6">
        <p-card>
          <ng-template pTemplate="header">
            <div class="px-3 pt-3 font-semibold">Hardware</div>
          </ng-template>
          <div class="flex flex-column gap-2">
            @if (lastInvoiceId()) {
              <p-button
                [label]="labels.printReceipt"
                icon="pi pi-print"
                styleClass="w-full"
                (onClick)="printReceipt()"
              />
            } @else {
              <div class="text-sm text-color-secondary">{{ labels.noInvoice }}</div>
            }
            <p-button
              [label]="labels.popDrawer"
              icon="pi pi-inbox"
              severity="secondary"
              styleClass="w-full"
              [loading]="drawerPopping()"
              (onClick)="popDrawer()"
            />
          </div>
        </p-card>
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
    // Product lookup wired here by the caller or parent in a full checkout flow
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
