import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

function strongPassword(control: AbstractControl): ValidationErrors | null {
  const val: string = control.value ?? '';
  const errors: string[] = [];
  if (val.length < 8)               errors.push('8 characters');
  if ((val.match(/[0-9]/g) ?? []).length < 2)  errors.push('2 numbers');
  if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(val)) errors.push('1 special character');
  return errors.length ? { strongPassword: errors } : null;
}
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly showPassword = signal(false);

  readonly form = this.fb.group({
    fullName:    ['', [Validators.required, Validators.maxLength(200)]],
    email:       ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    phoneNumber: ['', [Validators.pattern(/^\+?[0-9\s\-]{7,30}$/)]],
    dobDay:      ['', Validators.required],
    dobMonth:    ['', Validators.required],
    dobYear:     ['', Validators.required],
    password:    ['', [Validators.required, Validators.maxLength(100), strongPassword]]
  });

  readonly months = [
    'January','February','March','April','May','June',
    'July','August','September','October','November','December'
  ];

  readonly days   = Array.from({ length: 31 }, (_, i) => i + 1);
  readonly years  = Array.from({ length: 41 }, (_, i) => 2005 - i);

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading.set(true);
    this.error.set(null);

    const v = this.form.value;
    const month = String(this.months.indexOf(v.dobMonth!) + 1).padStart(2, '0');
    const day   = String(v.dobDay!).padStart(2, '0');
    const dateOfBirth = `${v.dobYear}-${month}-${day}`;

    this.auth.register({
      fullName:    v.fullName!,
      email:       v.email!,
      phoneNumber: v.phoneNumber ?? '',
      dateOfBirth,
      password:    v.password!
    }).subscribe({
      next: () => this.router.navigate(['/login']),
      error: (err) => {
        const detail = err?.error?.detail ?? 'Registration failed. Please try again.';
        this.error.set(detail);
        this.loading.set(false);
      }
    });
  }
}
