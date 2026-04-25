import {
  ChangeDetectionStrategy, Component,
  inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { AppRoutePaths } from '../../../shared/messages/app-routes';

interface OpenShiftForm {
  branchId: number | null;
  cashierName: string;
  openingCash: number;
}

interface DenominationRow {
  denomination: number;
  count: number;
}

const DENOMINATIONS = [2000, 500, 200, 100, 50, 20, 10, 5, 2, 1];

@Component({
  selector: 'app-open-shift',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    CardModule, InputTextModule, InputNumberModule, ButtonModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <app-page-header
      [title]="labels.openShiftTitle"
      [subtitle]="labels.openShiftSubtitle"
    />

    <div class="max-w-2xl mx-auto">
      <p-card>
        <div class="flex flex-col gap-4">
          <app-form-field [label]="labels.branchId" [required]="true">
            <p-inputNumber
              [(ngModel)]="form().branchId"
              [useGrouping]="false"
              placeholder="Branch ID"
              fluid
            />
          </app-form-field>

          <app-form-field [label]="labels.cashier" [required]="true">
            <input
              pInputText
              [(ngModel)]="form().cashierName"
              placeholder="Cashier name"
              class="w-full"
            />
          </app-form-field>

          <app-form-field [label]="labels.openingCash" [required]="true">
            <p-inputNumber
              [(ngModel)]="form().openingCash"
              mode="decimal"
              [minFractionDigits]="2"
              [maxFractionDigits]="2"
              placeholder="0.00"
              fluid
            />
          </app-form-field>

          <div>
            <p class="font-medium mb-2">Denomination Breakdown (optional)</p>
            <table class="w-full text-sm">
              <thead>
                <tr class="text-left border-b">
                  <th class="py-1">{{ labels.denomination }}</th>
                  <th class="py-1">{{ labels.count }}</th>
                  <th class="py-1 text-right">Subtotal</th>
                </tr>
              </thead>
              <tbody>
                @for (row of denominations(); track row.denomination) {
                  <tr class="border-b border-surface-100">
                    <td class="py-1">₹{{ row.denomination }}</td>
                    <td class="py-1">
                      <p-inputNumber
                        [(ngModel)]="row.count"
                        [min]="0"
                        [useGrouping]="false"
                        styleClass="w-24"
                        (ngModelChange)="updateOpeningCash()"
                      />
                    </td>
                    <td class="py-1 text-right">₹{{ (row.denomination * row.count) | number:'1.2-2' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <div class="flex justify-end gap-2 pt-2">
            <p-button
              [label]="commonLabels.cancel"
              severity="secondary"
              (click)="cancel()"
            />
            <p-button
              [label]="labels.openShift"
              icon="pi pi-play-circle"
              severity="success"
              [loading]="saving()"
              (click)="submit()"
            />
          </div>
        </div>
      </p-card>
    </div>
  `,
})
export class OpenShiftComponent {
  private readonly http    = inject(HttpClient);
  private readonly router  = inject(Router);
  private readonly toasts  = inject(MessageService);

  protected readonly labels       = AppLabels.pos;
  protected readonly commonLabels = AppLabels.common;

  protected readonly form = signal<OpenShiftForm>({
    branchId: null,
    cashierName: '',
    openingCash: 0,
  });

  protected readonly denominations = signal<DenominationRow[]>(
    DENOMINATIONS.map(d => ({ denomination: d, count: 0 })),
  );

  protected readonly saving = signal(false);

  protected updateOpeningCash(): void {
    const total = this.denominations().reduce((sum, r) => sum + r.denomination * r.count, 0);
    this.form.update(f => ({ ...f, openingCash: total }));
  }

  protected async submit(): Promise<void> {
    const f = this.form();
    if (!f.branchId || !f.cashierName.trim()) {
      this.toasts.add({ severity: 'warn', summary: 'Validation', detail: 'Branch and cashier name are [required]="true".' });
      return;
    }

    this.saving.set(true);
    try {
      const denoms = this.denominations()
        .filter(d => d.count > 0)
        .map(d => ({ denomination: d.denomination, count: d.count }));

      await firstValueFrom(this.http.post<{ value: number }>(ApiEndpoints.shift.open, {
        branchId:     f.branchId,
        cashierName:  f.cashierName,
        openingCash:  f.openingCash,
        denominations: denoms.length > 0 ? denoms : undefined,
      }));

      this.toasts.add({ severity: 'success', summary: 'Shift Opened', detail: 'Your shift has started.' });
      this.router.navigate([AppRoutePaths.pos.shifts]);
    } catch {
      this.toasts.add({ severity: 'error', summary: 'Error', detail: 'Failed to open shift.' });
    } finally {
      this.saving.set(false);
    }
  }

  protected cancel(): void {
    this.router.navigate([AppRoutePaths.pos.shifts]);
  }
}
