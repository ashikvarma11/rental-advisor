import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { LoginComponent } from './login.component';
import { AuthService } from './auth.service';

describe('LoginComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<LoginComponent>>;
  let component: LoginComponent;
  let auth: { login: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(() => {
    auth = { login: vi.fn() };
    TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }]
    });
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  });

  it('creates', () => {
    expect(component).toBeTruthy();
  });

  it('submit() logs in and navigates to /upload on success', async () => {
    component.email = 'user@example.com';
    component.password = 'password123';
    auth.login.mockResolvedValue({ token: 'abc' });

    await component.submit();

    expect(auth.login).toHaveBeenCalledWith('user@example.com', 'password123');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/upload');
    expect(component.loading()).toBe(false);
    expect(component.error()).toBeUndefined();
  });

  it('submit() sets error on failure', async () => {
    auth.login.mockRejectedValue({ error: { error: 'invalid email or password' } });

    await component.submit();

    expect(component.error()).toBe('invalid email or password');
    expect(component.loading()).toBe(false);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
