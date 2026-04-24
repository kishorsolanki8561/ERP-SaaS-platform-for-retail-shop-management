import { Injectable, Signal, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../../shared/messages/app-api';

export interface DdlItem {
  code: string;
  label: string;
  sortOrder: number;
  parentCode?: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class DdlKeyStore {
  private readonly http = inject(HttpClient);
  private readonly _cache = new Map<string, ReturnType<typeof signal<DdlItem[]>>>();

  getItems(key: string): Signal<DdlItem[]> {
    if (!this._cache.has(key)) {
      const sig = signal<DdlItem[]>([]);
      this._cache.set(key, sig);
      this.load(key, sig);
    }
    return this._cache.get(key)!.asReadonly();
  }

  preload(key: string): void { this.getItems(key); }

  private async load(key: string, sig: ReturnType<typeof signal<DdlItem[]>>): Promise<void> {
    try {
      const items = await firstValueFrom(
        this.http.get<DdlItem[]>(ApiEndpoints.ddl.single(key))
      );
      sig.set(items);
    } catch { /* silently fail — empty list is safe */ }
  }
}
