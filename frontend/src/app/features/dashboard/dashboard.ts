import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { CountryIndicator, Summary } from '../../core/api/models';
import { SummaryApi } from '../../core/api/summary.api';
import { toApiFailure } from '../../core/http/problem-details';

/** A country row with the widths its bars should have, worked out once rather than in the template. */
export interface CountryRow extends CountryIndicator {
  readonly servicesWidth: number;
  readonly providersWidth: number;
}

/**
 * Scales each row against the largest value in its own column.
 *
 * Against a fixed maximum the bars would be unreadable whenever the data is small, and against
 * the sum they would all be slivers. Relative to the leader, the shape of the distribution is
 * the thing the eye picks up, which is what the indicator is for.
 */
export function toRows(indicators: readonly CountryIndicator[]): readonly CountryRow[] {
  const maxServices = Math.max(1, ...indicators.map((row) => row.serviceCount));
  const maxProviders = Math.max(1, ...indicators.map((row) => row.providerCount));

  return indicators.map((row) => ({
    ...row,
    servicesWidth: Math.round((row.serviceCount / maxServices) * 100),
    providersWidth: Math.round((row.providerCount / maxProviders) * 100),
  }));
}

@Component({
  selector: 'app-dashboard',
  imports: [
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly api = inject(SummaryApi);

  protected readonly summary = signal<Summary | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly columns = ['country', 'services', 'providers'];

  protected readonly rows = computed(() => toRows(this.summary()?.byCountry ?? []));

  constructor() {
    void this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      // One request for the whole screen: the server does the grouping and the counting, so
      // this never grows into "fetch everything and add it up in the browser".
      this.summary.set(await firstValueFrom(this.api.get()));
    } catch (failure) {
      this.error.set(toApiFailure(failure).message);
    } finally {
      this.loading.set(false);
    }
  }
}
