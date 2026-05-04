import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OfflineService } from '../../../core/sync/offline.service';

@Component({
  selector: 'app-offline-banner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    @if (offline.isOffline()) {
      <div class="offline-banner">
        <i class="pi pi-wifi-off"></i>
        <span>You are offline — changes will sync when connection is restored.</span>
      </div>
    }
  `,
  styles: [`
    .offline-banner {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      z-index: 9999;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      background: #b45309;
      color: #fff;
      font-size: 0.875rem;
      font-weight: 500;
      justify-content: center;
    }
  `],
})
export class OfflineBannerComponent {
  protected readonly offline = inject(OfflineService);
}
