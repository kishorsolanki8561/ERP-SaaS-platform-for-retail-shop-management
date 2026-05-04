import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

// ── Response shapes ───────────────────────────────────────────────────────────

export interface ApiResult<T> {
  isSuccess: boolean;
  statusCode: number;
  errors: string[];
  value: T;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CustomerProfile {
  id: number;
  displayName: string;
  email: string | null;
  phone: string | null;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
}

export interface PurchaseHistory {
  invoiceId: number;
  invoiceNumber: string;
  shopId: number;
  shopName: string;
  invoiceDate: string;
  grandTotal: number;
  status: string;
}

export interface PurchaseLine {
  productName: string;
  unitCode: string;
  qty: number;
  unitPrice: number;
  lineTotal: number;
}

export interface PurchaseDetail {
  invoiceId: number;
  invoiceNumber: string;
  shopName: string;
  invoiceDate: string;
  subTotal: number;
  grandTotal: number;
  lines: PurchaseLine[];
}

export interface LinkedShop {
  shopId: number;
  shopName: string;
  hasWallet: boolean;
  hasOnlineOrders: boolean;
  totalSpend: number;
  linkedAtUtc: string;
}

export interface SpendByShop {
  shopId: number;
  shopName: string;
  spend: number;
  invoices: number;
}

export interface CustomerInsights {
  totalSpend: number;
  totalInvoices: number;
  byShop: SpendByShop[];
}

export interface InquirySummary {
  id: number;
  inquiryNumber: string;
  subject: string;
  status: string;
  type: string;
  openedAtUtc: string;
}

export interface InquiryMessage {
  id: number;
  isFromCustomer: boolean;
  body: string;
  sentAtUtc: string;
}

export interface InquiryDetail extends InquirySummary {
  body: string;
  closedAtUtc: string | null;
  messages: InquiryMessage[];
}

export interface OnlineOrderSummary {
  id: number;
  orderNumber: string;
  status: string;
  grandTotal: number;
  createdAtUtc: string;
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class PortalApiService {
  private readonly http = inject(HttpClient);

  // ── Profile ─────────────────────────────────────────────────────────────────

  getMe(): Promise<ApiResult<CustomerProfile>> {
    return firstValueFrom(this.http.get<ApiResult<CustomerProfile>>('/api/portal/me'));
  }

  updateMe(displayName: string, email: string | null): Promise<ApiResult<boolean>> {
    return firstValueFrom(this.http.patch<ApiResult<boolean>>('/api/portal/me', { displayName, email }));
  }

  // ── Purchases ────────────────────────────────────────────────────────────────

  listPurchases(page = 1, pageSize = 20): Promise<PagedResult<PurchaseHistory>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(this.http.get<PagedResult<PurchaseHistory>>('/api/portal/me/purchases', { params }));
  }

  getPurchase(invoiceId: number): Promise<ApiResult<PurchaseDetail | null>> {
    return firstValueFrom(this.http.get<ApiResult<PurchaseDetail | null>>(`/api/portal/me/purchases/${invoiceId}`));
  }

  // ── Shops ────────────────────────────────────────────────────────────────────

  listShops(page = 1, pageSize = 20): Promise<PagedResult<LinkedShop>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(this.http.get<PagedResult<LinkedShop>>('/api/portal/shops', { params }));
  }

  // ── Insights ─────────────────────────────────────────────────────────────────

  getInsights(from?: string, to?: string): Promise<ApiResult<CustomerInsights>> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return firstValueFrom(this.http.get<ApiResult<CustomerInsights>>('/api/portal/me/insights', { params }));
  }

  // ── Inquiries ────────────────────────────────────────────────────────────────

  listInquiries(page = 1, pageSize = 20): Promise<PagedResult<InquirySummary>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(this.http.get<PagedResult<InquirySummary>>('/api/portal/me/inquiries', { params }));
  }

  createInquiry(shopId: number, subject: string, body: string, type: string): Promise<ApiResult<number>> {
    return firstValueFrom(this.http.post<ApiResult<number>>(
      `/api/portal/shops/${shopId}/inquiries`,
      { subject, body, type }
    ));
  }

  // ── Online Orders ────────────────────────────────────────────────────────────

  listOrders(page = 1, pageSize = 20): Promise<PagedResult<OnlineOrderSummary>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(this.http.get<PagedResult<OnlineOrderSummary>>('/api/portal/me/orders', { params }));
  }
}
