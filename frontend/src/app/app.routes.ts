import { Routes } from '@angular/router';

import { anonymousGuard, authGuard } from './core/auth/auth.guard';

/**
 * Every feature is loaded lazily. It costs one `loadComponent` per route and means the login
 * screen does not ship the code for screens the visitor has not reached, and may never reach.
 */
export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in · ProviderHub',
    canActivate: [anonymousGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: '',
    title: 'ProviderHub',
    canActivate: [authGuard],
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },

  // Anything unrecognized goes home, where the guard decides whether that means the application
  // or the login screen.
  { path: '**', redirectTo: '' },
];
