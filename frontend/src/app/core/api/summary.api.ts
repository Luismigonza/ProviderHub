import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Summary } from './models';

/** The dashboard figures. */
@Injectable({ providedIn: 'root' })
export class SummaryApi {
  private readonly http = inject(HttpClient);

  get(): Observable<Summary> {
    return this.http.get<Summary>('/api/summary');
  }
}
