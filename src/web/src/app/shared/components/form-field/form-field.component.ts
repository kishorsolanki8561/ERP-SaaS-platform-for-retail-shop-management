import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-form-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-1.5">
      @if (label()) {
        <label class="text-sm font-medium text-slate-700 dark:text-slate-300">
          {{ label() }}
          @if (required()) { <span class="text-red-500 ml-0.5">*</span> }
        </label>
      }
      <ng-content />
      @if (error()) {
        <p class="text-xs text-red-500 flex items-center gap-1">
          <i class="pi pi-exclamation-circle text-[10px]"></i>
          {{ error() }}
        </p>
      } @else if (hint()) {
        <p class="text-xs text-slate-400 dark:text-slate-500">{{ hint() }}</p>
      }
    </div>
  `
})
export class FormFieldComponent {
  readonly label    = input<string>();
  readonly required = input(false);
  readonly error    = input<string>();
  readonly hint     = input<string>();
}
