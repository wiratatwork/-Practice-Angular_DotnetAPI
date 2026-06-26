import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideIcons } from '@ng-icons/core';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../auth.service';
import { APP_ICON_REGISTRY } from '../../shared/app-icons';

describe('LoginComponent', () => {
  let router: Router;
  let authServiceMock: {
    isLoggedIn: ReturnType<typeof vi.fn>;
    initializeSession: ReturnType<typeof vi.fn>;
    login: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    authServiceMock = {
      isLoggedIn: vi.fn(() => false),
      initializeSession: vi.fn(() => of(false)),
      login: vi.fn(() =>
        of({
          accessToken: 'token',
          expiresIn: 900,
          user: { username: 'admin', role: 'Admin' as const },
        }),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        provideIcons(APP_ICON_REGISTRY),
        { provide: AuthService, useValue: authServiceMock },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  it('should create the login component', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should redirect to home when already logged in', () => {
    authServiceMock.isLoggedIn.mockReturnValue(true);
    TestBed.createComponent(LoginComponent);
    expect(router.navigate).toHaveBeenCalledWith(['/']);
    expect(authServiceMock.initializeSession).not.toHaveBeenCalled();
  });

  it('should initialize session when not logged in', () => {
    TestBed.createComponent(LoginComponent);
    expect(authServiceMock.initializeSession).toHaveBeenCalled();
  });

  it('should redirect when initializeSession succeeds', () => {
    authServiceMock.initializeSession.mockReturnValue(of(true));
    TestBed.createComponent(LoginComponent);
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should not call login when form is invalid', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();

    fixture.componentInstance.submit();

    expect(authServiceMock.login).not.toHaveBeenCalled();
  });

  it('should login with trimmed username and redirect on success', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();

    fixture.componentInstance.form.setValue({
      username: '  admin  ',
      password: 'Admin@1234',
    });
    fixture.componentInstance.submit();

    expect(authServiceMock.login).toHaveBeenCalledWith({
      username: 'admin',
      password: 'Admin@1234',
    });
    expect(fixture.componentInstance.isSubmitting()).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should show backend error message on login failure', () => {
    authServiceMock.login.mockReturnValue(
      throwError(() => ({ error: { message: 'Invalid credentials' } })),
    );

    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();

    fixture.componentInstance.form.setValue({
      username: 'admin',
      password: 'wrong',
    });
    fixture.componentInstance.submit();

    expect(fixture.componentInstance.errorMessage()).toBe('Invalid credentials');
    expect(fixture.componentInstance.isSubmitting()).toBe(false);
  });

  it('should show fallback error message when backend message is missing', () => {
    authServiceMock.login.mockReturnValue(throwError(() => ({})));

    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();

    fixture.componentInstance.form.setValue({
      username: 'admin',
      password: 'wrong',
    });
    fixture.componentInstance.submit();

    expect(fixture.componentInstance.errorMessage()).toBe('ไม่สามารถเข้าสู่ระบบได้ กรุณาลองใหม่');
  });
});
