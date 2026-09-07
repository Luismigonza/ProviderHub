import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    // withComponentInputBinding lets a route parameter arrive as a component input, which keeps
    // the screens free of ActivatedRoute plumbing once they start taking ids.
    provideRouter(routes, withComponentInputBinding()),

    // One interceptor, registered once. Every request in the application carries the token
    // without a single component having to remember.
    provideHttpClient(withInterceptors([authInterceptor])),

    // No animations provider: Angular Material 21 animates with CSS, so @angular/animations is
    // a dependency this application does not have to carry.
  ],
};
