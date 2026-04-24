import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { DdlDropdownComponent } from '../../../shared/components/ddl-dropdown/ddl-dropdown.component';

interface ShopProfile {
  shopCode: string;
  legalName: string;
  tradeName?: string;
  gstNumber?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  stateCode?: string;
  pinCode?: string;
  currencyCode: string;
  timeZone: string;
}

@Component({
  selector: 'app-shop-profile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule, CardModule, InputTextModule, ButtonModule,
    PageHeaderComponent, FormFieldComponent, DdlDropdownComponent
  ],
  template: `
    <app-page-header title="Shop Profile" subtitle="Update your shop details and settings." />

    <p-card>
      @if (profile()) {
        <form (ngSubmit)="save()" class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <app-form-field label="Shop Code">
            <input pInputText [ngModel]="profile()!.shopCode" name="shopCode"
                   class="w-full" [disabled]="true" />
          </app-form-field>

          <app-form-field label="Legal Name" [required]="true">
            <input pInputText [(ngModel)]="profile()!.legalName" name="legalName" class="w-full" />
          </app-form-field>

          <app-form-field label="Trade Name">
            <input pInputText [(ngModel)]="profile()!.tradeName" name="tradeName" class="w-full" />
          </app-form-field>

          <app-form-field label="GST Number">
            <input pInputText [(ngModel)]="profile()!.gstNumber" name="gstNumber"
                   class="w-full" maxlength="15" />
          </app-form-field>

          <app-form-field label="Address Line 1">
            <input pInputText [(ngModel)]="profile()!.addressLine1" name="addressLine1" class="w-full" />
          </app-form-field>

          <app-form-field label="Address Line 2">
            <input pInputText [(ngModel)]="profile()!.addressLine2" name="addressLine2" class="w-full" />
          </app-form-field>

          <app-form-field label="City">
            <input pInputText [(ngModel)]="profile()!.city" name="city" class="w-full" />
          </app-form-field>

          <app-form-field label="State">
            <app-ddl-dropdown dkey="INDIAN_STATE" [(ngModel)]="profile()!.stateCode" name="stateCode" />
          </app-form-field>

          <app-form-field label="PIN Code">
            <input pInputText [(ngModel)]="profile()!.pinCode" name="pinCode"
                   class="w-full" maxlength="6" />
          </app-form-field>

          <app-form-field label="Currency">
            <app-ddl-dropdown dkey="CURRENCY" [(ngModel)]="profile()!.currencyCode" name="currencyCode" />
          </app-form-field>

          <div class="md:col-span-2 flex justify-end gap-2 mt-2">
            <p-button type="submit" label="Save Changes" [loading]="saving()" />
          </div>
        </form>
      } @else {
        <div class="flex justify-center p-8">
          <i class="pi pi-spin pi-spinner text-2xl text-primary-500"></i>
        </div>
      }
    </p-card>
  `
})
export class ShopProfileComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected profile = signal<ShopProfile | null>(null);
  protected saving = signal(false);

  async ngOnInit(): Promise<void> {
    try {
      const data = await firstValueFrom(this.http.get<ShopProfile>('/api/admin/shop-profile'));
      this.profile.set(data);
    } catch { /* errorInterceptor shows toast */ }
  }

  protected async save(): Promise<void> {
    if (!this.profile()) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.put('/api/admin/shop-profile', this.profile()));
    } catch { /* handled by errorInterceptor */ }
    finally { this.saving.set(false); }
  }
}
