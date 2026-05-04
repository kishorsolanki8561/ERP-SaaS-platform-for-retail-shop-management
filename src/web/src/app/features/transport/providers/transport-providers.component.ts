import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, computed
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { PageHeaderComponent, PageAction } from '../../../shared/components/page-header/page-header.component';
import { FormFieldComponent } from '../../../shared/components/form-field/form-field.component';
import { AppLabels } from '../../../shared/messages/app-messages';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { Permissions } from '../../../shared/messages/app-permissions';

interface ProviderRow {
  id: number;
  providerName: string;
  contactPerson?: string;
  phone?: string;
  gstNumber?: string;
  isActive: boolean;
}

interface VehicleRow {
  id: number;
  vehicleNumber: string;
  vehicleType: string;
  providerId?: number;
  providerName?: string;
  isActive: boolean;
}

@Component({
  selector: 'app-transport-providers',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule,
    TableModule, ButtonModule, DialogModule, InputTextModule, TooltipModule,
    PageHeaderComponent, FormFieldComponent,
  ],
  template: `
    <div class="p-6 space-y-6 max-w-7xl mx-auto">
      <app-page-header
        [title]="labels.transport.providersTitle"
        [subtitle]="labels.transport.providersSubtitle"
        [actions]="headerActions"
        (actionClick)="onHeaderAction($event)"
      />

      <!-- Providers table -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800">
          <h3 class="text-sm font-semibold text-slate-700 dark:text-slate-300">Providers</h3>
        </div>
        <p-table [value]="providers()" [loading]="loading()" styleClass="p-datatable-sm"
                 [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th>Provider Name</th>
              <th style="width: 160px">Contact</th>
              <th style="width: 140px">Phone</th>
              <th style="width: 150px">GST Number</th>
              <th style="width: 80px">Active</th>
              <th style="width: 80px" class="text-right">Actions</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-semibold text-slate-800 dark:text-slate-200">{{ row.providerName }}</td>
              <td class="text-slate-600">{{ row.contactPerson ?? '—' }}</td>
              <td class="text-slate-600">{{ row.phone ?? '—' }}</td>
              <td class="font-mono text-sm text-slate-600">{{ row.gstNumber ?? '—' }}</td>
              <td>
                @if (row.isActive) {
                  <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-emerald-100">
                    <i class="pi pi-check text-emerald-600" style="font-size: 0.5625rem"></i>
                  </span>
                } @else {
                  <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-slate-100">
                    <i class="pi pi-times text-slate-400" style="font-size: 0.5625rem"></i>
                  </span>
                }
              </td>
              <td class="text-right">
                <button pButton [icon]="row.isActive ? 'pi pi-ban' : 'pi pi-check'"
                        class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        [pTooltip]="row.isActive ? 'Deactivate' : 'Activate'" tooltipPosition="left"
                        (click)="toggleProvider(row)" [disabled]="actionId() === row.id"></button>
              </td>
            </tr>
          </ng-template>
          <ng-template pTemplate="emptymessage">
            <tr><td colspan="6">
              <div class="flex flex-col items-center justify-center py-16 gap-3 text-center px-4">
                <i class="pi pi-truck text-3xl text-slate-300 dark:text-slate-600"></i>
                <p class="text-sm text-slate-500">No transport providers. Add your first provider.</p>
              </div>
            </td></tr>
          </ng-template>
        </p-table>
      </div>

      <!-- Vehicles table -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden shadow-sm">
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800">
          <h3 class="text-sm font-semibold text-slate-700 dark:text-slate-300">Vehicles</h3>
          <p-button icon="pi pi-plus" size="small" [label]="labels.transport.newVehicle" severity="secondary"
                    (onClick)="openVehicleDialog()" />
        </div>
        <p-table [value]="vehicles()" [loading]="loading()" styleClass="p-datatable-sm"
                 [tableStyle]="{ 'min-width': '100%' }">
          <ng-template pTemplate="header">
            <tr>
              <th style="width: 160px">Vehicle Number</th>
              <th>Type</th>
              <th>Provider</th>
              <th style="width: 80px">Active</th>
              <th style="width: 80px" class="text-right">Actions</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-row>
            <tr>
              <td class="font-mono text-sm font-semibold text-slate-700 dark:text-slate-300">{{ row.vehicleNumber }}</td>
              <td class="text-slate-700">{{ row.vehicleType }}</td>
              <td class="text-slate-600">{{ row.providerName ?? '—' }}</td>
              <td>
                @if (row.isActive) {
                  <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-emerald-100">
                    <i class="pi pi-check text-emerald-600" style="font-size: 0.5625rem"></i>
                  </span>
                } @else {
                  <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-slate-100">
                    <i class="pi pi-times text-slate-400" style="font-size: 0.5625rem"></i>
                  </span>
                }
              </td>
              <td class="text-right">
                <button pButton [icon]="row.isActive ? 'pi pi-ban' : 'pi pi-check'"
                        class="p-button-sm p-button-text p-button-rounded p-button-secondary"
                        [pTooltip]="row.isActive ? 'Deactivate' : 'Activate'" tooltipPosition="left"
                        (click)="toggleVehicle(row)" [disabled]="vehicleActionId() === row.id"></button>
              </td>
            </tr>
          </ng-template>
          <ng-template pTemplate="emptymessage">
            <tr><td colspan="5">
              <div class="flex flex-col items-center justify-center py-16 gap-3 text-center px-4">
                <i class="pi pi-car text-3xl text-slate-300 dark:text-slate-600"></i>
                <p class="text-sm text-slate-500">No vehicles registered.</p>
              </div>
            </td></tr>
          </ng-template>
        </p-table>
      </div>
    </div>

    <!-- New Provider dialog -->
    <p-dialog [(visible)]="providerDialogVisible" [header]="labels.transport.newProvider"
              [modal]="true" [style]="{ width: '480px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field label="Provider Name" [required]="true">
          <input pInputText [(ngModel)]="providerForm.providerName" name="providerName"
                 class="w-full" placeholder="e.g. Delhivery" />
        </app-form-field>
        <div class="grid grid-cols-2 gap-4">
          <app-form-field label="Contact Person">
            <input pInputText [(ngModel)]="providerForm.contactPerson" name="contactPerson" class="w-full" />
          </app-form-field>
          <app-form-field label="Phone">
            <input pInputText [(ngModel)]="providerForm.phone" name="phone" class="w-full" />
          </app-form-field>
        </div>
        <app-form-field label="GST Number">
          <input pInputText [(ngModel)]="providerForm.gstNumber" name="gstNumber" class="w-full" />
        </app-form-field>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="providerDialogVisible = false" />
        <p-button [label]="labels.transport.newProvider" [loading]="saving()"
                  [disabled]="!providerForm.providerName"
                  (onClick)="saveProvider()" />
      </ng-template>
    </p-dialog>

    <!-- New Vehicle dialog -->
    <p-dialog [(visible)]="vehicleDialogVisible" [header]="labels.transport.newVehicle"
              [modal]="true" [style]="{ width: '420px' }" [draggable]="false">
      <form class="space-y-4 pt-2">
        <app-form-field [label]="labels.transport.vehicleNumber" [required]="true">
          <input pInputText [(ngModel)]="vehicleForm.vehicleNumber" name="vehicleNumber"
                 class="w-full" placeholder="e.g. MH12AB1234" />
        </app-form-field>
        <app-form-field label="Vehicle Type" [required]="true">
          <input pInputText [(ngModel)]="vehicleForm.vehicleType" name="vehicleType"
                 class="w-full" placeholder="e.g. Truck, Van, Bike" />
        </app-form-field>
      </form>
      <ng-template pTemplate="footer">
        <p-button [label]="labels.shared.cancel" severity="secondary" [outlined]="true"
                  (onClick)="vehicleDialogVisible = false" />
        <p-button [label]="labels.transport.newVehicle" [loading]="saving()"
                  [disabled]="!vehicleForm.vehicleNumber || !vehicleForm.vehicleType"
                  (onClick)="saveVehicle()" />
      </ng-template>
    </p-dialog>
  `
})
export class TransportProvidersComponent implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly labels          = AppLabels;
  protected readonly loading         = signal(false);
  protected readonly saving          = signal(false);
  protected readonly actionId        = signal<number | null>(null);
  protected readonly vehicleActionId = signal<number | null>(null);
  protected readonly providers       = signal<ProviderRow[]>([]);
  protected readonly vehicles        = signal<VehicleRow[]>([]);

  protected providerDialogVisible = false;
  protected vehicleDialogVisible  = false;
  protected providerForm = this.emptyProviderForm();
  protected vehicleForm  = this.emptyVehicleForm();

  protected readonly headerActions: PageAction[] = [
    { label: AppLabels.transport.newProvider, icon: 'pi pi-plus', severity: 'primary', permission: Permissions.transport.manage },
  ];

  ngOnInit(): void { this.load(); }

  protected onHeaderAction(action: string): void {
    if (action === AppLabels.transport.newProvider) {
      this.providerForm = this.emptyProviderForm();
      this.providerDialogVisible = true;
    }
  }

  protected openVehicleDialog(): void {
    this.vehicleForm = this.emptyVehicleForm();
    this.vehicleDialogVisible = true;
  }

  protected async toggleProvider(row: ProviderRow): Promise<void> {
    this.actionId.set(row.id);
    try {
      await firstValueFrom(
        this.http.patch(`${ApiEndpoints.transport.toggleProvider(row.id)}?isActive=${!row.isActive}`, {})
      );
      await this.load();
    } catch { /* handled */ }
    finally { this.actionId.set(null); }
  }

  protected async toggleVehicle(row: VehicleRow): Promise<void> {
    this.vehicleActionId.set(row.id);
    try {
      await firstValueFrom(
        this.http.patch(`${ApiEndpoints.transport.toggleVehicle(row.id)}?isActive=${!row.isActive}`, {})
      );
      await this.load();
    } catch { /* handled */ }
    finally { this.vehicleActionId.set(null); }
  }

  protected async saveProvider(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.transport.providers, {
        providerName:  this.providerForm.providerName,
        contactPerson: this.providerForm.contactPerson || null,
        phone:         this.providerForm.phone || null,
        gstNumber:     this.providerForm.gstNumber || null,
      }));
      this.providerDialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  protected async saveVehicle(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.post(ApiEndpoints.transport.vehicles, {
        vehicleNumber: this.vehicleForm.vehicleNumber,
        vehicleType:   this.vehicleForm.vehicleType,
      }));
      this.vehicleDialogVisible = false;
      await this.load();
    } catch { /* handled */ }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const [provs, vehs] = await Promise.all([
        firstValueFrom(this.http.get<ProviderRow[]>(ApiEndpoints.transport.providers)),
        firstValueFrom(this.http.get<VehicleRow[]>(ApiEndpoints.transport.vehicles)),
      ]);
      this.providers.set(provs ?? []);
      this.vehicles.set(vehs ?? []);
    } catch { /* handled */ }
    finally { this.loading.set(false); }
  }

  private emptyProviderForm() {
    return { providerName: '', contactPerson: '', phone: '', gstNumber: '' };
  }

  private emptyVehicleForm() {
    return { vehicleNumber: '', vehicleType: '' };
  }
}
