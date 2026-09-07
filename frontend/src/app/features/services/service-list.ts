import { CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { firstValueFrom } from 'rxjs';

import { CatalogueService } from '../../core/api/models';
import { ServicesApi } from '../../core/api/services.api';
import { pagedList } from '../../core/lists/paged-list';
import { ServiceFormData, ServiceFormDialog } from './service-form-dialog';

@Component({
  selector: 'app-service-list',
  imports: [
    CurrencyPipe,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSortModule,
    MatTableModule,
  ],
  templateUrl: './service-list.html',
  styleUrl: './service-list.scss',
})
export class ServiceList {
  private readonly api = inject(ServicesApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly list = pagedList((query) => this.api.list(query), { sortBy: 'name' });

  /** The column ids, matching the sort fields the API accepts. */
  protected readonly columns = ['name', 'hourlyRate', 'actions'];

  protected onPage(event: PageEvent): void {
    this.list.goToPage(event.pageIndex, event.pageSize);
  }

  protected onSort(sort: Sort): void {
    // Clearing the sort in the header sends an empty direction; the list then falls back to the
    // server's default rather than asking for an order that does not exist.
    this.list.sortBy(
      sort.direction === '' ? undefined : sort.active,
      sort.direction === 'desc' ? 'desc' : 'asc',
    );
  }

  protected onSearch(term: string): void {
    this.list.search(term);
  }

  protected async create(): Promise<void> {
    const saved = await this.open({});

    if (saved !== undefined) {
      this.list.reload();
      this.snackBar.open(`"${saved.name}" was added to the catalogue.`, 'Dismiss', {
        duration: 4000,
      });
    }
  }

  protected async edit(service: CatalogueService): Promise<void> {
    const saved = await this.open({ service });

    if (saved !== undefined) {
      this.list.reload();
      this.snackBar.open(`"${saved.name}" was updated.`, 'Dismiss', { duration: 4000 });
    }
  }

  private async open(data: ServiceFormData): Promise<CatalogueService | undefined> {
    const reference = this.dialog.open<ServiceFormDialog, ServiceFormData, CatalogueService>(
      ServiceFormDialog,
      { data, width: '32rem', maxWidth: '95vw' },
    );

    return firstValueFrom(reference.afterClosed());
  }
}
