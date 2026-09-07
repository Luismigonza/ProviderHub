import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom } from 'rxjs';

import { ProviderDetails, ProviderListItem } from '../../core/api/models';
import { ProvidersApi } from '../../core/api/providers.api';
import { applyServerErrors, clearServerErrors } from '../../core/http/form-errors';
import { toApiFailure } from '../../core/http/problem-details';

export interface ProviderFormData {
  readonly provider?: ProviderListItem | ProviderDetails;
}

@Component({
  selector: 'app-provider-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './provider-form-dialog.html',
})
export class ProviderFormDialog {
  private readonly api = inject(ProvidersApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialog =
    inject<MatDialogRef<ProviderFormDialog, ProviderDetails>>(MatDialogRef);

  protected readonly data = inject<ProviderFormData>(MAT_DIALOG_DATA);
  protected readonly isEdit = this.data.provider !== undefined;

  protected readonly form = this.formBuilder.nonNullable.group({
    // No check-digit arithmetic here. That rule lives in the domain, on the server, and copying
    // it into the browser would create a second place for it to be wrong. The client checks the
    // shape; the server checks the rule and sends back its own message.
    nit: [this.data.provider?.nit ?? '', [Validators.required, Validators.pattern(/^[\d.\s]+-?\d*$/)]],
    name: [this.data.provider?.name ?? '', [Validators.required, Validators.maxLength(200)]],
    website: [this.data.provider?.website ?? '', [Validators.required]],
    email: [this.data.provider?.email ?? '', [Validators.required, Validators.email]],
  });

  protected readonly busy = signal(false);
  protected readonly generalErrors = signal<readonly string[]>([]);

  constructor() {
    this.form.valueChanges.subscribe(() => clearServerErrors(this.form));
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();

      return;
    }

    this.busy.set(true);
    this.generalErrors.set([]);

    const body = this.form.getRawValue();

    try {
      const saved = await firstValueFrom(
        this.isEdit ? this.api.update(this.data.provider!.id, body) : this.api.create(body),
      );

      this.dialog.close(saved);
    } catch (failure) {
      const apiFailure = toApiFailure(failure);
      const unmatched = applyServerErrors(this.form, apiFailure);

      this.generalErrors.set(
        unmatched.length > 0
          ? unmatched
          : Object.keys(apiFailure.fieldErrors).length > 0
            ? []
            : [apiFailure.message],
      );
    } finally {
      this.busy.set(false);
    }
  }
}
