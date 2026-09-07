import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ListQuery, PagedResult } from '../api/models';
import { PagedListState, pagedList } from './paged-list';

interface Row {
  readonly id: number;
}

function page(query: ListQuery): PagedResult<Row> {
  return {
    items: [{ id: 1 }],
    page: query.page ?? 1,
    pageSize: query.pageSize ?? 10,
    totalCount: 42,
    totalPages: 5,
    hasPreviousPage: (query.page ?? 1) > 1,
    hasNextPage: true,
  };
}

/** Builds a list and records every query it sends, so the assertions are about the requests. */
function build(): { list: PagedListState<Row>; queries: ListQuery[] } {
  const queries: ListQuery[] = [];

  const list = TestBed.runInInjectionContext(() =>
    pagedList<Row>((query) => {
      queries.push(query);

      return of(page(query));
    }, { sortBy: 'name' }),
  );

  return { list, queries };
}

/** Lets the signal-to-observable bridge run, which happens on the microtask queue. */
async function settle(): Promise<void> {
  TestBed.tick();
  await Promise.resolve();
  TestBed.tick();
}

describe('pagedList', () => {
  beforeEach(() => TestBed.configureTestingModule({}));
  afterEach(() => TestBed.resetTestingModule());

  it('asks the server for the first page to begin with', async () => {
    const { list, queries } = build();
    await settle();

    expect(queries.at(-1)?.page).toBe(1);
    expect(list.total()).toBe(42);
    expect(list.rows()).toHaveLength(1);
  });

  it('turns the paginator zero-based index into the API one-based page', async () => {
    // Off-by-one here shows the wrong page and nothing says so, which is why it is worth a test.
    const { list, queries } = build();
    await settle();

    list.goToPage(2, 25);
    await settle();

    expect(queries.at(-1)?.page).toBe(3);
    expect(queries.at(-1)?.pageSize).toBe(25);
  });

  it('returns to the first page when the order changes', async () => {
    // Staying on page seven of a differently ordered list shows rows the user never asked for.
    const { list, queries } = build();
    await settle();

    list.goToPage(6, 10);
    await settle();
    list.sortBy('hourlyRate', 'desc');
    await settle();

    expect(queries.at(-1)?.page).toBe(1);
    expect(queries.at(-1)?.sortBy).toBe('hourlyRate');
    expect(queries.at(-1)?.direction).toBe('desc');
  });

  it('reports the page the user is on, zero-based, for the paginator', async () => {
    const { list } = build();
    await settle();

    list.goToPage(3, 10);
    await settle();

    expect(list.pageIndex()).toBe(3);
  });
});
