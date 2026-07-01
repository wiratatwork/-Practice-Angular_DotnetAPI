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
    // จำลอง TokenResponse ด้วย accessToken, expiresIn และ user
    accessToken: 'access-token',
    expiresIn: 900,
    user: { username: 'admin', role: 'Admin' },
  };

  beforeEach(() => {
    routerMock = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      // สร้าง Testing Module สำหรับการทดสอบ
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: routerMock },
      ],
    });

    service = TestBed.inject(AuthService); // ดึง AuthService ออกมาใช้ในการทดสอบ
    httpMock = TestBed.inject(HttpTestingController); // ดึง HttpTestingController ออกมาใช้ในการทดสอบ
  });

  afterEach(() => {
    httpMock.verify(); // ตรวจสอบว่าไม่มี request ที่ยังไม่ได้ตอบกลับ
  });

  it('should login and apply session', async () => {
    //ทดสอบการ login และการ apply session
    const loginPromise = firstValueFrom(
      // รอการ login และการ apply session
      service.login({ username: 'admin', password: 'Admin@1234' }),
    );

    const req = httpMock.expectOne('/api/auth/login'); // ตรวจสอบว่าเรียก API /api/auth/login
    expect(req.request.method).toBe('POST'); // ตรวจสอบว่า request เป็น POST
    expect(req.request.withCredentials).toBe(true); // ตรวจสอบว่า request มี withCredentials เป็น true
    expect(req.request.body).toEqual({ username: 'admin', password: 'Admin@1234' }); // ตรวจสอบว่า request มี body เป็น { username: 'admin', password: 'Admin@1234' }
    req.flush(tokenResponse); // จำลองการตอบกลับจาก server ด้วย tokenResponse

    await loginPromise; // รอการ login และการ apply session

    expect(service.getAccessToken()).toBe('access-token'); // ตรวจสอบว่า getAccessToken คือ 'access-token'
    expect(service.currentUser()).toEqual({ username: 'admin', role: 'Admin' }); // ตรวจสอบว่า currentUser คือ { username: 'admin', role: 'Admin' }
    expect(service.isLoggedIn()).toBe(true); // ตรวจสอบว่า isLoggedIn คือ true
    expect(service.isAdmin()).toBe(true); // ตรวจสอบว่า isAdmin คือ true
  });

  it('should short-circuit initializeSession when already logged in', async () => {
    //ทดสอบการ short-circuit initializeSession เมื่อผู้ใช้งานล็อกอินแล้ว
    const loginPromise = firstValueFrom(
      // รอการ login และการ apply session
      service.login({ username: 'admin', password: 'Admin@1234' }),
    );
    httpMock.expectOne('/api/auth/login').flush(tokenResponse); // จำลองการตอบกลับจาก server ด้วย tokenResponse
    await loginPromise; // รอการ login และการ apply session

    const initialized = await firstValueFrom(service.initializeSession()); // รอการ initializeSession

    expect(initialized).toBe(true); // ตรวจสอบว่า initializeSession คือ true
    httpMock.expectNone('/api/auth/refresh'); // ตรวจสอบว่าไม่ต้องกระทำการ refresh
  });

  it('should refresh session successfully', async () => {
    // ทดสอบการ refresh session สำเร็จ
    const refreshPromise = firstValueFrom(service.refreshSession()); // รอการ refresh session

    const req = httpMock.expectOne('/api/auth/refresh'); // ตรวจสอบว่าเรียก API /api/auth/refresh
    expect(req.request.method).toBe('POST'); // ตรวจสอบว่า request เป็น POST
    expect(req.request.withCredentials).toBe(true); // ตรวจสอบว่า request มี withCredentials เป็น true
    req.flush(tokenResponse); // จำลองการตอบกลับจาก server ด้วย tokenResponse

    const refreshed = await refreshPromise; // รอการ refresh session

    expect(refreshed).toBe(true); // ตรวจสอบว่า refresh session คือ true
    expect(service.currentUser()?.username).toBe('admin'); // ตรวจสอบว่า currentUser คือ { username: 'admin', role: 'Admin' }
  });

  it('should clear session when refresh fails without navigating', async () => {
    // ทดสอบการ clear session เมื่อ refresh ล้มเหลวและไม่ได้ navigate
    const refreshPromise = firstValueFrom(service.refreshSession()); // รอการ refresh session

    const req = httpMock.expectOne('/api/auth/refresh'); // ตรวจสอบว่าเรียก API /api/auth/refresh
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' }); // จำลองการตอบกลับจาก server ด้วย 'Unauthorized'

    const refreshed = await refreshPromise; // รอการ refresh session

    expect(refreshed).toBe(false); // ตรวจสอบว่า refresh session คือ false
    expect(service.isLoggedIn()).toBe(false); // ตรวจสอบว่า isLoggedIn คือ false
    expect(routerMock.navigate).not.toHaveBeenCalled(); // ตรวจสอบว่าไม่ได้ navigate
  });

  it('should share one in-flight refresh request', async () => {
    const firstRefresh = firstValueFrom(service.refreshSession()); // รอทดสอบการ refresh session
    const secondRefresh = firstValueFrom(service.refreshSession()); // รอทดสอบการ refresh session ครั้งที่ 2

    const req = httpMock.expectOne('/api/auth/refresh'); // ตรวจสอบว่าเรียก API /api/auth/refresh
    req.flush(tokenResponse); // จำลองการตอบกลับจาก server ด้วย tokenResponse

    const [firstResult, secondResult] = await Promise.all([firstRefresh, secondRefresh]); // รอการ refresh session ครั้งที่ 1 และ 2

    expect(firstResult).toBe(true); // ตรวจสอบว่า refresh session ครั้งที่ 1 คือ true
    expect(secondResult).toBe(true); // ตรวจสอบว่า refresh session ครั้งที่ 2 คือ true
    httpMock.expectNone('/api/auth/refresh'); // ตรวจสอบว่าไม่ต้องกระทำการ refresh
  });

  it('should logout, clear session, and navigate to login', () => {
    const loginSub = service.login({ username: 'admin', password: 'Admin@1234' }).subscribe(); // รอการ login และการ apply session
    httpMock.expectOne('/api/auth/login').flush(tokenResponse); // จำลองการตอบกลับจาก server ด้วย tokenResponse
    loginSub.unsubscribe(); // ยกเลิกการ subscribe
    // เลือกเขียน subcribe แทน firstValueFrom เพราะ ไม่ใช่ผลลัพธ์ที่คืนมา แต่ก็เขียนแบบ firstValueFrom ได้

    service.logout(); // ล็อกอินออก

    const req = httpMock.expectOne('/api/auth/logout'); // ตรวจสอบว่าเรียก API /api/auth/logout
    expect(req.request.method).toBe('POST'); // ตรวจสอบว่า request เป็น POST
    expect(req.request.withCredentials).toBe(true); // ตรวจสอบว่า request มี withCredentials เป็น true
    req.flush({}); // จำลองการตอบกลับจาก server ด้วย {}

    expect(service.isLoggedIn()).toBe(false); // ตรวจสอบว่า isLoggedIn คือ false
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']); // ตรวจสอบว่าได้ navigate ไปหน้า login
  });
});
