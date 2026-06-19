import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { AppShellComponent } from './app-shell.component';
import { AuthService } from '../auth/auth.service';
import { routes } from '../app.routes';

describe('AppShellComponent', () => {
  const authServiceMock = {
    currentUser: signal({ username: 'testuser', role: 'Admin' as const }).asReadonly(),
    isAdmin: signal(true).asReadonly(),
    logout: vi.fn(),
  };

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [
        provideRouter(routes),
        { provide: AuthService, useValue: authServiceMock },
      ],
    }).compileComponents();
  });

  it('should create the shell', () => {
    const fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the current username in the topbar', () => {
    const fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.user-label')?.textContent).toContain('testuser');
  });

  it('should toggle sidebar collapsed state', () => {
    const fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();

    const shell = fixture.componentInstance;
    expect(shell.sidebarCollapsed()).toBe(false);

    const toggle = fixture.nativeElement.querySelector('.sidebar-toggle') as HTMLButtonElement;
    toggle.click();
    fixture.detectChanges();

    expect(shell.sidebarCollapsed()).toBe(true);
    expect(localStorage.getItem('app-sidebar-collapsed')).toBe('true');
  });

  it('should render navigation links for home and machine', () => {
    const fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();
    const links = Array.from(
      fixture.nativeElement.querySelectorAll('.nav-item'),
    ) as HTMLAnchorElement[];

    expect(links).toHaveLength(2);
    expect(links[0].getAttribute('href')).toBe('/');
    expect(links[1].getAttribute('href')).toBe('/machine');
  });

  it('should call logout from auth service', () => {
    const fixture = TestBed.createComponent(AppShellComponent);
    fixture.detectChanges();

    const logoutButton = fixture.nativeElement.querySelector(
      '.btn.btn-secondary',
    ) as HTMLButtonElement;
    logoutButton.click();

    expect(authServiceMock.logout).toHaveBeenCalled();
  });
});
