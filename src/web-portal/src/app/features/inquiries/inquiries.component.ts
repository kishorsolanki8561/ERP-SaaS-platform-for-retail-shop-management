import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { SlicePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PortalApiService, InquirySummary, LinkedShop } from '../../core/services/portal-api.service';

@Component({
  selector: 'app-inquiries',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, SlicePipe],
  template: `
    <div class="space-y-4">

      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-gray-900">Inquiries</h1>
        <button
          (click)="showForm.set(true)"
          class="flex items-center gap-1.5 bg-purple-600 hover:bg-purple-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors">
          <i class="pi pi-plus text-xs"></i> New Inquiry
        </button>
      </div>

      <!-- New inquiry form -->
      @if (showForm()) {
        <div class="bg-white rounded-xl border border-purple-200 p-5 space-y-4">
          <h2 class="font-semibold text-gray-800">New Inquiry</h2>

          <div>
            <label class="text-xs font-medium text-gray-600 mb-1 block">Shop</label>
            <select [(ngModel)]="form.shopId"
              class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-purple-300">
              <option value="0">Select a shop...</option>
              @for (s of shops(); track s.shopId) {
                <option [value]="s.shopId">{{ s.shopName }}</option>
              }
            </select>
          </div>

          <div>
            <label class="text-xs font-medium text-gray-600 mb-1 block">Type</label>
            <select [(ngModel)]="form.type"
              class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-purple-300">
              <option value="ProductAvailability">Product Availability</option>
              <option value="PriceQuery">Price Query</option>
              <option value="Complaint">Complaint</option>
              <option value="FeatureRequest">Feature Request</option>
            </select>
          </div>

          <div>
            <label class="text-xs font-medium text-gray-600 mb-1 block">Subject</label>
            <input [(ngModel)]="form.subject" type="text" placeholder="Brief subject..."
              class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-purple-300" />
          </div>

          <div>
            <label class="text-xs font-medium text-gray-600 mb-1 block">Message</label>
            <textarea [(ngModel)]="form.body" rows="4" placeholder="Describe your inquiry..."
              class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-purple-300 resize-none"></textarea>
          </div>

          @if (formError()) {
            <p class="text-xs text-red-500">{{ formError() }}</p>
          }

          <div class="flex gap-3 pt-1">
            <button (click)="submitInquiry()"
              [disabled]="submitting()"
              class="bg-purple-600 hover:bg-purple-700 text-white text-sm font-medium px-5 py-2 rounded-lg transition-colors disabled:opacity-60">
              {{ submitting() ? 'Submitting...' : 'Submit' }}
            </button>
            <button (click)="cancelForm()"
              class="text-sm text-gray-500 hover:text-gray-700 px-4 py-2 rounded-lg border border-gray-200 hover:bg-gray-50 transition-colors">
              Cancel
            </button>
          </div>
        </div>
      }

      <!-- List -->
      <div class="bg-white rounded-xl border border-gray-200 overflow-hidden">
        @if (loading()) {
          <div class="divide-y divide-gray-100">
            @for (_ of [1,2,3]; track $index) {
              <div class="px-5 py-4 animate-pulse">
                <div class="h-4 bg-gray-100 rounded w-1/2 mb-2"></div>
                <div class="h-3 bg-gray-100 rounded w-1/4"></div>
              </div>
            }
          </div>
        } @else if (inquiries().length === 0) {
          <div class="flex flex-col items-center justify-center py-20 text-center">
            <i class="pi pi-comments text-4xl text-gray-200 mb-4"></i>
            <p class="text-gray-500 font-medium">No inquiries</p>
            <p class="text-sm text-gray-400 mt-1">Submit an inquiry to a shop for support or information.</p>
          </div>
        } @else {
          <div class="divide-y divide-gray-100">
            @for (inq of inquiries(); track inq.id) {
              <div class="px-5 py-4">
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0">
                    <p class="text-sm font-semibold text-gray-800 truncate">{{ inq.subject }}</p>
                    <p class="text-xs text-gray-400 mt-0.5">{{ inq.inquiryNumber }} · {{ inq.type }} · {{ inq.openedAtUtc | slice:0:10 }}</p>
                  </div>
                  <span class="shrink-0 text-xs px-2 py-0.5 rounded-full font-medium"
                    [class]="statusClass(inq.status)">
                    {{ inq.status }}
                  </span>
                </div>
              </div>
            }
          </div>
        }
      </div>
    </div>
  `
})
export class InquiriesComponent implements OnInit {
  private readonly api = inject(PortalApiService);

  readonly inquiries = signal<InquirySummary[]>([]);
  readonly shops = signal<LinkedShop[]>([]);
  readonly loading = signal(true);
  readonly showForm = signal(false);
  readonly submitting = signal(false);
  readonly formError = signal('');

  form = { shopId: 0, subject: '', body: '', type: 'ProductAvailability' };

  async ngOnInit() {
    const [inqRes, shopRes] = await Promise.all([
      this.api.listInquiries(1, 50),
      this.api.listShops(1, 50),
    ]);
    this.inquiries.set(inqRes.items ?? []);
    this.shops.set(shopRes.items ?? []);
    this.loading.set(false);
  }

  cancelForm() {
    this.showForm.set(false);
    this.formError.set('');
    this.form = { shopId: 0, subject: '', body: '', type: 'ProductAvailability' };
  }

  async submitInquiry() {
    if (!this.form.shopId || !this.form.subject.trim() || !this.form.body.trim()) {
      this.formError.set('Please fill in all fields and select a shop.');
      return;
    }
    this.submitting.set(true);
    this.formError.set('');
    try {
      const r = await this.api.createInquiry(this.form.shopId, this.form.subject, this.form.body, this.form.type);
      if (r.isSuccess) {
        this.cancelForm();
        const updated = await this.api.listInquiries(1, 50);
        this.inquiries.set(updated.items ?? []);
      } else {
        this.formError.set(r.errors?.[0] ?? 'Failed to submit inquiry.');
      }
    } finally {
      this.submitting.set(false);
    }
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Open:       'bg-blue-100 text-blue-700',
      InProgress: 'bg-yellow-100 text-yellow-700',
      Resolved:   'bg-green-100 text-green-700',
      Closed:     'bg-gray-100 text-gray-500',
    };
    return map[status] ?? 'bg-gray-100 text-gray-500';
  }
}
