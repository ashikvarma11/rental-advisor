import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private base = environment.apiUrl;
  token = signal<string | null>(localStorage.getItem('token'));
  isLoggedIn = computed(() => !!this.token());

  constructor(private http: HttpClient) {}

  async register(email: string, password: string) {
    const result: any = await firstValueFrom(this.http.post(`${this.base}/api/auth/register`, { email, password }));
    this.setToken(result.token);
    return result;
  }

  async login(email: string, password: string) {
    const result: any = await firstValueFrom(this.http.post(`${this.base}/api/auth/login`, { email, password }));
    this.setToken(result.token);
    return result;
  }

  logout() {
    localStorage.removeItem('token');
    this.token.set(null);
  }

  private setToken(token: string) {
    localStorage.setItem('token', token);
    this.token.set(token);
  }
}
