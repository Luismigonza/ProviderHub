import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

export interface ConfirmDialogData {
  readonly title: string;
  readonly message: string;
  readonly confirmLabel: string;
}

/**
 * Asks before something that cannot be undone with a click.
 *
 * Small enough to inline, kept apart because "are you sure" copied into three screens is three
 * places for the wording, the button order and the accessibility to drift.
 */
@Component({
  selector: 'app-confirm-dialog',
  imports: [MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <!-- Cancel first and focused: the safe option should be the easy one to hit. -->
      <button matButton [mat-dialog-close]="false" type="button" cdkFocusInitial>Cancel</button>
      <button matButton="filled" [mat-dialog-close]="true" type="button">
        {{ data.confirmLabel }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
