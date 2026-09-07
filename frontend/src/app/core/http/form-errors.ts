import { FormGroup } from '@angular/forms';

import { ApiFailure } from './problem-details';

/**
 * Moves the errors the API reported onto the form controls they belong to.
 *
 * The backend answers a rejected form with one entry per offending field. Showing all of that in
 * a single banner would put the message far from the input that caused it and leave the user
 * matching them up by hand. Attaching each message to its control puts it under the right field,
 * where a form error belongs.
 *
 * @returns the messages that had no matching control, for showing above the form. A rule that
 * spans fields, or a field the form does not have, still has to be said somewhere.
 */
export function applyServerErrors(form: FormGroup, failure: ApiFailure): readonly string[] {
  const unmatched: string[] = [];

  for (const [field, messages] of Object.entries(failure.fieldErrors)) {
    const control = form.get(field);

    if (control === null) {
      unmatched.push(...messages);

      continue;
    }

    control.setErrors({ ...(control.errors ?? {}), server: messages.join(' ') });

    // Material only shows an error once the control has been touched, and a control the user
    // never reached would otherwise hold a message nobody sees.
    control.markAsTouched();
  }

  return unmatched;
}

/**
 * Clears the errors the server set, leaving the ones the form works out for itself.
 *
 * Called as the user edits: a message about the value that was rejected should not survive the
 * correction, and re-running the client-side validators is what decides whether the field is
 * valid now.
 */
export function clearServerErrors(form: FormGroup): void {
  for (const control of Object.values(form.controls)) {
    if (control.errors?.['server'] !== undefined) {
      const { server: _removed, ...rest } = control.errors;

      control.setErrors(Object.keys(rest).length > 0 ? rest : null);
      control.updateValueAndValidity({ emitEvent: false });
    }
  }
}
