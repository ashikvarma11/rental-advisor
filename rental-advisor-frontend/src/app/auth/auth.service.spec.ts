import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const base = environment.apiUrl;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('login_ValidCredentials_StoresToken', async () => {
    const promise = service.login('user@example.com', 'password123');
    const req = httpMock.expectOne(`${base}/api/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@example.com', password: 'password123' });
    req.flush({ token: 'abc.def.ghi' });
    await promise;

    expect(service.token()).toBe('abc.def.ghi');
    expect(localStorage.getItem('token')).toBe('abc.def.ghi');
    expect(service.isLoggedIn()).toBe(true);
  });

  it('register_ValidDetails_StoresToken', async () => {
    const promise = service.register('new@example.com', 'password123');
    const req = httpMock.expectOne(`${base}/api/auth/register`);
    expect(req.request.method).toBe('POST');
    req.flush({ token: 'xyz' });
    await promise;

    expect(service.token()).toBe('xyz');
  });

  it('logout_ClearsToken', async () => {
    const promise = service.login('user@example.com', 'password123');
    httpMock.expectOne(`${base}/api/auth/login`).flush({ token: 'abc' });
    await promise;

    service.logout();

    expect(service.token()).toBeNull();
    expect(localStorage.getItem('token')).toBeNull();
    expect(service.isLoggedIn()).toBe(false);
  });
});
