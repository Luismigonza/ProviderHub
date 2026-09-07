import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

/**
 * Keeps unauthenticated visitors out of the application.
 *
 * This is a convenience, not a security control: everything it protects is a screen, and the
 * data behind those screens is protected by the API, which refuses any request without a valid
 * token. A guard that could be bypassed by editing the URL would be the only thing standing
 * between a visitor and the data if the server trusted the client, and the server does not.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isSignedIn()) {
    return true;
  }

  // Where the user was heading, so signing in resumes it instead of dropping them at the start.
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};

/** Keeps a signed-in user away from the login screen. */
export const anonymousGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isSignedIn() ? router.createUrlTree(['/']) : true;
};
