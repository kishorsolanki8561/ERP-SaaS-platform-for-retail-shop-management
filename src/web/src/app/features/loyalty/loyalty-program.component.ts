import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { TableModule } from 'primeng/table';
import { ApiEndpoints } from '../../shared/messages/app-api';
import { catchError, of } from 'rxjs';

interface LoyaltyProgram {
  id: number;
  name: string;
  pointsPerRupee: number;
  rupeeValuePerPoint: number;
  minimumRedemptionPoints: number;
  maxRedemptionPercentPerBill: number;
  pointExpiryDays: number;
  isActive: boolean;
}

interface CustomerLoyalty {
  customerId: number;
  totalPoints: number;
  redeemablePoints: number;
  pointsValue: number;
}

@Component({
  selector: 'app-loyalty-program',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, ButtonModule, CardModule, InputTextModule, InputNumberModule, ToggleButtonModule, TableModule],
  template: `
    <div class="p-4 max-w-3xl">
      <h2 class="text-xl font-semibold mb-4">Loyalty Programme</h2>

      @if(program(); as p) {
        <p-card header="Programme Settings">
          <div class="grid grid-cols-2 gap-4">
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">Programme Name</label>
              <input pInputText [(ngModel)]="edit.name" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">Points Per ₹1 Spent</label>
              <p-inputNumber [(ngModel)]="edit.pointsPerRupee" [minFractionDigits]="2" [min]="0" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">₹ Value Per Point</label>
              <p-inputNumber [(ngModel)]="edit.rupeeValuePerPoint" [minFractionDigits]="4" [min]="0" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">Min Redemption Points</label>
              <p-inputNumber [(ngModel)]="edit.minimumRedemptionPoints" [min]="0" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">Max Redemption % Per Bill</label>
              <p-inputNumber [(ngModel)]="edit.maxRedemptionPercentPerBill" [min]="0" [max]="100" suffix="%" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">Points Expiry (days)</label>
              <p-inputNumber [(ngModel)]="edit.pointExpiryDays" [min]="1" />
            </div>
            <div class="flex items-center gap-2 col-span-2">
              <p-toggleButton [(ngModel)]="edit.isActive" onLabel="Active" offLabel="Inactive" onIcon="pi pi-check" offIcon="pi pi-times" />
            </div>
          </div>
          <div class="flex justify-end mt-4">
            <p-button label="Save Programme" icon="pi pi-save" (onClick)="saveProgram()" />
          </div>
        </p-card>

        <div class="mt-6 bg-blue-50 rounded-lg p-4 flex gap-8">
          <div>
            <div class="text-2xl font-bold text-blue-700">{{ p.pointsPerRupee }}</div>
            <div class="text-sm text-gray-500">Points per ₹1</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-green-700">₹{{ p.rupeeValuePerPoint }}</div>
            <div class="text-sm text-gray-500">Value per point</div>
          </div>
          <div>
            <div class="text-2xl font-bold text-gray-700">{{ p.pointExpiryDays }}d</div>
            <div class="text-sm text-gray-500">Points expiry</div>
          </div>
        </div>
      } @else {
        <p-card>
          <div class="text-center py-8">
            <p class="text-gray-500 mb-4">No loyalty programme configured yet.</p>
            <p-button label="Create Programme" icon="pi pi-plus" (onClick)="createDefault()" />
          </div>
        </p-card>
      }
    </div>
  `,
})
export class LoyaltyProgramComponent {
  private http = inject(HttpClient);

  program = signal<LoyaltyProgram | null>(null);
  edit: Partial<LoyaltyProgram> = {};

  constructor() { this.loadProgram(); }

  loadProgram() {
    this.http.get<LoyaltyProgram>(ApiEndpoints.loyalty.program).pipe(catchError(() => of(null)))
      .subscribe(data => {
        this.program.set(data);
        if (data) this.edit = { ...data };
      });
  }

  createDefault() {
    this.edit = { name: 'Loyalty Club', pointsPerRupee: 1, rupeeValuePerPoint: 0.25,
      minimumRedemptionPoints: 100, maxRedemptionPercentPerBill: 20, pointExpiryDays: 365, isActive: true };
    this.saveProgram();
  }

  saveProgram() {
    this.http.post<{ value: number }>(ApiEndpoints.loyalty.program, this.edit)
      .subscribe(() => this.loadProgram());
  }
}
