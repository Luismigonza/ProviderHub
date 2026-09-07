/**
 * TypeScript mirrors of the contracts the API publishes.
 *
 * They are written by hand rather than generated. For a surface this size, hand-written types
 * read better and stay honest about what the frontend actually consumes; the OpenAPI document
 * the backend already serves is the source to generate from the day this grows past that.
 */

/** One page of results, together with everything a pager needs. */
export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
  readonly hasPreviousPage: boolean;
  readonly hasNextPage: boolean;
}

/** The paging, searching and sorting options every list accepts. */
export interface ListQuery {
  readonly page?: number;
  readonly pageSize?: number;
  readonly search?: string;
  readonly sortBy?: string;
  readonly direction?: 'asc' | 'desc';
}

export interface Country {
  readonly code: string;
  readonly name: string;
}

export interface ServiceOffering {
  readonly serviceId: number;
  readonly serviceName: string;
  readonly hourlyRate: number;
  readonly currency: string;
  readonly countries: readonly Country[];
}

export interface ProviderListItem {
  readonly id: number;
  readonly nit: string;
  readonly name: string;
  readonly website: string;
  readonly email: string;
  readonly offeredServiceCount: number;
  readonly countries: readonly Country[];
}

export interface ProviderDetails {
  readonly id: number;
  readonly nit: string;
  readonly name: string;
  readonly website: string;
  readonly email: string;
  readonly offerings: readonly ServiceOffering[];
}

export interface CatalogueService {
  readonly id: number;
  readonly name: string;
  readonly hourlyRate: number;
  readonly currency: string;
}

export interface CountryIndicator {
  readonly countryCode: string;
  readonly countryName: string;
  readonly serviceCount: number;
  readonly providerCount: number;
}

export interface SummaryTotals {
  readonly providerCount: number;
  readonly serviceCount: number;
  readonly offeringCount: number;
  readonly countryCount: number;
}

export interface Summary {
  readonly totals: SummaryTotals;
  readonly byCountry: readonly CountryIndicator[];
}

export interface AccessToken {
  readonly accessToken: string;
  readonly expiresAt: string;
  readonly tokenType: string;
}
