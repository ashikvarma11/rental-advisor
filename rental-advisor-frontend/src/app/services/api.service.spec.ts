import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';

describe('ApiService', () => {
  let service: ApiService;
  let httpMock: HttpTestingController;
  const base = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApiService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('compareSuburb GETs with postcode param', async () => {
    const promise = service.compareSuburb('2010');
    const req = httpMock.expectOne(r => r.url === `${base}/api/compare/suburb`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('postcode')).toBe('2010');
    req.flush({ median: 500 });
    expect(await promise).toEqual({ median: 500 });
  });

  it('uploadLease POSTs a FormData with the file', async () => {
    const file = new File(['content'], 'lease.pdf');
    const promise = service.uploadLease(file);
    const req = httpMock.expectOne(`${base}/api/leases/upload`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBe(true);
    expect((req.request.body as FormData).get('file')).toStrictEqual(file);
    req.flush({ id: 1 });
    expect(await promise).toEqual({ id: 1 });
  });

  it('getClauses GETs clauses for a lease', async () => {
    const promise = service.getClauses(42);
    const req = httpMock.expectOne(`${base}/api/leases/42/clauses`);
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 1, text: 'clause' }]);
    expect(await promise).toEqual([{ id: 1, text: 'clause' }]);
  });

  it('extractClauses POSTs to trigger extraction', async () => {
    const promise = service.extractClauses(42);
    const req = httpMock.expectOne(`${base}/api/leases/42/extract-clauses`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({ ok: true });
    expect(await promise).toEqual({ ok: true });
  });

  it('getExtractStatus GETs the extraction status for a lease', async () => {
    const promise = service.getExtractStatus(42);
    const req = httpMock.expectOne(`${base}/api/leases/42/extract-status`);
    expect(req.request.method).toBe('GET');
    req.flush({ status: 'Done', clausesCreated: 3 });
    expect(await promise).toEqual({ status: 'Done', clausesCreated: 3 });
  });

  it('resolveClause POSTs to resolve a clause', async () => {
    const promise = service.resolveClause(7);
    const req = httpMock.expectOne(`${base}/api/clauses/7/resolve`);
    expect(req.request.method).toBe('POST');
    req.flush({ isResolved: true });
    expect(await promise).toEqual({ isResolved: true });
  });
});
