import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ApiEndpoints } from '../../../shared/messages/app-api';
import { catchError, of } from 'rxjs';

interface VerticalPack {
  id: number;
  code: string;
  name: string;
  description: string;
  featureFlagsCsv: string;
  iconClass: string;
  sortOrder: number;
  isActive: boolean;
}

interface ShopVertical {
  id: number;
  verticalPackId: number;
  verticalPackCode: string;
  verticalPackName: string;
  appliedAtUtc: string;
}

@Component({
  selector: 'app-admin-vertical',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ButtonModule, CardModule, TagModule],
  template: `
    <div class="p-4 max-w-4xl">
      <h2 class="text-xl font-semibold mb-2">Vertical Pack</h2>
      <p class="text-gray-500 text-sm mb-6">Choose the industry vertical for your shop. This enables vertical-specific features and workflows.</p>

      @if(current(); as sv) {
        <div class="bg-green-50 border border-green-200 rounded-lg p-4 mb-6 flex items-center gap-4">
          <i class="pi pi-check-circle text-green-600 text-2xl"></i>
          <div>
            <div class="font-semibold text-green-800">Currently: {{ sv.verticalPackName }}</div>
            <div class="text-sm text-gray-500">Applied {{ sv.appliedAtUtc | date:'dd MMM yyyy' }}</div>
          </div>
        </div>
      }

      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        @for(pack of packs(); track pack.code) {
          <p-card>
            <div class="flex flex-col h-full">
              <div class="flex items-center gap-3 mb-3">
                <span class="text-3xl"><i [class]="pack.iconClass || 'pi pi-box'"></i></span>
                <div>
                  <div class="font-semibold">{{ pack.name }}</div>
                  <code class="text-xs text-gray-400">{{ pack.code }}</code>
                </div>
              </div>
              <p class="text-sm text-gray-600 flex-1 mb-4">{{ pack.description || '' }}</p>
              <div class="text-xs text-gray-400 mb-4">
                @for(flag of pack.featureFlagsCsv.split(','); track flag) {
                  <span class="inline-block bg-blue-50 text-blue-700 rounded px-1 mr-1 mb-1">{{ flag.trim() }}</span>
                }
              </div>
              <p-button
                [label]="isSelected(pack.code) ? 'Installed' : 'Install'"
                [icon]="isSelected(pack.code) ? 'pi pi-check' : 'pi pi-download'"
                [severity]="isSelected(pack.code) ? 'success' : 'primary'"
                [outlined]="isSelected(pack.code)"
                [disabled]="!pack.isActive"
                class="w-full"
                (onClick)="install(pack.code)"
              />
            </div>
          </p-card>
        }
      </div>
    </div>
  `,
})
export class AdminVerticalComponent {
  private http = inject(HttpClient);

  packs = signal<VerticalPack[]>([]);
  current = signal<ShopVertical | null>(null);

  constructor() {
    this.http.get<VerticalPack[]>(ApiEndpoints.verticals.packs).pipe(catchError(() => of([])))
      .subscribe(data => this.packs.set(data));
    this.http.get<ShopVertical>(ApiEndpoints.verticals.shopVertical).pipe(catchError(() => of(null)))
      .subscribe(data => this.current.set(data));
  }

  isSelected(code: string) { return this.current()?.verticalPackCode === code; }

  install(code: string) {
    this.http.post<{ value: number }>(ApiEndpoints.verticals.install, { packCode: code }).subscribe(() => {
      this.http.get<ShopVertical>(ApiEndpoints.verticals.shopVertical).pipe(catchError(() => of(null)))
        .subscribe(data => this.current.set(data));
    });
  }
}
