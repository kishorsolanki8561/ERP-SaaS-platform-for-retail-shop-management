import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';

export type PrimeSeverity = 'primary' | 'secondary' | 'success' | 'info' | 'warn' | 'danger' | 'contrast';

export interface PageAction {
  label: string;
  icon?: string;
  severity?: PrimeSeverity;
  permission?: string;
  outlined?: boolean;
}

@Component({
  selector: 'app-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule],
  template: `
    <div class="flex items-start justify-between mb-6 gap-4">
      <div class="min-w-0">
        <h1 class="text-xl font-bold text-slate-900 dark:text-white leading-tight">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="text-sm text-slate-500 dark:text-slate-400 mt-0.5">{{ subtitle() }}</p>
        }
      </div>
      @if (actions().length) {
        <div class="flex items-center gap-2 shrink-0">
          @for (action of actions(); track action.label) {
            <p-button
              [label]="action.label"
              [icon]="action.icon ?? ''"
              [severity]="action.severity ?? 'primary'"
              [outlined]="action.outlined ?? false"
              size="small"
              (onClick)="actionClick.emit(action.label)" />
          }
        </div>
      }
    </div>
  `
})
export class PageHeaderComponent {
  readonly title      = input.required<string>();
  readonly subtitle   = input<string>();
  readonly actions    = input<PageAction[]>([]);
  readonly actionClick = output<string>();
}
