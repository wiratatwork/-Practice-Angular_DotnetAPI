import { TestBed } from '@angular/core/testing';
import { GuardResult, Router, UrlTree } from '@angular/router';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  let authServiceMock: {
    isLoggedIn: ReturnType<typeof vi.fn>;
    initializeSession: ReturnType<typeof vi.fn>;
  };
  let loginUrlTree: UrlTree;
  let routerMock: {
    createUrlTree: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    loginUrlTree = {} as UrlTree;

    authServiceMock = {
      isLoggedIn: vi.fn(() => false),
      initializeSession: vi.fn(() => of(false)),
    };
    routerMock = {
      createUrlTree: vi.fn(() => loginUrlTree),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    });
  });

  async function runGuard(): Promise<GuardResult> {
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as Parameters<typeof authGuard>[0], {} as Parameters<typeof authGuard>[1]),
    );

    if (isObservable(result)) {
      return firstValueFrom(result);
    }

    return result;
  }

  it('should allow activation when user is already logged in', async () => {
    authServiceMock.isLoggedIn.mockReturnValue(true);

    const result = await runGuard();

    expect(result).toBe(true);
    expect(authServiceMock.initializeSession).not.toHaveBeenCalled();
  });

  it('should allow activation when initializeSession succeeds', async () => {
    authServiceMock.initializeSession.mockReturnValue(of(true));

    const result = await runGuard();

    expect(result).toBe(true);
    expect(authServiceMock.initializeSession).toHaveBeenCalled();
  });

  it('should redirect to login when initializeSession fails', async () => {
    authServiceMock.initializeSession.mockReturnValue(of(false));

    const result = await runGuard();

    expect(routerMock.createUrlTree).toHaveBeenCalledWith(['/login']);
    expect(result).toBe(loginUrlTree);
  });
});
