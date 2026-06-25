import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SchoolService } from '../../../core/services/school.service';
import { SchoolSummary } from '../../../core/models/school.models';

@Component({
  selector: 'app-schools-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './schools-list.html',
  styleUrl: './schools-list.scss'
})
export class SchoolsListComponent implements OnInit {
  private readonly schoolService = inject(SchoolService);

  readonly schools = signal<SchoolSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit() {
    this.schoolService.getAll().subscribe({
      next: data => { this.schools.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load schools.'); this.loading.set(false); }
    });
  }
}
