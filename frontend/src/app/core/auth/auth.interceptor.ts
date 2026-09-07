import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';

/** The one endpoint that must not carry a token, since obtaining one is what it is for. */
const ANONYMOUS_ENDPOINTS = ['/api/auth/login'];

/**
 * Attaches the bearer token to every call to this API, and signs the user out when the server
 * says the token is no longer good.
 *
 * A functional interceptor rather than a class: it is a function of a request, and `inject()`
 * works inside it, so there is nothing a class would add.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.accessToken();
  const isAnonymous = ANONYMOUS_ENDPOINTS.some((url) => request.url.startsWith(url));

  // Only requests to this API get the header. Sending a token to a third party because a URL
  // happened to pass through here would leak the session.
  const authorized =
    token !== null && !isAnonymous && request.url.startsWith('/api')
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isAnonymous) {
        // The token was rejected: expired, or signed with a key that has since changed. Staying
        // on the page would mean every subsequent request failing in the same silent way.
        auth.signOut(router.url);
      }

      return throwError(() => error);
    }),
  );
};
