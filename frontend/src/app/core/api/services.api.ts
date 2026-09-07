import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CatalogueService, ListQuery, PagedResult } from './models';
import { toHttpParams } from './providers.api';

export interface SaveService {
  readonly name: string;
  readonly hourlyRate: number;
}

/** Every call the service catalogue makes. */
@Injectable({ providedIn: 'root' })
export class ServicesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/services';

  list(query: ListQuery): Observable<PagedResult<CatalogueService>> {
    return this.http.get<PagedResult<CatalogueService>>(this.baseUrl, {
      params: toHttpParams(query),
    });
  }

  get(id: number): Observable<CatalogueService> {
    return this.http.get<CatalogueService>(`${this.baseUrl}/${id}`);
  }

  create(service: SaveService): Observable<CatalogueService> {
    return this.http.post<CatalogueService>(this.baseUrl, service);
  }

  update(id: number, service: SaveService): Observable<CatalogueService> {
    return this.http.put<CatalogueService>(`${this.baseUrl}/${id}`, service);
  }
}
