import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { HomeComponent } from './home.component';
import { AuthService } from '../auth/auth.service';
import { AuthUser } from '../auth/auth.model';

describe('HomeComponent', () => {
  function setup(currentUser: AuthUser | null) {
    const authServiceMock = {
      currentUser: signal(currentUser).asReadonly(),
    };

    TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [{ provide: AuthService, useValue: authServiceMock }],
    });

    return TestBed.createComponent(HomeComponent);
  }

  it('should create the home component', () => {
    const fixture = setup({ username: 'testuser', role: 'Admin' });
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the current username', () => {
    const fixture = setup({ username: 'testuser', role: 'Admin' });
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('testuser');
  });

  it('should display fallback label when currentUser is null', () => {
    const fixture = setup(null);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('ผู้ใช้');
  });
});
