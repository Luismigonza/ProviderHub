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
    path: 'providers',
    title: 'Providers · ProviderHub',
    canActivate: [authGuard],
    loadComponent: () => import('./features/providers/provider-list').then((m) => m.ProviderList),
  },
  {
    path: 'providers/:id',
    title: 'Provider · ProviderHub',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/providers/provider-detail').then((m) => m.ProviderDetail),
  },
  {
    path: 'services',
    title: 'Services · ProviderHub',
    canActivate: [authGuard],
    loadComponent: () => import('./features/services/service-list').then((m) => m.ServiceList),
  },
  {
    path: '',
    title: 'Dashboard · ProviderHub',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
  },

  // Anything unrecognized goes home, where the guard decides whether that means the application
  // or the login screen.
  { path: '**', redirectTo: '' },
];
