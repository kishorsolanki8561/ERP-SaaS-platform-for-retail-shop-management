import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';

export interface PageAction {
  label: string;
  icon?: string;
  severity?: 'primary' | 'secondary' | 'success' | 'danger' | 'warn' | 'info' | 'contrast';
  permission?: string;
}

@Component({
  selector: 'app-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ButtonModule],
  template: `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-semibold text-surface-800">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="text-sm text-surface-500 mt-1">{{ subtitle() }}</p>
        }
      </div>
      <div class="flex gap-2">
        @for (action of actions(); track action.label) {
          <p-button
            [label]="action.label"
            [icon]="action.icon ?? ''"
            [severity]="action.severity ?? 'primary'"
            (onClick)="actionClick.emit(action.label)"
          />
        }
      </div>
    </div>
  `
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly actions = input<PageAction[]>([]);
  readonly actionClick = output<string>();
}
