import { HttpErrorResponse } from '@angular/common/http';

import { flattenFieldErrors, toApiFailure } from './problem-details';

describe('toApiFailure', () => {
  it('keeps every field error the API reported', () => {
    // The backend answers with all the offending fields at once. Collapsing that into one
    // message would waste the work it already did and make the user fix them one per attempt.
    const failure = toApiFailure(
      new HttpErrorResponse({
        status: 400,
        error: {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: {
            nit: ["The check digit of NIT '890903938-1' is wrong, expected 8."],
            email: ["'nope' is not a valid e-mail address."],
          },
        },
      }),
    );

    expect(failure.status).toBe(400);
    expect(Object.keys(failure.fieldErrors)).toEqual(['nit', 'email']);
    expect(flattenFieldErrors(failure)).toHaveLength(2);
  });

  it('prefers the detail the server wrote over a generic sentence', () => {
    const failure = toApiFailure(
      new HttpErrorResponse({
        status: 409,
        error: { title: 'Conflict', detail: "NIT '890903938-8' is already registered." },
      }),
    );

    expect(failure.message).toBe("NIT '890903938-8' is already registered.");
  });

  it('falls back to a sentence for the status when the body says nothing', () => {
    const failure = toApiFailure(new HttpErrorResponse({ status: 404, error: null }));

    expect(failure.message).toContain('no longer exists');
  });

  it('tells a request that never arrived apart from one the server rejected', () => {
    // Status 0 means the API was unreachable. "Something went wrong" would send the user
    // looking for a mistake in their data instead of at a server that is not running.
    const failure = toApiFailure(new HttpErrorResponse({ status: 0 }));

    expect(failure.message).toContain('could not be reached');
  });

  it('survives something that is not an HTTP error at all', () => {
    const failure = toApiFailure(new Error('boom'));

    expect(failure.status).toBe(0);
    expect(failure.fieldErrors).toEqual({});
  });
});
