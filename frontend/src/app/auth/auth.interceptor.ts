import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const AUTH_PATHS = ['/api/auth/login', '/api/auth/refresh', '/api/auth/logout'];

function isAuthPath(url: string): boolean {
  return AUTH_PATHS.some((path) => url.includes(path));
}

function withBearerToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`,
    },
  });
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getAccessToken();

  const authReq = token && !isAuthPath(req.url) ? withBearerToken(req, token) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthPath(req.url)) {
        return throwError(() => error);
      }

      return authService.refreshSession().pipe(
        switchMap((success) => {
          if (!success) {
            return throwError(() => error);
          }

          const refreshedToken = authService.getAccessToken();
          if (!refreshedToken) {
            return throwError(() => error);
          }

          return next(withBearerToken(req, refreshedToken));
        }),
      );
    }),
  );
};
