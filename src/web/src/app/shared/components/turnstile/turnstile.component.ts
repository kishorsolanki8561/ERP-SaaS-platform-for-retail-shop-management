import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  input,
  output,
  viewChild,
} from '@angular/core';

declare global {
  interface Window {
    turnstile: {
      render(container: HTMLElement, options: TurnstileOptions): string;
      reset(widgetId: string): void;
      remove(widgetId: string): void;
    };
  }
}

interface TurnstileOptions {
  sitekey: string;
  theme?: 'light' | 'dark' | 'auto';
  callback: (token: string) => void;
}

@Component({
  selector: 'app-turnstile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div #container></div>`,
})
export class TurnstileComponent implements AfterViewInit, OnDestroy {
  siteKey = input.required<string>();
  theme   = input<'light' | 'dark' | 'auto'>('auto');
  resolved = output<string>();

  private containerRef = viewChild.required<ElementRef<HTMLDivElement>>('container');
  private widgetId: string | null = null;

  ngAfterViewInit(): void {
    this._loadScript().then(() => {
      this.widgetId = window.turnstile.render(this.containerRef().nativeElement, {
        sitekey:  this.siteKey(),
        theme:    this.theme(),
        callback: (token: string) => this.resolved.emit(token),
      });
    });
  }

  reset(): void {
    if (this.widgetId) window.turnstile.reset(this.widgetId);
  }

  ngOnDestroy(): void {
    if (this.widgetId) window.turnstile.remove(this.widgetId);
  }

  private _loadScript(): Promise<void> {
    const SCRIPT_ID = 'cf-turnstile-script';
    if (window.turnstile) return Promise.resolve();
    const existing = document.getElementById(SCRIPT_ID);
    if (existing) {
      return new Promise(resolve => existing.addEventListener('load', () => resolve(), { once: true }));
    }
    return new Promise(resolve => {
      const s = document.createElement('script');
      s.id = SCRIPT_ID;
      s.src = 'https://challenges.cloudflare.com/turnstile/v0/api.js';
      s.onload = () => resolve();
      document.head.appendChild(s);
    });
  }
}
