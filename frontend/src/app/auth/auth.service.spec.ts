import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';
import { TokenResponse } from './auth.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  const tokenResponse: TokenResponse = {
    accessToken: 'access-token',
    expiresIn: 900,
    user: { username: 'admin', role: 'Admin' },
  };

  beforeEach(() => {
    routerMock = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: routerMock },
      ],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should login and apply session', async () => {
    const loginPromise = firstValueFrom(
      service.login({ username: 'admin', password: 'Admin@1234' }),
    );

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.body).toEqual({ username: 'admin', password: 'Admin@1234' });
    req.flush(tokenResponse);

    await loginPromise;

    expect(service.getAccessToken()).toBe('access-token');
    expect(service.currentUser()).toEqual({ username: 'admin', role: 'Admin' });
    expect(service.isLoggedIn()).toBe(true);
    expect(service.isAdmin()).toBe(true);
  });

  it('should short-circuit initializeSession when already logged in', async () => {
    const loginPromise = firstValueFrom(
      service.login({ username: 'admin', password: 'Admin@1234' }),
    );
    httpMock.expectOne('/api/auth/login').flush(tokenResponse);
    await loginPromise;

    const initialized = await firstValueFrom(service.initializeSession());

    expect(initialized).toBe(true);
    httpMock.expectNone('/api/auth/refresh');
  });

  it('should refresh session successfully', async () => {
    const refreshPromise = firstValueFrom(service.refreshSession());

    const req = httpMock.expectOne('/api/auth/refresh');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    req.flush(tokenResponse);

    const refreshed = await refreshPromise;

    expect(refreshed).toBe(true);
    expect(service.currentUser()?.username).toBe('admin');
  });

  it('should clear session when refresh fails without navigating', async () => {
    const refreshPromise = firstValueFrom(service.refreshSession());

    const req = httpMock.expectOne('/api/auth/refresh');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    const refreshed = await refreshPromise;

    expect(refreshed).toBe(false);
    expect(service.isLoggedIn()).toBe(false);
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it('should share one in-flight refresh request', async () => {
    const firstRefresh = firstValueFrom(service.refreshSession());
    const secondRefresh = firstValueFrom(service.refreshSession());

    const req = httpMock.expectOne('/api/auth/refresh');
    req.flush(tokenResponse);

    const [firstResult, secondResult] = await Promise.all([firstRefresh, secondRefresh]);

    expect(firstResult).toBe(true);
    expect(secondResult).toBe(true);
    httpMock.expectNone('/api/auth/refresh');
  });

  it('should logout, clear session, and navigate to login', () => {
    const loginSub = service.login({ username: 'admin', password: 'Admin@1234' }).subscribe();
    httpMock.expectOne('/api/auth/login').flush(tokenResponse);
    loginSub.unsubscribe();

    service.logout();

    const req = httpMock.expectOne('/api/auth/logout');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});

    expect(service.isLoggedIn()).toBe(false);
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
  });
});
