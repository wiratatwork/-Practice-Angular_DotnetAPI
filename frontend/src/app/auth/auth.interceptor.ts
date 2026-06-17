import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);
    const token = authService.getToken();

    const isAuthRequest = req.url.includes('/api/auth/login');
    const authReq = token && !isAuthRequest
        ? req.clone({
              setHeaders: {
                  Authorization: `Bearer ${token}`,
              },
          })
        : req;

    return next(authReq).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401 && !isAuthRequest) {
                authService.logout();
            }
            return throwError(() => error);
        }),
    );
};
