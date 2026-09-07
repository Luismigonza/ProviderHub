import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { ProviderDetails, ServiceOffering } from '../../core/api/models';
import { ProvidersApi } from '../../core/api/providers.api';
import { toApiFailure } from '../../core/http/problem-details';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/confirm-dialog';
import { OfferingDialog, OfferingDialogData } from './offering-dialog';
import { ProviderFormDialog } from './provider-form-dialog';

@Component({
  selector: 'app-provider-detail',
  imports: [
    CurrencyPipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './provider-detail.html',
  styleUrl: './provider-detail.scss',
})
export class ProviderDetail {
  /**
   * Bound straight from the route by `withComponentInputBinding`, so this component never has to
   * read `ActivatedRoute`. The transform turns the URL segment into a number once, here, instead
   * of at every use.
   */
  readonly id = input.required({ transform: numberAttribute });

  private readonly api = inject(ProvidersApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly provider = signal<ProviderDetails | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly columns = ['service', 'hourlyRate', 'countries', 'actions'];

  protected readonly offeredServiceIds = computed(
    () => this.provider()?.offerings.map((offering) => offering.serviceId) ?? [],
  );

  constructor() {
    // `id` is an input, so it is read inside an effect-free async load kicked off once the
    // component has its inputs.
    queueMicrotask(() => void this.load());
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      this.provider.set(await firstValueFrom(this.api.get(this.id())));
    } catch (failure) {
      this.error.set(toApiFailure(failure).message);
    } finally {
      this.loading.set(false);
    }
  }

  protected async edit(): Promise<void> {
    const reference = this.dialog.open(ProviderFormDialog, {
      data: { provider: this.provider() ?? undefined },
      width: '34rem',
      maxWidth: '95vw',
    });

    const saved = await firstValueFrom(reference.afterClosed());

    if (saved !== undefined) {
      this.provider.set(saved);
      this.snackBar.open('The provider was updated.', 'Dismiss', { duration: 4000 });
    }
  }

  protected async offerService(offering?: ServiceOffering): Promise<void> {
    const data: OfferingDialogData = {
      providerId: this.id(),
      offering,
      alreadyOffered: this.offeredServiceIds(),
    };

    const reference = this.dialog.open(OfferingDialog, { data, width: '32rem', maxWidth: '95vw' });
    const updated = await firstValueFrom(reference.afterClosed());

    if (updated !== undefined) {
      this.provider.set(updated);
      this.snackBar.open(
        offering === undefined
          ? 'The service was enabled, and the notification e-mail went out.'
          : 'The countries were updated.',
        'Dismiss',
        { duration: 5000 },
      );
    }
  }

  protected async withdraw(offering: ServiceOffering): Promise<void> {
    const data: ConfirmDialogData = {
      title: 'Stop offering this service?',
      message: `"${offering.serviceName}" will no longer be offered by this provider. The catalogue entry itself is not deleted.`,
      confirmLabel: 'Stop offering',
    };

    const reference = this.dialog.open(ConfirmDialog, { data, width: '28rem', maxWidth: '95vw' });

    if ((await firstValueFrom(reference.afterClosed())) !== true) {
      return;
    }

    try {
      await firstValueFrom(this.api.withdrawService(this.id(), offering.serviceId));
      await this.load();
      this.snackBar.open(`"${offering.serviceName}" is no longer offered.`, 'Dismiss', {
        duration: 4000,
      });
    } catch (failure) {
      this.snackBar.open(toApiFailure(failure).message, 'Dismiss', { duration: 6000 });
    }
  }
}
