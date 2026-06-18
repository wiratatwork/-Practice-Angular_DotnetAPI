import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';
import { AuthUser, LoginRequest, TokenResponse, UserRole } from './auth.model';

const AUTH_HTTP_OPTIONS = {
    withCredentials: true,
};

@Injectable({
    providedIn: 'root',
})
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly router = inject(Router);
    private readonly baseUrl = '/api/auth';

    private readonly accessTokenSignal = signal<string | null>(null);
    private readonly currentUserSignal = signal<AuthUser | null>(null);
    private refreshInProgress: Observable<boolean> | null = null;

    readonly currentUser = this.currentUserSignal.asReadonly();
    readonly isLoggedIn = computed(() => this.currentUserSignal() !== null);
    readonly isAdmin = computed(() => this.currentUserSignal()?.role === 'Admin');

    login(credentials: LoginRequest): Observable<TokenResponse> {
        return this.http
            .post<TokenResponse>(`${this.baseUrl}/login`, credentials, AUTH_HTTP_OPTIONS)
            .pipe(tap((response) => this.applySession(response)));
    }

    refreshSession(): Observable<boolean> {
        if (this.refreshInProgress) {
            return this.refreshInProgress;
        }

        this.refreshInProgress = this.http
            .post<TokenResponse>(`${this.baseUrl}/refresh`, {}, AUTH_HTTP_OPTIONS)
            .pipe(
                tap((response) => this.applySession(response)),
                map(() => true),
                catchError(() => {
                    this.clearSession(false);
                    return of(false);
                }),
                finalize(() => {
                    this.refreshInProgress = null;
                }),
                shareReplay(1),
            );

        return this.refreshInProgress;
    }

    initializeSession(): Observable<boolean> {
        if (this.isLoggedIn()) {
            return of(true);
        }

        return this.refreshSession();
    }

    logout(): void {
        this.http
            .post(`${this.baseUrl}/logout`, {}, AUTH_HTTP_OPTIONS)
            .pipe(finalize(() => this.clearSession(true)))
            .subscribe({
                error: () => this.clearSession(true),
            });
    }

    getAccessToken(): string | null {
        return this.accessTokenSignal();
    }

    private applySession(response: TokenResponse): void {
        const user: AuthUser = {
            username: response.user.username,
            role: response.user.role as UserRole,
        };

        this.accessTokenSignal.set(response.accessToken);
        this.currentUserSignal.set(user);
    }

    private clearSession(navigateToLogin: boolean): void {
        this.accessTokenSignal.set(null);
        this.currentUserSignal.set(null);

        if (navigateToLogin) {
            this.router.navigate(['/login']);
        }
    }
}
