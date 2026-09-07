import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';

describe('App shell', () => {
  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([{ path: '**', children: [] }])],
    }).compileComponents();
  });

  it('hides the toolbar while nobody is signed in', async () => {
    // A sign-out button with nothing to sign out of is worse than no toolbar at all.
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('mat-toolbar')).toBeNull();
  });

  it('shows who is signed in once there is a session', async () => {
    localStorage.setItem(
      'providerhub.session',
      JSON.stringify({
        accessToken: 'a-token',
        expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
        userName: 'admin',
      }),
    );

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const toolbar = (fixture.nativeElement as HTMLElement).querySelector('mat-toolbar');
    expect(toolbar?.textContent).toContain('admin');
  });
});
