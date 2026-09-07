import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AccessToken } from '../api/models';

const STORAGE_KEY = 'providerhub.session';

interface StoredSession {
  readonly accessToken: string;
  readonly expiresAt: string;
  readonly userName: string;
}

/**
 * Holds the session and knows how to start and end one.
 *
 * State lives in signals rather than in a `BehaviorSubject`: a template can read
 * `auth.isSignedIn()` directly, and Angular tracks the dependency itself, so no component has to
 * subscribe or remember to unsubscribe.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<StoredSession | null>(readStoredSession());

  /** The raw token, for the interceptor. Null when there is no usable session. */
  readonly accessToken = computed(() => {
    const current = this.session();

    return current !== null && !hasExpired(current) ? current.accessToken : null;
  });

  readonly isSignedIn = computed(() => this.accessToken() !== null);

  readonly userName = computed(() => this.session()?.userName ?? null);

  async signIn(userName: string, password: string): Promise<void> {
    const token = await firstValueFrom(
      this.http.post<AccessToken>('/api/auth/login', { userName, password }),
    );

    const session: StoredSession = {
      accessToken: token.accessToken,
      expiresAt: token.expiresAt,
      userName,
    };

    this.session.set(session);
    writeStoredSession(session);
  }

  /**
   * Ends the session locally. There is nothing to tell the server: a JWT is stateless, so a
   * token stays technically valid until it expires. Revoking one before then would need a deny
   * list on the server, which is the cost of statelessness and a deliberate trade.
   */
  signOut(returnUrl?: string): void {
    this.session.set(null);
    clearStoredSession();

    void this.router.navigate(['/login'], {
      queryParams: returnUrl ? { returnUrl } : undefined,
    });
  }
}

/**
 * The session survives a page reload because it is kept in `localStorage`.
 *
 * That is a deliberate trade and worth stating plainly: anything in `localStorage` is readable
 * by any script running on the page, so a cross-site scripting hole would hand the token over.
 * The stronger arrangement is an `HttpOnly` cookie, which JavaScript cannot read at all, and it
 * requires the server to issue and validate cookies plus CSRF protection. For a bearer-token API
 * of this size, storage is the honest compromise; the reason it is safe enough here is that the
 * app renders no untrusted HTML.
 */
function readStoredSession(): StoredSession | null {
  const raw = localStorage.getItem(STORAGE_KEY);

  if (raw === null) {
    return null;
  }

  try {
    const session = JSON.parse(raw) as StoredSession;

    // An expired token would only produce 401s. Dropping it here means the app starts on the
    // login screen instead of looking signed in and failing on the first request.
    return hasExpired(session) ? null : session;
  } catch {
    clearStoredSession();

    return null;
  }
}

function writeStoredSession(session: StoredSession): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

function clearStoredSession(): void {
  localStorage.removeItem(STORAGE_KEY);
}

function hasExpired(session: StoredSession): boolean {
  return new Date(session.expiresAt).getTime() <= Date.now();
}
