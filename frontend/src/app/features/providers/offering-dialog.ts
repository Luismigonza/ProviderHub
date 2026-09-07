import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { catchError, firstValueFrom, map, of } from 'rxjs';

import { COUNTRY_OPTIONS } from '../../core/api/countries';
import { CatalogueService, ProviderDetails, ServiceOffering } from '../../core/api/models';
import { ProvidersApi } from '../../core/api/providers.api';
import { ServicesApi } from '../../core/api/services.api';
import { applyServerErrors, clearServerErrors } from '../../core/http/form-errors';
import { toApiFailure } from '../../core/http/problem-details';

export interface OfferingDialogData {
  readonly providerId: number;

  /** Present when changing where an existing offering is available. */
  readonly offering?: ServiceOffering;

  /** Services this provider already offers, so the picker cannot suggest a duplicate. */
  readonly alreadyOffered: readonly number[];
}

@Component({
  selector: 'app-offering-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  templateUrl: './offering-dialog.html',
})
export class OfferingDialog {
  private readonly providers = inject(ProvidersApi);
  private readonly services = inject(ServicesApi);
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialog = inject<MatDialogRef<OfferingDialog, ProviderDetails>>(MatDialogRef);

  protected readonly data = inject<OfferingDialogData>(MAT_DIALOG_DATA);
  protected readonly isEdit = this.data.offering !== undefined;
  protected readonly countries = COUNTRY_OPTIONS;

  /**
   * The catalogue, minus what this provider already offers.
   *
   * Offering the same service twice is a 409 the server would refuse anyway. Leaving those
   * options out means the user cannot walk into an error the interface already knows about.
   */
  protected readonly catalogue = toSignal(
    this.services.list({ pageSize: 100, sortBy: 'name' }).pipe(
      map((page) =>
        page.items.filter((service) => !this.data.alreadyOffered.includes(service.id)),
      ),
      catchError(() => of<readonly CatalogueService[]>([])),
    ),
    { initialValue: [] as readonly CatalogueService[] },
  );

  protected readonly form = this.formBuilder.nonNullable.group({
    serviceId: [this.data.offering?.serviceId ?? 0, [Validators.required, Validators.min(1)]],
    countries: [
      this.data.offering?.countries.map((country) => country.code) ?? ([] as string[]),
      [Validators.required, Validators.minLength(1)],
    ],
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

    const { serviceId, countries } = this.form.getRawValue();

    try {
      const updated = await firstValueFrom(
        this.isEdit
          ? this.providers.changeCountries(this.data.providerId, serviceId, countries)
          : this.providers.offerService(this.data.providerId, serviceId, countries),
      );

      this.dialog.close(updated);
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
