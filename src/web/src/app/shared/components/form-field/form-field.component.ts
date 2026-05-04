import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-form-field',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-1.5">
      @if (label()) {
        <label class="text-[13px] font-semibold text-slate-700 dark:text-slate-300 leading-none">
          {{ label() }}
          @if (required()) {
            <span class="text-red-500 ml-0.5 font-medium">*</span>
          }
        </label>
      }

      <ng-content />

      @if (error()) {
        <p class="flex items-center gap-1.5 text-xs text-red-500 font-medium mt-0.5">
          <i class="pi pi-exclamation-circle text-[11px] shrink-0"></i>
          {{ error() }}
        </p>
      } @else if (hint()) {
        <p class="text-xs text-slate-400 dark:text-slate-500 leading-relaxed">{{ hint() }}</p>
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
