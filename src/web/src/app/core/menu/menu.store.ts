import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface MenuItemDto {
  code: string;
  label: string;
  kind: 'Group' | 'Submenu' | 'Page';
  icon?: string;
  route?: string;
  sortOrder: number;
  children: MenuItemDto[];
}

@Injectable({ providedIn: 'root' })
export class MenuStore {
  private readonly http = inject(HttpClient);

  readonly tree = signal<MenuItemDto[]>([]);
  readonly loading = signal(false);

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const items = await firstValueFrom(this.http.get<MenuItemDto[]>('/api/menu/tree'));
      this.tree.set(items);
    } catch { /* cleared on logout */ } finally {
      this.loading.set(false);
    }
  }

  clear(): void {
    this.tree.set([]);
  }
}
