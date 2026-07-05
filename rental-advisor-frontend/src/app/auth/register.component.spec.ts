import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RegisterComponent } from './register.component';
import { AuthService } from './auth.service';

describe('RegisterComponent', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<RegisterComponent>>;
  let component: RegisterComponent;
  let auth: { register: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(() => {
    auth = { register: vi.fn() };
    TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }]
    });
    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  });

  it('creates', () => {
    expect(component).toBeTruthy();
  });

  it('submit() registers and navigates to /upload on success', async () => {
    component.email = 'new@example.com';
    component.password = 'password123';
    auth.register.mockResolvedValue({ token: 'abc' });

    await component.submit();

    expect(auth.register).toHaveBeenCalledWith('new@example.com', 'password123');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/upload');
    expect(component.loading()).toBe(false);
    expect(component.error()).toBeUndefined();
  });

  it('submit() sets error on failure', async () => {
    auth.register.mockRejectedValue({ error: { error: 'email already registered' } });

    await component.submit();

    expect(component.error()).toBe('email already registered');
    expect(component.loading()).toBe(false);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
