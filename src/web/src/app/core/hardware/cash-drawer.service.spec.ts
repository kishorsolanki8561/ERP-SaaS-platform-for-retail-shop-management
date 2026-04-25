import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { CashDrawerService } from './cash-drawer.service';
import { ApiEndpoints } from '../../shared/messages/app-api';

describe('CashDrawerService', () => {
  let service: CashDrawerService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(CashDrawerService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('popping starts as false', () => {
    expect(service.popping()).toBeFalse();
  });

  it('pop() sets popping to true while request is in-flight', () => {
    service.pop();
    expect(service.popping()).toBeTrue();
    http.expectOne(ApiEndpoints.hardware.cashDrawerPop).flush(null, { status: 204, statusText: 'No Content' });
    expect(service.popping()).toBeFalse();
  });

  it('pop() is a no-op when already popping', () => {
    service.pop();
    service.pop(); // second call should be ignored
    http.expectOne(ApiEndpoints.hardware.cashDrawerPop).flush(null, { status: 204, statusText: 'No Content' });
  });

  it('popping resets to false on error response', () => {
    service.pop();
    http.expectOne(ApiEndpoints.hardware.cashDrawerPop).flush(
      { error: 'Server error' }, { status: 500, statusText: 'Error' }
    );
    expect(service.popping()).toBeFalse();
  });
});
