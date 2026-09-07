import { FormControl, FormGroup, Validators } from '@angular/forms';

import { applyServerErrors, clearServerErrors } from './form-errors';
import { ApiFailure } from './problem-details';

function failure(fieldErrors: Record<string, readonly string[]>): ApiFailure {
  return { status: 400, message: 'Validation failed.', fieldErrors };
}

function buildForm(): FormGroup {
  return new FormGroup({
    nit: new FormControl('', Validators.required),
    email: new FormControl('a@b.co'),
  });
}

describe('applyServerErrors', () => {
  it('puts each message on the control it belongs to', () => {
    // The message belongs under the field that caused it, not in a banner the user has to
    // match up with the inputs by hand.
    const form = buildForm();

    const unmatched = applyServerErrors(
      form,
      failure({ nit: ["The check digit of NIT '890903938-1' is wrong, expected 8."] }),
    );

    expect(form.get('nit')?.getError('server')).toContain('check digit');
    expect(unmatched).toEqual([]);
  });

  it('marks the control as touched so the message is actually shown', () => {
    // Material only renders an error on a touched control, and a field the user never reached
    // would otherwise hold a message nobody sees.
    const form = buildForm();

    applyServerErrors(form, failure({ email: ['Not an e-mail address.'] }));

    expect(form.get('email')?.touched).toBe(true);
  });

  it('returns the messages that belong to no control', () => {
    // A rule spanning fields, or one about a field this form does not have, still has to be
    // said somewhere rather than quietly dropped.
    const form = buildForm();

    const unmatched = applyServerErrors(form, failure({ somethingElse: ['Cannot be done.'] }));

    expect(unmatched).toEqual(['Cannot be done.']);
  });

  it('keeps the errors the form worked out for itself', () => {
    const form = buildForm();
    form.get('nit')?.setValue('');

    applyServerErrors(form, failure({ nit: ['Wrong check digit.'] }));

    expect(form.get('nit')?.hasError('required')).toBe(true);
    expect(form.get('nit')?.hasError('server')).toBe(true);
  });
});

describe('clearServerErrors', () => {
  it('removes the server message once the value changes', () => {
    // A complaint about the value that was rejected must not outlive the correction.
    const form = buildForm();
    form.get('nit')?.setValue('890903938-8');
    applyServerErrors(form, failure({ nit: ['Wrong check digit.'] }));

    clearServerErrors(form);

    expect(form.get('nit')?.hasError('server')).toBe(false);
  });

  it('leaves the client-side rules alone', () => {
    const form = buildForm();
    applyServerErrors(form, failure({ nit: ['Wrong check digit.'] }));

    clearServerErrors(form);

    // The field is still empty, so "required" is still true. Only the server's message went.
    expect(form.get('nit')?.hasError('required')).toBe(true);
    expect(form.get('nit')?.hasError('server')).toBe(false);
  });
});
