import { Injectable, Signal, computed, effect, signal } from '@angular/core';

export type Theme = 'light' | 'dark' | 'system';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'portal-theme';
  private readonly _systemDark = signal(this.detectSystem());
  private readonly _theme = signal<Theme>(this.loadSaved());

  readonly theme: Signal<Theme> = this._theme.asReadonly();
  readonly isDark = computed(() => {
    const t = this._theme();
    if (t === 'dark') return true;
    if (t === 'light') return false;
    return this._systemDark();
  });

  constructor() {
    if (typeof window !== 'undefined') {
      window.matchMedia('(prefers-color-scheme: dark)')
        .addEventListener('change', e => this._systemDark.set(e.matches));
    }
    effect(() => {
      if (typeof document !== 'undefined') {
        document.documentElement.classList.toggle('app-dark', this.isDark());
      }
    });
  }

  set(theme: Theme): void {
    this._theme.set(theme);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(this.storageKey, theme);
    }
  }

  private detectSystem(): boolean {
    return typeof window !== 'undefined'
      ? window.matchMedia('(prefers-color-scheme: dark)').matches
      : false;
  }

  private loadSaved(): Theme {
    if (typeof localStorage === 'undefined') return 'system';
    return (localStorage.getItem(this.storageKey) as Theme) ?? 'system';
  }
}
