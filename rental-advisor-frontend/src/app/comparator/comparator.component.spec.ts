import { TestBed } from '@angular/core/testing';
import { ComparatorComponent } from './comparator.component';
import { ApiService } from '../services/api.service';

describe('ComparatorComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<ComparatorComponent>>;
  let component: ComparatorComponent;
  let api: { compareSuburb: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { compareSuburb: vi.fn() };
    TestBed.configureTestingModule({
      imports: [ComparatorComponent],
      providers: [{ provide: ApiService, useValue: api }]
    });
    fixture = TestBed.createComponent(ComparatorComponent);
    component = fixture.componentInstance;
  });

  it('creates with default postcode', () => {
    expect(component).toBeTruthy();
    expect(component.postcode).toBe('2010');
  });

  it('compare() sets result on success', async () => {
    const result = { postcode: '2010', median: 500, averageListingRent: 520, minRent: 400, maxRent: 600, stdDeviation: 50, listingCount: 10, isLowConfidence: false, differencePercent: 4 };
    api.compareSuburb.mockResolvedValue(result);

    await component.compare();

    expect(api.compareSuburb).toHaveBeenCalledWith('2010');
    expect(component.result()).toEqual(result);
    expect(component.loading()).toBe(false);
    expect(component.error()).toBeUndefined();
  });

  it('compare() sets error and clears result on failure', async () => {
    component.result.set({ postcode: '2010', median: 1, averageListingRent: 1, minRent: 1, maxRent: 1, stdDeviation: 1, listingCount: 1, isLowConfidence: false, differencePercent: 1 });
    api.compareSuburb.mockRejectedValue({ error: { error: 'no data' } });

    await component.compare();

    expect(component.error()).toBe('no data');
    expect(component.result()).toBeNull();
    expect(component.loading()).toBe(false);
  });

  it('barWidth caps at 100 and handles falsy value', () => {
    expect(component.barWidth(50, 100)).toBe(50);
    expect(component.barWidth(200, 100)).toBe(100);
    expect(component.barWidth(null, 100)).toBe(0);
  });

  it('diffClass maps difference to badge class', () => {
    expect(component.diffClass(null)).toBe('badge-med');
    expect(component.diffClass(-5)).toBe('badge-low');
    expect(component.diffClass(5)).toBe('badge-med');
    expect(component.diffClass(15)).toBe('badge-high');
  });

  it('renders empty panel before any comparison', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.empty-panel')).toBeTruthy();
    expect(compiled.querySelector('.result-panel')).toBeNull();
  });

  it('renders result panel after successful compare', async () => {
    api.compareSuburb.mockResolvedValue({ postcode: '2010', median: 500, averageListingRent: 520, minRent: 400, maxRent: 600, stdDeviation: 50, listingCount: 10, isLowConfidence: false, differencePercent: 4 });
    await component.compare();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.result-panel')).toBeTruthy();
    expect(compiled.querySelector('.result-footnote')?.textContent).toContain('10 tracked listings');
  });
});
