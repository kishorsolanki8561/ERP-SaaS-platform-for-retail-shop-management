import {
  ChangeDetectionStrategy, Component, OnInit,
  forwardRef, inject, input
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { FormsModule } from '@angular/forms';
import { DdlKeyStore } from '../../../core/ddl/ddl.store';

@Component({
  selector: 'app-ddl-dropdown',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DropdownModule, FormsModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => DdlDropdownComponent), multi: true }
  ],
  template: `
    <p-dropdown
      [options]="store.getItems(dkey())()"
      [ngModel]="value"
      (ngModelChange)="onChange($event)"
      (blur)="onTouched()"
      optionLabel="label"
      optionValue="code"
      [placeholder]="placeholder()"
      [disabled]="isDisabled"
      styleClass="w-full"
    />
  `
})
export class DdlDropdownComponent implements ControlValueAccessor, OnInit {
  readonly dkey = input.required<string>();
  readonly placeholder = input('Select...');

  protected readonly store = inject(DdlKeyStore);

  protected value: string | null = null;
  protected isDisabled = false;
  protected onChange: (v: string | null) => void = () => {};
  protected onTouched: () => void = () => {};

  ngOnInit(): void {
    this.store.preload(this.dkey());
  }

  writeValue(v: string | null): void { this.value = v; }
  registerOnChange(fn: (v: string | null) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }
  setDisabledState(disabled: boolean): void { this.isDisabled = disabled; }
}
