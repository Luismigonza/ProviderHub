import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { catchError, map, of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { toApiFailure } from '../../core/http/problem-details';

interface CurrentUser {
  readonly userName: string | null;
}

/**
 * The three states this screen can be in, spelled out as a union.
 *
 * Written explicitly rather than inferred: TypeScript widens each branch of `map` and
 * `catchError` on its own and cannot see that they belong to the same union, so the annotation
 * is what makes the template's `@switch` exhaustive and type-safe.
 */
type HomeState =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly userName: string | null }
  | { readonly status: 'failed'; readonly message: string };

/**
 * A placeholder landing page for the signed-in area.
 *
 * It earns its place by proving the whole chain end to end: the token was stored, the
 * interceptor attached it, the guard let the route through, and the API accepted the call. The
 * lists and the dashboard replace it in the next steps.
 */
@Component({
  selector: 'app-home',
  imports: [MatCardModule, MatProgressBarModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly http = inject(HttpClient);
  protected readonly auth = inject(AuthService);

  /**
   * `toSignal` turns the response into a signal, so the template reads a value instead of piping
   * through `async` and the subscription is torn down with the component.
   */
  protected readonly whoAmI = toSignal(
    this.http.get<CurrentUser>('/api/auth/me').pipe(
      map((user): HomeState => ({ status: 'ready', userName: user.userName })),
      catchError((failure: unknown) =>
        of<HomeState>({ status: 'failed', message: toApiFailure(failure).message }),
      ),
    ),
    { initialValue: { status: 'loading' } as HomeState },
  );
}
