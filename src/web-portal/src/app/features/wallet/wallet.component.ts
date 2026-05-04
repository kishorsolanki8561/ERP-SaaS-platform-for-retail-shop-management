import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-wallet',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-4">
      <h1 class="text-xl font-bold text-gray-900">Wallet</h1>

      <div class="bg-white rounded-xl border border-gray-200 flex flex-col items-center justify-center py-24 text-center">
        <div class="w-16 h-16 rounded-2xl bg-purple-100 flex items-center justify-center mb-4">
          <i class="pi pi-wallet text-2xl text-purple-600"></i>
        </div>
        <h2 class="text-lg font-semibold text-gray-800">Your Wallet</h2>
        <p class="text-sm text-gray-400 mt-2 max-w-xs">
          View your wallet balance, top-up history, and use your credits for purchases. Coming soon.
        </p>
        <span class="mt-4 text-xs bg-gray-100 text-gray-500 px-3 py-1.5 rounded-full font-medium">Coming Soon</span>
      </div>
    </div>
  `
})
export class WalletComponent {}
