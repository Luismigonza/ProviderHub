import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

const STORAGE_KEY = 'providerhub.session';

function inOneHour(): string {
  return new Date(Date.now() + 3_600_000).toISOString();
}

function anHourAgo(): string {
  return new Date(Date.now() - 3_600_000).toISOString();
}

function configure(): { auth: AuthService; http: HttpTestingController; client: HttpClient } {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      // signOut navigates to the login screen, so the test router needs somewhere to land.
      // Without it the navigation rejects and shows up as an unhandled error.
      provideRouter([{ path: '**', children: [] }]),
    ],
  });

  return {
    auth: TestBed.inject(AuthService),
    http: TestBed.inject(HttpTestingController),
    client: TestBed.inject(HttpClient),
  };
}

describe('AuthService', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => TestBed.resetTestingModule());

  it('starts signed out when nothing was stored', () => {
    const { auth } = configure();

    expect(auth.isSignedIn()).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  it('stores the session after a successful sign-in', async () => {
    const { auth, http } = configure();

    const signingIn = auth.signIn('admin', 'Tekus2026!');

    const request = http.expectOne('/api/auth/login');
    expect(request.request.body).toEqual({ userName: 'admin', password: 'Tekus2026!' });
    request.flush({ accessToken: 'a-token', expiresAt: inOneHour(), tokenType: 'Bearer' });

    await signingIn;

    expect(auth.isSignedIn()).toBe(true);
    expect(auth.userName()).toBe('admin');
    expect(localStorage.getItem(STORAGE_KEY)).toContain('a-token');
  });

  it('ignores a stored session that has already expired', () => {
    // Keeping it would make the application look signed in and then fail on its first request,
    // which is a worse experience than simply asking for the password again.
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ accessToken: 'stale', expiresAt: anHourAgo(), userName: 'admin' }),
    );

    const { auth } = configure();

    expect(auth.isSignedIn()).toBe(false);
  });

  it('ignores stored rubbish instead of crashing on start-up', () => {
    localStorage.setItem(STORAGE_KEY, 'not json');

    const { auth } = configure();

    expect(auth.isSignedIn()).toBe(false);
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });

  it('forgets everything on sign-out', () => {
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ accessToken: 'a-token', expiresAt: inOneHour(), userName: 'admin' }),
    );

    const { auth } = configure();
    expect(auth.isSignedIn()).toBe(true);

    auth.signOut();

    expect(auth.isSignedIn()).toBe(false);
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });
});

describe('authInterceptor', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => TestBed.resetTestingModule());

  function signedIn(): ReturnType<typeof configure> {
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ accessToken: 'a-token', expiresAt: inOneHour(), userName: 'admin' }),
    );

    return configure();
  }

  it('attaches the token to calls to this API', () => {
    const { client, http } = signedIn();

    client.get('/api/providers').subscribe();

    const request = http.expectOne('/api/providers');
    expect(request.request.headers.get('Authorization')).toBe('Bearer a-token');
  });

  it('never sends the token to the login endpoint', () => {
    const { client, http } = signedIn();

    client.post('/api/auth/login', {}).subscribe();

    expect(http.expectOne('/api/auth/login').request.headers.has('Authorization')).toBe(false);
  });

  it('never sends the token anywhere but this API', () => {
    // A URL that merely passes through the interceptor must not carry the session with it.
    const { client, http } = signedIn();

    client.get('https://example.com/data').subscribe();

    expect(http.expectOne('https://example.com/data').request.headers.has('Authorization')).toBe(
      false,
    );
  });

  it('sends nothing when there is no session', () => {
    const { client, http } = configure();

    client.get('/api/providers').subscribe();

    expect(http.expectOne('/api/providers').request.headers.has('Authorization')).toBe(false);
  });

  it('signs the user out when the API rejects the token', async () => {
    const { auth, client, http } = signedIn();

    client.get('/api/providers').subscribe({ error: () => undefined });

    http.expectOne('/api/providers').flush(
      { title: 'Unauthorized', status: 401 },
      { status: 401, statusText: 'Unauthorized' },
    );

    // Staying put would mean every later request failing the same silent way.
    expect(auth.isSignedIn()).toBe(false);
  });
});
