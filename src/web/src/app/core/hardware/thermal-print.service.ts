import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiEndpoints } from '../../shared/messages/app-api';

@Injectable({ providedIn: 'root' })
export class ThermalPrintService {
  private readonly http = inject(HttpClient);

  printInvoice(invoiceId: number | string, format: 'A4' | 'Thermal80mm' = 'Thermal80mm'): void {
    const url = ApiEndpoints.billing.invoicePdf(invoiceId, format);
    this.http.get(url, { responseType: 'blob' }).subscribe(blob => this._printBlob(blob));
  }

  private _printBlob(blob: Blob): void {
    const objectUrl = URL.createObjectURL(blob);
    const iframe = document.createElement('iframe');
    iframe.style.display = 'none';
    iframe.src = objectUrl;
    document.body.appendChild(iframe);
    iframe.onload = () => {
      iframe.contentWindow?.print();
      setTimeout(() => {
        document.body.removeChild(iframe);
        URL.revokeObjectURL(objectUrl);
      }, 5000);
    };
  }
}
