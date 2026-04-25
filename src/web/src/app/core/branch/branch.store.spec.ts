import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { BranchStore, BranchItem } from './branch.store';
import { ApiEndpoints } from '../../shared/messages/app-api';

const BRANCHES: BranchItem[] = [
  { id: 1, name: 'Head Office', city: 'Mumbai', phone: null, isActive: true, isHeadOffice: true },
  { id: 2, name: 'Warehouse', city: 'Pune',   phone: null, isActive: true, isHeadOffice: false },
  { id: 3, name: 'Old Branch', city: null,     phone: null, isActive: false, isHeadOffice: false },
];

describe('BranchStore', () => {
  let store: BranchStore;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    store = TestBed.inject(BranchStore);
    http  = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('should start with no branches and null activeBranchId', () => {
    expect(store.branches()).toEqual([]);
    expect(store.activeBranchId()).toBeNull();
  });

  describe('load()', () => {
    it('sets only active branches', () => {
      store.load();
      http.expectOne(ApiEndpoints.admin.branches).flush(BRANCHES);
      expect(store.branches().length).toBe(2);
      expect(store.branches().every(b => b.isActive)).toBeTrue();
    });

    it('clears stale activeBranchId when stored branch is deactivated', () => {
      store.setActive(3); // branch 3 is inactive
      store.load();
      http.expectOne(ApiEndpoints.admin.branches).flush(BRANCHES);
      expect(store.activeBranchId()).toBeNull();
    });
  });

  describe('setActive()', () => {
    it('updates activeBranchId signal and localStorage', () => {
      store.setActive(2);
      expect(store.activeBranchId()).toBe(2);
      expect(localStorage.getItem('active-branch-id')).toBe('2');
    });

    it('clears localStorage when null is passed', () => {
      store.setActive(2);
      store.setActive(null);
      expect(store.activeBranchId()).toBeNull();
      expect(localStorage.getItem('active-branch-id')).toBeNull();
    });
  });

  describe('activeBranch computed', () => {
    beforeEach(() => {
      store.load();
      http.expectOne(ApiEndpoints.admin.branches).flush(BRANCHES);
    });

    it('returns matching branch when activeBranchId is set', () => {
      store.setActive(2);
      expect(store.activeBranch()?.id).toBe(2);
    });

    it('returns first branch when activeBranchId is null', () => {
      store.setActive(null);
      expect(store.activeBranch()?.id).toBe(1);
    });
  });
});
