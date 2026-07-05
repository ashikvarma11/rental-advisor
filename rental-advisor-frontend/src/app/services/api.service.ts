import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = environment.apiUrl;
  constructor(private http: HttpClient) {}

  compareSuburb(postcode: string) {
    const params = new HttpParams().set('postcode', postcode);
    return firstValueFrom(this.http.get(`${this.base}/api/compare/suburb`, { params }));
  }

  getLeases() {
    return firstValueFrom(this.http.get(`${this.base}/api/leases`));
  }

  uploadLease(file: File) {
    const fd = new FormData();
    fd.append('file', file, file.name);
    return firstValueFrom(this.http.post(`${this.base}/api/leases/upload`, fd));
  }

  getClauses(leaseId: number) {
    return firstValueFrom(this.http.get(`${this.base}/api/leases/${leaseId}/clauses`));
  }

  extractClauses(leaseId: number) {
    return firstValueFrom(this.http.post(`${this.base}/api/leases/${leaseId}/extract-clauses`, {}));
  }

  getExtractStatus(leaseId: number) {
    return firstValueFrom(this.http.get(`${this.base}/api/leases/${leaseId}/extract-status`));
  }

  resolveClause(clauseId: number) {
    return firstValueFrom(this.http.post(`${this.base}/api/clauses/${clauseId}/resolve`, {}));
  }
}
