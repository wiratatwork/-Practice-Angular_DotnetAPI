import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthUser, LoginRequest, LoginResponse, UserRole } from './auth.model';

const TOKEN_KEY = 'auth_token';
const USER_KEY = 'auth_user';

@Injectable({
    providedIn: 'root',
})
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly router = inject(Router);
    private readonly baseUrl = '/api/auth';

    private readonly currentUserSignal = signal<AuthUser | null>(this.loadStoredUser());

    readonly currentUser = this.currentUserSignal.asReadonly();
    readonly isLoggedIn = computed(() => this.currentUserSignal() !== null);
    readonly isAdmin = computed(() => this.currentUserSignal()?.role === 'Admin');

    login(credentials: LoginRequest): Observable<LoginResponse> {
        return this.http.post<LoginResponse>(`${this.baseUrl}/login`, credentials).pipe(
            tap((response) => {
                this.persistSession(response);
            }),
        );
    }

    logout(): void {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        this.currentUserSignal.set(null);
        this.router.navigate(['/login']);
    }

    getToken(): string | null {
        return localStorage.getItem(TOKEN_KEY);
    }

    private persistSession(response: LoginResponse): void {
        const user: AuthUser = {
            username: response.username,
            role: response.role as UserRole,
        };

        localStorage.setItem(TOKEN_KEY, response.token);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        this.currentUserSignal.set(user);
    }

    private loadStoredUser(): AuthUser | null {
        const token = localStorage.getItem(TOKEN_KEY);
        const userJson = localStorage.getItem(USER_KEY);

        if (!token || !userJson) {
            return null;
        }

        try {
            return JSON.parse(userJson) as AuthUser;
        } catch {
            this.clearStorage();
            return null;
        }
    }

    private clearStorage(): void {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
    }
}
