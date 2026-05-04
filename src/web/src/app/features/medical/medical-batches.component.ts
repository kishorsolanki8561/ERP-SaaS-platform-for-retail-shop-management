import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { DropdownModule } from 'primeng/dropdown';
import { ApiEndpoints } from '../../shared/messages/app-api';
import { catchError, of } from 'rxjs';

interface DrugBatch {
  id: number;
  batchNumber: string;
  productId: number;
  productNameSnapshot: string;
  genericName: string;
  manufacturer: string;
  schedule: string;
  manufactureDate: string;
  expiryDate: string;
  initialQuantity: number;
  currentQuantity: number;
  purchasePrice: number;
  sellingPrice: number;
  isActive: boolean;
  daysToExpiry: number;
}

@Component({
  selector: 'app-medical-batches',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, ButtonModule, TableModule, TagModule, DialogModule, InputTextModule, InputNumberModule, CalendarModule, DropdownModule],
  template: `
    <div class="p-4">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-xl font-semibold">Drug Batch Inventory</h2>
        <div class="flex gap-2">
          <p-button label="Expiring Soon" icon="pi pi-exclamation-triangle" severity="warn" [outlined]="true" (onClick)="loadExpiring()" />
          <p-button label="Add Batch" icon="pi pi-plus" (onClick)="openNew()" />
        </div>
      </div>

      <p-table [value]="batches()" [loading]="loading()" dataKey="id" responsiveLayout="scroll">
        <ng-template pTemplate="header">
          <tr>
            <th>Batch #</th>
            <th>Product</th>
            <th>Generic Name</th>
            <th>Schedule</th>
            <th>Expiry</th>
            <th>Stock</th>
            <th>MRP</th>
            <th>Status</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-b>
          <tr>
            <td><code>{{ b.batchNumber }}</code></td>
            <td>{{ b.productNameSnapshot }}</td>
            <td>{{ b.genericName || '—' }}</td>
            <td>
              <p-tag [value]="'Sch-' + b.schedule"
                [severity]="b.schedule === 'H' || b.schedule === 'H1' || b.schedule === 'X' ? 'danger' : 'secondary'" />
            </td>
            <td [class.text-red-500]="b.daysToExpiry < 30">
              {{ b.expiryDate | date:'dd MMM yyyy' }}
              @if(b.daysToExpiry < 30) { <span class="text-xs">({{ b.daysToExpiry }}d)</span> }
            </td>
            <td>{{ b.currentQuantity | number:'1.0-2' }} / {{ b.initialQuantity | number:'1.0-2' }}</td>
            <td>₹{{ b.sellingPrice | number:'1.2-2' }}</td>
            <td><p-tag [value]="b.isActive ? 'Active' : 'Inactive'" [severity]="b.isActive ? 'success' : 'secondary'" /></td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr><td colspan="8" class="text-center py-8 text-gray-400">No drug batches found</td></tr>
        </ng-template>
      </p-table>
    </div>

    <p-dialog header="Add Drug Batch" [(visible)]="showNew" [modal]="true" [style]="{width:'520px'}">
      <div class="grid grid-cols-2 gap-3 p-2">
        <div class="flex flex-col gap-1 col-span-2">
          <label>Product ID</label>
          <p-inputNumber [(ngModel)]="dto.productId" [min]="1" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Batch Number</label>
          <input pInputText [(ngModel)]="dto.batchNumber" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Schedule</label>
          <p-dropdown [(ngModel)]="dto.schedule" [options]="scheduleOptions" optionLabel="label" optionValue="value" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Manufacture Date</label>
          <p-calendar [(ngModel)]="dto.manufactureDate" dateFormat="dd/mm/yy" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Expiry Date</label>
          <p-calendar [(ngModel)]="dto.expiryDate" dateFormat="dd/mm/yy" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Initial Qty</label>
          <p-inputNumber [(ngModel)]="dto.initialQuantity" [min]="0" [minFractionDigits]="2" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Purchase Price</label>
          <p-inputNumber [(ngModel)]="dto.purchasePrice" [min]="0" mode="currency" currency="INR" locale="en-IN" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Selling Price</label>
          <p-inputNumber [(ngModel)]="dto.sellingPrice" [min]="0" mode="currency" currency="INR" locale="en-IN" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Generic Name</label>
          <input pInputText [(ngModel)]="dto.genericName" />
        </div>
        <div class="flex flex-col gap-1">
          <label>Manufacturer</label>
          <input pInputText [(ngModel)]="dto.manufacturer" />
        </div>
      </div>
      <ng-template pTemplate="footer">
        <p-button label="Cancel" [text]="true" (onClick)="showNew = false" />
        <p-button label="Add Batch" icon="pi pi-check" (onClick)="saveBatch()" />
      </ng-template>
    </p-dialog>
  `,
})
export class MedicalBatchesComponent {
  private http = inject(HttpClient);

  loading = signal(false);
  batches = signal<DrugBatch[]>([]);
  showNew = false;

  scheduleOptions = [
    { label: 'None', value: 'None' }, { label: 'G', value: 'G' }, { label: 'C', value: 'C' },
    { label: 'C1', value: 'C1' }, { label: 'H', value: 'H' }, { label: 'H1', value: 'H1' }, { label: 'X', value: 'X' },
  ];

  dto = this.blankDto();

  constructor() { this.loadBatches(); }

  blankDto() {
    return { productId: 0, batchNumber: '', schedule: 'None', genericName: '', manufacturer: '',
      manufactureDate: null as Date | null, expiryDate: null as Date | null,
      initialQuantity: 0, purchasePrice: 0, sellingPrice: 0 };
  }

  loadBatches() {
    this.loading.set(true);
    this.http.get<DrugBatch[]>(ApiEndpoints.medical.batches).pipe(catchError(() => of([])))
      .subscribe(data => { this.batches.set(data); this.loading.set(false); });
  }

  loadExpiring() {
    this.loading.set(true);
    this.http.get<DrugBatch[]>(`${ApiEndpoints.medical.expiring}?days=30`).pipe(catchError(() => of([])))
      .subscribe(data => { this.batches.set(data); this.loading.set(false); });
  }

  openNew() { this.dto = this.blankDto(); this.showNew = true; }

  saveBatch() {
    this.http.post(ApiEndpoints.medical.batches, this.dto).subscribe(() => { this.showNew = false; this.loadBatches(); });
  }
}
