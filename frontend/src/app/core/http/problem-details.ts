import { HttpErrorResponse } from '@angular/common/http';

/**
 * The error body the API returns for every failure (RFC 9457). Because the backend answers in
 * one shape whichever endpoint failed, the frontend needs one place that understands it.
 */
export interface ProblemDetails {
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;

  /** Present on validation failures: one entry per field, each with its messages. */
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

/** A failure translated into something a screen can show. */
export interface ApiFailure {
  readonly status: number;

  /** One sentence to put in front of the user. */
  readonly message: string;

  /** Field name to messages, ready to attach to form controls. */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
}

const NETWORK_FAILURE =
  'The server could not be reached. Check that the API is running.';

const UNEXPECTED_FAILURE = 'Something went wrong. Please try again.';

/**
 * Turns any HTTP failure into an {@link ApiFailure}.
 *
 * The API is generous with its errors: a rejected form comes back with every offending field at
 * once, so the user fixes them together instead of one per round trip. Throwing that away and
 * showing "invalid data" would waste the work the backend already did.
 */
export function toApiFailure(error: unknown): ApiFailure {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, message: UNEXPECTED_FAILURE, fieldErrors: {} };
  }

  // Status 0 means the request never reached a server: wrong port, API down, blocked by the
  // browser. It deserves a different sentence from "the server said no".
  if (error.status === 0) {
    return { status: 0, message: NETWORK_FAILURE, fieldErrors: {} };
  }

  const problem = (error.error ?? {}) as ProblemDetails;
  const fieldErrors = problem.errors ?? {};

  return {
    status: error.status,
    message: problem.detail ?? problem.title ?? defaultMessageFor(error.status),
    fieldErrors,
  };
}

/** Flattens the field errors into a list, for showing next to a form rather than inside it. */
export function flattenFieldErrors(failure: ApiFailure): readonly string[] {
  return Object.values(failure.fieldErrors).flat();
}

function defaultMessageFor(status: number): string {
  switch (status) {
    case 401:
      return 'Your session is not valid. Please sign in again.';
    case 403:
      return 'You are not allowed to do that.';
    case 404:
      return 'That resource no longer exists.';
    case 409:
      return 'That change conflicts with the current data.';
    default:
      return UNEXPECTED_FAILURE;
  }
}
