import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { toApiFailure } from '../../../core/http/problem-details';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly form = this.formBuilder.nonNullable.group({
    userName: ['', Validators.required],
    password: ['', Validators.required],
  });

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly hidePassword = signal(true);

  protected async submit(): Promise<void> {
    // A form can be submitted with Enter before anything is typed.
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();

      return;
    }

    this.busy.set(true);
    this.error.set(null);

    const { userName, password } = this.form.getRawValue();

    try {
      await this.auth.signIn(userName, password);

      // Back to wherever the guard interrupted, or to the start.
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
      await this.router.navigateByUrl(returnUrl);
    } catch (failure) {
      // The API answers a wrong username and a wrong password identically, and so does this
      // screen: telling them apart would help somebody find out which accounts exist.
      this.error.set(toApiFailure(failure).message);
    } finally {
      this.busy.set(false);
    }
  }

  protected togglePassword(): void {
    this.hidePassword.update((hidden) => !hidden);
  }
}
