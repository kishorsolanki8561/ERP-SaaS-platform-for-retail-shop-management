import { TestBed } from '@angular/core/testing';
import { BarcodeListenerService } from './barcode-listener.service';

function fire(key: string, target: EventTarget = document): void {
  target.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
}

describe('BarcodeListenerService', () => {
  let service: BarcodeListenerService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BarcodeListenerService);
  });

  it('scanned starts as null', () => {
    expect(service.scanned()).toBeNull();
  });

  it('does not emit while disabled', () => {
    service.disable();
    '1234567'.split('').forEach(k => fire(k));
    fire('Enter');
    expect(service.scanned()).toBeNull();
  });

  it('emits barcode after rapid key sequence + Enter', () => {
    service.enable();
    '12345'.split('').forEach(k => fire(k));
    fire('Enter');
    expect(service.scanned()).toBe('12345');
  });

  it('does not emit barcode shorter than MIN_LENGTH', () => {
    service.enable();
    '123'.split('').forEach(k => fire(k));
    fire('Enter');
    expect(service.scanned()).toBeNull();
  });

  it('clear() resets the signal to null', () => {
    service.enable();
    '12345'.split('').forEach(k => fire(k));
    fire('Enter');
    service.clear();
    expect(service.scanned()).toBeNull();
  });

  it('ignores non-printable keys (e.g. Shift, Control)', () => {
    service.enable();
    fire('Shift');
    fire('Control');
    '12345'.split('').forEach(k => fire(k));
    fire('Enter');
    expect(service.scanned()).toBe('12345');
  });
});
