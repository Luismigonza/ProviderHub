import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom } from 'rxjs';

import { CatalogueService } from '../../core/api/models';
import { ServicesApi } from '../../core/api/services.api';
import { applyServerErrors, clearServerErrors } from '../../core/http/form-errors';
import { toApiFailure } from '../../core/http/problem-details';

/** Passed in by whoever opens the dialog. Absent means "new service". */
export interface ServiceFormData {
  readonly service?: CatalogueService;
}

@Component({
  selector: 'app-service-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './service-form-dialog.html',
})
export class ServiceFormDialog {
  private readonly api = inject(ServicesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialog = inject<MatDialogRef<ServiceFormDialog, CatalogueService>>(MatDialogRef);

  protected readonly data = inject<ServiceFormData>(MAT_DIALOG_DATA);
  protected readonly isEdit = this.data.service !== undefined;

  protected readonly form = this.formBuilder.nonNullable.group({
    // The client-side rules mirror the server's, so the obvious mistakes are caught without a
    // round trip. The server still validates: these are for speed, not for safety.
    name: [this.data.service?.name ?? '', [Validators.required, Validators.maxLength(200)]],
    hourlyRate: [this.data.service?.hourlyRate ?? 0, [Validators.required, Validators.min(0)]],
  });

  protected readonly busy = signal(false);
  protected readonly generalErrors = signal<readonly string[]>([]);

  constructor() {
    // A message about the value the server rejected must not outlive the correction.
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
        this.isEdit ? this.api.update(this.data.service!.id, body) : this.api.create(body),
      );

      this.dialog.close(saved);
    } catch (failure) {
      const apiFailure = toApiFailure(failure);
      const unmatched = applyServerErrors(this.form, apiFailure);

      // A 409 has no field errors at all: a duplicated name is a fact about the catalogue, not
      // about the shape of the value, so it is shown above the form.
      this.generalErrors.set(
        unmatched.length > 0 ? unmatched : Object.keys(apiFailure.fieldErrors).length > 0 ? [] : [apiFailure.message],
      );
    } finally {
      this.busy.set(false);
    }
  }
}
