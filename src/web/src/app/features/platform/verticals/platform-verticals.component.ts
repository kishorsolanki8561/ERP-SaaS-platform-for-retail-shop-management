import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { TableModule } from 'primeng/table';
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

@Component({
  selector: 'app-platform-verticals',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ButtonModule, CardModule, TagModule, TableModule],
  template: `
    <div class="p-4">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-xl font-semibold">Vertical Packs</h2>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        @for(pack of packs(); track pack.code) {
          <p-card [header]="pack.name">
            <ng-template pTemplate="header">
              <div class="flex items-center justify-between px-4 pt-4">
                <span class="text-2xl"><i [class]="pack.iconClass || 'pi pi-box'"></i></span>
                <p-tag [value]="pack.isActive ? 'Active' : 'Inactive'" [severity]="pack.isActive ? 'success' : 'secondary'" />
              </div>
            </ng-template>
            <p class="text-sm text-gray-600 mb-3">{{ pack.description || 'No description' }}</p>
            <div class="text-xs text-gray-400 mb-2">
              <strong>Code:</strong> <code>{{ pack.code }}</code>
            </div>
            <div class="text-xs text-gray-400">
              <strong>Feature flags:</strong>
              @for(flag of pack.featureFlagsCsv.split(','); track flag) {
                <span class="inline-block bg-gray-100 rounded px-1 mr-1">{{ flag.trim() }}</span>
              }
            </div>
          </p-card>
        }
        @empty {
          <div class="col-span-3 text-center py-12 text-gray-400">
            <i class="pi pi-box text-4xl mb-2"></i>
            <p>No vertical packs configured.</p>
          </div>
        }
      </div>
    </div>
  `,
})
export class PlatformVerticalsComponent {
  private http = inject(HttpClient);
  packs = signal<VerticalPack[]>([]);

  constructor() {
    this.http.get<VerticalPack[]>(ApiEndpoints.verticals.packs).pipe(catchError(() => of([])))
      .subscribe(data => this.packs.set(data));
  }
}
