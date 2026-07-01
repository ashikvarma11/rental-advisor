import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, timeout } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class WakeService {
  ready = signal(!environment.production);
  waking = signal(false);

  constructor(private http: HttpClient) {}

  async ping() {
    if (this.ready()) return;
    this.waking.set(true);
    try {
      await firstValueFrom(this.http.get(`${environment.apiUrl}/health`).pipe(timeout(2500)));
      this.ready.set(true);
    } catch {
      // still cold — poll until the server wakes up
      while (!this.ready()) {
        try {
          await firstValueFrom(this.http.get(`${environment.apiUrl}/health`).pipe(timeout(5000)));
          this.ready.set(true);
        } catch {
          await new Promise(r => setTimeout(r, 2000));
        }
      }
    } finally {
      this.waking.set(false);
    }
  }
}
