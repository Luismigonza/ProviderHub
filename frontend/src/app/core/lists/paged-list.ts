import { Signal, computed, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  Observable,
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  scan,
  startWith,
  switchMap,
} from 'rxjs';

import { ListQuery, PagedResult } from '../api/models';
import { toApiFailure } from '../http/problem-details';

/** What a list screen needs to render itself. */
export interface PagedListState<T> {
  readonly rows: Signal<readonly T[]>;
  readonly total: Signal<number>;
  readonly pageIndex: Signal<number>;
  readonly pageSize: Signal<number>;
  readonly loading: Signal<boolean>;
  readonly error: Signal<string | null>;

  /** Moves to a page. Indexes are zero-based here, matching `mat-paginator`. */
  goToPage(pageIndex: number, pageSize: number): void;

  sortBy(field: string | undefined, direction: 'asc' | 'desc'): void;

  search(term: string): void;

  /** Re-runs the current query, after a create or an edit. */
  reload(): void;
}

interface Loaded<T> {
  readonly page: PagedResult<T> | null;
  readonly loading: boolean;
  readonly error: string | null;
}

/**
 * The paging, searching and sorting machinery both list screens share.
 *
 * All three happen on the server, which is the only way they can be correct: sorting a page of
 * twenty rows in the browser sorts twenty rows, not the ten thousand behind them. What this
 * function does is turn user gestures into query parameters and the answers into signals.
 *
 * It exists because the alternative was writing the same forty lines in the providers list and
 * again in the services list, where the second copy drifts from the first the moment one of them
 * gains a feature.
 *
 * Must be called from an injection context, such as a component field initializer.
 */
export function pagedList<T>(
  fetch: (query: ListQuery) => Observable<PagedResult<T>>,
  options: { readonly pageSize?: number; readonly sortBy?: string } = {},
): PagedListState<T> {
  const query = signal<ListQuery>({
    page: 1,
    pageSize: options.pageSize ?? 10,
    sortBy: options.sortBy,
    direction: 'asc',
    search: '',
  });

  // Bumped to re-run the same query, which a plain signal would ignore because the value is
  // unchanged.
  const reloads = signal(0);

  const searchTerm = computed(() => query().search ?? '');

  // Everything except the search term applies immediately; typing waits. Without the delay every
  // keystroke would be a round trip, and the answers could arrive out of order.
  const debouncedSearch = toObservable(searchTerm).pipe(
    debounceTime(300),
    distinctUntilChanged(),
    startWith(''),
  );

  const withoutSearch = toObservable(computed(() => ({ ...query(), search: undefined })));

  const state = toSignal(
    combineLatest([withoutSearch, debouncedSearch, toObservable(reloads)]).pipe(
      // switchMap cancels the request in flight, so a slow page one can never overwrite the
      // page two the user has already moved on to.
      switchMap(([options_, search]) =>
        fetch({ ...options_, search }).pipe(
          map((page): Loaded<T> => ({ page, loading: false, error: null })),
          catchError((failure: unknown) =>
            of<Loaded<T>>({ page: null, loading: false, error: toApiFailure(failure).message }),
          ),

          // Emitted before the request goes out, which is what turns the progress bar on.
          startWith<Loaded<T>>({ page: null, loading: true, error: null }),
        ),
      ),

      // Carry the page already on screen through the next load, so the table shows the previous
      // rows under a progress bar instead of blinking empty and jumping back when the answer
      // arrives. Kept in the stream rather than in a signal: writing to a signal from inside a
      // computed is forbidden, and Angular is right to forbid it.
      scan(
        (previous: Loaded<T>, next: Loaded<T>): Loaded<T> =>
          next.loading && next.page === null ? { ...next, page: previous.page } : next,
        { page: null, loading: true, error: null } as Loaded<T>,
      ),
    ),
    { initialValue: { page: null, loading: true, error: null } as Loaded<T> },
  );

  return {
    rows: computed(() => state().page?.items ?? []),
    total: computed(() => state().page?.totalCount ?? 0),
    pageIndex: computed(() => (query().page ?? 1) - 1),
    pageSize: computed(() => query().pageSize ?? 10),
    loading: computed(() => state().loading),
    error: computed(() => state().error),

    goToPage: (pageIndex, pageSize) =>
      query.update((current) => ({ ...current, page: pageIndex + 1, pageSize })),

    sortBy: (field, direction) =>
      // Back to the first page: staying on page seven of a differently ordered list shows rows
      // the user never asked to see.
      query.update((current) => ({ ...current, sortBy: field, direction, page: 1 })),

    search: (term) => query.update((current) => ({ ...current, search: term, page: 1 })),

    reload: () => reloads.update((count) => count + 1),
  };
}
