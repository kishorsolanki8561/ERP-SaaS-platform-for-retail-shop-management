import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiEndpoints } from '../../shared/messages/app-api';

export interface BranchItem {
  id: number;
  name: string;
  city: string | null;
  phone: string | null;
  isActive: boolean;
  isHeadOffice: boolean;
}

const STORAGE_KEY = 'active-branch-id';

@Injectable({ providedIn: 'root' })
export class BranchStore {
  private readonly http = inject(HttpClient);

  readonly branches = signal<BranchItem[]>([]);
  readonly activeBranchId = signal<number | null>(
    Number(localStorage.getItem(STORAGE_KEY)) || null
  );

  readonly activeBranch = computed(() => {
    const id = this.activeBranchId();
    return this.branches().find(b => b.id === id) ?? this.branches()[0] ?? null;
  });

  load(): void {
    this.http
      .get<BranchItem[]>(ApiEndpoints.admin.branches)
      .subscribe(list => {
        this.branches.set(list.filter(b => b.isActive));
        // If stored selection is gone (deactivated), clear it
        const id = this.activeBranchId();
        if (id && !list.some(b => b.id === id && b.isActive)) {
          this.setActive(null);
        }
      });
  }

  setActive(id: number | null): void {
    this.activeBranchId.set(id);
    if (id === null) {
      localStorage.removeItem(STORAGE_KEY);
    } else {
      localStorage.setItem(STORAGE_KEY, String(id));
    }
  }
}
