import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SchoolService } from '../../../core/services/school.service';
import { SchoolDetail } from '../../../core/models/school.models';

@Component({
  selector: 'app-school-detail',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './school-detail.html',
  styleUrl: './school-detail.scss'
})
export class SchoolDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly schoolService = inject(SchoolService);

  readonly school = signal<SchoolDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.schoolService.getById(id).subscribe({
      next: data => { this.school.set(data); this.loading.set(false); },
      error: () => { this.error.set('School not found.'); this.loading.set(false); }
    });
  }
}
