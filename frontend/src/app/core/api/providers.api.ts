import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ListQuery, PagedResult, ProviderDetails, ProviderListItem } from './models';

/** The body both create and update accept. */
export interface SaveProvider {
  readonly nit: string;
  readonly name: string;
  readonly website: string;
  readonly email: string;
}

/**
 * Turns list options into query parameters, leaving out anything the caller did not set.
 *
 * Sending `search=` empty would ask the API to filter on nothing, and `sortBy=undefined` would be
 * rejected as an unknown field. Omitting them lets the server apply its own defaults.
 */
export function toHttpParams(query: ListQuery): HttpParams {
  let params = new HttpParams();

  if (query.page !== undefined) {
    params = params.set('page', query.page);
  }

  if (query.pageSize !== undefined) {
    params = params.set('pageSize', query.pageSize);
  }

  if (query.search !== undefined && query.search.trim() !== '') {
    params = params.set('search', query.search.trim());
  }

  if (query.sortBy !== undefined && query.sortBy !== '') {
    params = params.set('sortBy', query.sortBy);
    params = params.set('direction', query.direction ?? 'asc');
  }

  return params;
}

/** Every call the providers area makes, in one place. */
@Injectable({ providedIn: 'root' })
export class ProvidersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/providers';

  list(query: ListQuery): Observable<PagedResult<ProviderListItem>> {
    return this.http.get<PagedResult<ProviderListItem>>(this.baseUrl, {
      params: toHttpParams(query),
    });
  }

  get(id: number): Observable<ProviderDetails> {
    return this.http.get<ProviderDetails>(`${this.baseUrl}/${id}`);
  }

  create(provider: SaveProvider): Observable<ProviderDetails> {
    return this.http.post<ProviderDetails>(this.baseUrl, provider);
  }

  update(id: number, provider: SaveProvider): Observable<ProviderDetails> {
    return this.http.put<ProviderDetails>(`${this.baseUrl}/${id}`, provider);
  }

  /** Enables a catalogue service for this provider in a set of countries. */
  offerService(id: number, serviceId: number, countries: readonly string[]): Observable<ProviderDetails> {
    return this.http.post<ProviderDetails>(`${this.baseUrl}/${id}/services`, { serviceId, countries });
  }

  changeCountries(
    id: number,
    serviceId: number,
    countries: readonly string[],
  ): Observable<ProviderDetails> {
    return this.http.put<ProviderDetails>(`${this.baseUrl}/${id}/services/${serviceId}`, { countries });
  }

  withdrawService(id: number, serviceId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/services/${serviceId}`);
  }

  /** Which columns this list can be sorted by, so the table does not hard-code the server's rules. */
  sortFields(): Observable<readonly string[]> {
    return this.http.get<readonly string[]>(`${this.baseUrl}/sort-fields`);
  }
}
