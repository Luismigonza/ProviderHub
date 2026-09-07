import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { ProviderListItem } from '../../core/api/models';
import { ProvidersApi } from '../../core/api/providers.api';
import { pagedList } from '../../core/lists/paged-list';
import { ProviderFormData, ProviderFormDialog } from './provider-form-dialog';

@Component({
  selector: 'app-provider-list',
  imports: [
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './provider-list.html',
  styleUrl: './provider-list.scss',
})
export class ProviderList {
  private readonly api = inject(ProvidersApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly list = pagedList((query) => this.api.list(query), { sortBy: 'name' });

  protected readonly columns = ['name', 'nit', 'email', 'services', 'countries', 'actions'];

  protected onPage(event: PageEvent): void {
    this.list.goToPage(event.pageIndex, event.pageSize);
  }

  protected onSort(sort: Sort): void {
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
      this.snackBar.open(`"${saved.name}" was registered.`, 'Dismiss', { duration: 4000 });
    }
  }

  protected async edit(provider: ProviderListItem): Promise<void> {
    const saved = await this.open({ provider });

    if (saved !== undefined) {
      this.list.reload();
      this.snackBar.open(`"${saved.name}" was updated.`, 'Dismiss', { duration: 4000 });
    }
  }

  private async open(data: ProviderFormData) {
    const reference = this.dialog.open(ProviderFormDialog, {
      data,
      width: '34rem',
      maxWidth: '95vw',
    });

    return firstValueFrom(reference.afterClosed());
  }
}
