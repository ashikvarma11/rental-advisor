import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  function run(isLoggedIn: boolean) {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: { isLoggedIn: () => isLoggedIn } }]
    });
    return TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));
  }

  it('allows activation when logged in', () => {
    const result = run(true);
    expect(result).toBe(true);
  });

  it('redirects to /login when not logged in', () => {
    const result = run(false);
    const router = TestBed.inject(Router);
    expect(result).toEqual(router.createUrlTree(['/login']));
  });
});
