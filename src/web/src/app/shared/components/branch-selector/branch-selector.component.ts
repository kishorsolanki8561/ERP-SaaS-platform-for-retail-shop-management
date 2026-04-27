import {
  ChangeDetectionStrategy,
  Component,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DropdownModule } from 'primeng/dropdown';
import { FormsModule } from '@angular/forms';
import { BranchStore } from '../../../core/branch/branch.store';

@Component({
  selector: 'app-branch-selector',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, DropdownModule, FormsModule],
  template: `
    @if (!store.loaded()) {
      <div data-testid="branch-selector"
           class="w-32 h-8 rounded-lg bg-slate-200 dark:bg-slate-700 animate-pulse">
      </div>
    } @else if (store.branches().length > 1) {
      <p-dropdown
        data-testid="branch-selector"
        [options]="store.branches()"
        [ngModel]="store.activeBranchId()"
        (ngModelChange)="store.setActive($event)"
        optionLabel="name"
        optionValue="id"
        placeholder="Select branch"
        styleClass="branch-selector text-sm"
        appendTo="body"
      >
        <ng-template pTemplate="selectedItem">
          <div class="flex items-center gap-1.5 text-sm">
            <i class="pi pi-building text-xs text-slate-500"></i>
            <span class="text-slate-700 dark:text-slate-300 font-medium">
              {{ store.activeBranch()?.name ?? 'Select branch' }}
            </span>
          </div>
        </ng-template>
        <ng-template pTemplate="item" let-branch>
          <div class="flex flex-column">
            <span class="font-medium text-sm">{{ branch.name }}</span>
            @if (branch.city) {
              <span class="text-xs text-slate-500">{{ branch.city }}</span>
            }
          </div>
        </ng-template>
      </p-dropdown>
    } @else if (store.branches().length === 1) {
      <div data-testid="branch-selector"
           class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-100 dark:bg-slate-800 text-sm">
        <i class="pi pi-building text-xs text-slate-500"></i>
        <span class="text-slate-700 dark:text-slate-300 font-medium">
          {{ store.branches()[0].name }}
        </span>
      </div>
    } @else {
      <div data-testid="branch-selector"
           class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-100 dark:bg-slate-800 text-sm text-slate-500">
        <i class="pi pi-building text-xs"></i>
        <span>No branch</span>
      </div>
    }
  `,
})
export class BranchSelectorComponent {
  protected readonly store = inject(BranchStore);
}
