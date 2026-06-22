import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LessonService } from '../../../core/services/lesson.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-book-lesson',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './book-lesson.html',
  styleUrl: './book-lesson.scss'
})
export class BookLessonComponent {
  private readonly fb = inject(FormBuilder);
  private readonly lessonService = inject(LessonService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal(false);

  readonly enrollmentId = localStorage.getItem('de_enrollment_id') ?? '';
  readonly hasEnrollment = !!this.enrollmentId;

  readonly form = this.fb.group({
    instructorId: ['', Validators.required],
    scheduledAt: ['', Validators.required],
    durationHours: ['1', Validators.required]
  });

  readonly durationOptions = [
    { value: '00:30:00', label: '30 minutes' },
    { value: '01:00:00', label: '1 hour' },
    { value: '01:30:00', label: '1.5 hours' },
    { value: '02:00:00', label: '2 hours' }
  ];

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading.set(true);
    this.error.set(null);

    const v = this.form.value;
    const scheduledAt = new Date(v.scheduledAt!).toISOString();

    this.lessonService.book({
      enrollmentId: this.enrollmentId,
      studentId: this.auth.studentId()!,
      instructorId: v.instructorId!,
      scheduledAt,
      duration: v.durationHours!
    }).subscribe({
      next: () => {
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/lessons']), 2000);
      },
      error: (err) => {
        this.error.set(err?.error?.detail ?? 'Booking failed. Please try again.');
        this.loading.set(false);
      }
    });
  }
}
