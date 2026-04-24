import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-form-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col gap-1">
      @if (label()) {
        <label class="text-sm font-medium text-surface-700">
          {{ label() }}
          @if (required()) { <span class="text-red-500 ml-0.5">*</span> }
        </label>
      }
      <ng-content />
      @if (error()) {
        <small class="text-red-500 text-xs">{{ error() }}</small>
      }
      @else if (hint()) {
        <small class="text-surface-500 text-xs">{{ hint() }}</small>
      }
    </div>
  `
})
export class FormFieldComponent {
  readonly label = input<string>();
  readonly required = input(false);
  readonly error = input<string>();
  readonly hint = input<string>();
}
