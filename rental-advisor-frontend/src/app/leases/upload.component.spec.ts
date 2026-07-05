import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { LeaseUploadComponent } from './upload.component';
import { ApiService } from '../services/api.service';

describe('LeaseUploadComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<LeaseUploadComponent>>;
  let component: LeaseUploadComponent;
  let api: { uploadLease: ReturnType<typeof vi.fn>; extractClauses: ReturnType<typeof vi.fn>; getExtractStatus: ReturnType<typeof vi.fn>; getClauses: ReturnType<typeof vi.fn>; resolveClause: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = {
      uploadLease: vi.fn(),
      extractClauses: vi.fn(),
      getExtractStatus: vi.fn(),
      getClauses: vi.fn(),
      resolveClause: vi.fn()
    };
    TestBed.configureTestingModule({
      imports: [LeaseUploadComponent],
      providers: [
        { provide: ApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({}) } } }
      ]
    });
    fixture = TestBed.createComponent(LeaseUploadComponent);
    component = fixture.componentInstance;
  });

  it('creates', () => {
    expect(component).toBeTruthy();
  });

  it('setFile via onFileChange sets the file and clears prior state', () => {
    const file = new File(['x'], 'lease.pdf');
    component.error.set('old error');
    component.onFileChange({ target: { files: [file] } });
    expect(component.file()).toBe(file);
    expect(component.error()).toBeUndefined();
    expect(component.clauses()).toEqual([]);
    expect(component.leaseId).toBeUndefined();
  });

  it('upload() polls extraction status and loads clauses once done', async () => {
    const file = new File(['x'], 'lease.pdf');
    component.file.set(file);
    api.uploadLease.mockResolvedValue({ id: 5 });
    api.extractClauses.mockResolvedValue({ status: 'queued' });
    api.getExtractStatus.mockResolvedValue({ status: 'Done' });
    api.getClauses.mockResolvedValue([{ id: 1, text: 'c1', riskScore: 0.8, suggestion: null, isResolved: false }]);

    await component.upload();

    expect(api.uploadLease).toHaveBeenCalledWith(file);
    expect(api.extractClauses).toHaveBeenCalledWith(5);
    expect(api.getExtractStatus).toHaveBeenCalledWith(5);
    expect(component.leaseId).toBe(5);
    expect(component.clauses().length).toBe(1);
    expect(component.loading()).toBe(false);
    expect(component.processing()).toBe(false);
    expect(component.error()).toBeUndefined();
  });

  it('upload() sets error state on failure', async () => {
    component.file.set(new File(['x'], 'lease.pdf'));
    api.uploadLease.mockRejectedValue({ error: { error: 'bad file' } });

    await component.upload();

    expect(component.error()).toBe('bad file');
    expect(component.loading()).toBe(false);
    expect(component.clauses()).toEqual([]);
  });

  it('upload() shows error when the extraction job fails', async () => {
    component.file.set(new File(['x'], 'lease.pdf'));
    api.uploadLease.mockResolvedValue({ id: 9 });
    api.extractClauses.mockResolvedValue({ status: 'queued' });
    api.getExtractStatus.mockResolvedValue({ status: 'Failed', error: 'No text content available' });

    await component.upload();

    expect(component.error()).toBe('No text content available');
    expect(component.processing()).toBe(false);
    expect(api.getClauses).not.toHaveBeenCalled();
  });

  it('upload() is a no-op when no file selected', async () => {
    await component.upload();
    expect(api.uploadLease).not.toHaveBeenCalled();
    expect(component.loading()).toBe(false);
  });

  it('filteredClauses filters by risk level', () => {
    component.clauses.set([
      { id: 1, text: 'a', riskScore: 0.8, suggestion: null, isResolved: false },
      { id: 2, text: 'b', riskScore: 0.3, suggestion: null, isResolved: false },
      { id: 3, text: 'c', riskScore: 0.1, suggestion: null, isResolved: true }
    ]);

    component.filter.set('high');
    expect(component.filteredClauses().map(c => c.id)).toEqual([1]);

    component.filter.set('med');
    expect(component.filteredClauses().map(c => c.id)).toEqual([2]);

    component.filter.set('low');
    expect(component.filteredClauses().map(c => c.id)).toEqual([3]);

    component.filter.set('resolved');
    expect(component.filteredClauses().map(c => c.id)).toEqual([3]);

    component.filter.set('all');
    expect(component.filteredClauses().length).toBe(3);
  });

  it('toggleFilter toggles the same filter back to all', () => {
    component.toggleFilter('high');
    expect(component.filter()).toBe('high');
    component.toggleFilter('high');
    expect(component.filter()).toBe('all');
  });

  it('resolveClause marks a clause resolved on success', async () => {
    component.clauses.set([{ id: 1, text: 'a', riskScore: 0.8, suggestion: null, isResolved: false }]);
    api.resolveClause.mockResolvedValue({});

    await component.resolveClause(component.clauses()[0]);

    expect(api.resolveClause).toHaveBeenCalledWith(1);
    expect(component.clauses()[0].isResolved).toBe(true);
  });

  it('resolveClause sets message on failure', async () => {
    component.clauses.set([{ id: 1, text: 'a', riskScore: 0.8, suggestion: null, isResolved: false }]);
    api.resolveClause.mockRejectedValue(new Error('resolve failed'));

    await component.resolveClause(component.clauses()[0]);

    expect(component.message()).toBe('resolve failed');
    expect(component.clauses()[0].isResolved).toBe(false);
  });

  it('riskLabel and riskClass map score to label/class', () => {
    expect(component.riskLabel(0.8)).toBe('High risk');
    expect(component.riskClass(0.8)).toBe('badge-high');
    expect(component.riskLabel(0.3)).toBe('Medium risk');
    expect(component.riskClass(0.3)).toBe('badge-med');
    expect(component.riskLabel(0.1)).toBe('Low risk');
    expect(component.riskClass(0.1)).toBe('badge-low');
  });

  it('renders the drop zone with no file initially', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.drop-title')?.textContent).toContain('Drag & drop');
    expect(compiled.querySelector('.clauses-page')).toBeNull();
  });
});
