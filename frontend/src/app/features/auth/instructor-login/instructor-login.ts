import { Component, computed, HostListener, inject, signal, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

interface DemoInstructor {
  name: string;
  email: string;
  license: string;
  password: string;
}

interface DemoSchool {
  name: string;
  instructors: DemoInstructor[];
}

const DEMO_SCHOOLS: DemoSchool[] = [
  {
    name: 'Mumbai Central Driving Academy',
    instructors: [
      { name: 'Amit Sharma',  email: 'amit.sharma@mumbaicentral.com',  license: 'MH-01AB1001', password: 'Instructor@123' },
      { name: 'Priya Patel',  email: 'priya.patel@mumbaicentral.com',  license: 'MH-01AB1002', password: 'Instructor@123' },
      { name: 'Ravi Nair',    email: 'ravi.nair@mumbaicentral.com',    license: 'MH-01AB1003', password: 'Instructor@123' },
    ]
  },
  {
    name: 'Pune Road Masters',
    instructors: [
      { name: 'Suresh Jadhav',  email: 'suresh.jadhav@puneroadmasters.com', license: 'MH-12CD2001', password: 'Instructor@123' },
      { name: 'Neha Kulkarni',  email: 'neha.kulkarni@puneroadmasters.com', license: 'MH-12CD2002', password: 'Instructor@123' },
      { name: 'Vijay Desai',    email: 'vijay.desai@puneroadmasters.com',   license: 'MH-12CD2003', password: 'Instructor@123' },
    ]
  },
  {
    name: 'Sunrise Driving Academy',
    instructors: [
      { name: 'Mohan Singh',   email: 'mohan.singh@sunrise.driveease.com',   license: 'MH-02EF3001', password: 'Instructor@123' },
      { name: 'Kavita Rao',    email: 'kavita.rao@sunrise.driveease.com',    license: 'MH-02EF3002', password: 'Instructor@123' },
      { name: 'Deepak Thakur', email: 'deepak.thakur@sunrise.driveease.com', license: 'MH-02EF3003', password: 'Instructor@123' },
    ]
  },
  {
    name: 'Thinkschool Safe Drive Institute',
    instructors: [
      { name: 'Rahul Sharma', email: 'rahul.sharma@thinkschool.com', license: 'MH-20GH4001', password: 'Instructor@123' },
      { name: 'Anita Joshi',  email: 'anita.joshi@thinkschool.com',  license: 'MH-20GH4002', password: 'Instructor@123' },
      { name: 'Sanjay More',  email: 'sanjay.more@thinkschool.com',  license: 'MH-20GH4003', password: 'Instructor@123' },
    ]
  },
];

@Component({
  selector: 'app-instructor-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './instructor-login.html',
  styleUrl: './instructor-login.scss'
})
export class InstructorLoginComponent implements AfterViewInit, OnDestroy {
  @ViewChild('roadCanvas') private readonly canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly fb     = inject(FormBuilder);
  private readonly router = inject(Router);

  // ── Existing auth signals ──────────────────────────────────────────────────
  readonly loading = signal(false);
  readonly error   = signal<string | null>(null);

  readonly form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  // ── Demo credentials signals ───────────────────────────────────────────────
  readonly showDemoModal = signal(false);
  readonly demoSearch    = signal('');
  readonly copiedKey     = signal<string | null>(null);
  readonly demoSchools   = DEMO_SCHOOLS;

  readonly filteredDemoSchools = computed(() => {
    const q = this.demoSearch().trim().toLowerCase();
    if (!q) return this.demoSchools;
    return this.demoSchools
      .map(school => ({
        ...school,
        instructors: school.instructors.filter(i =>
          i.name.toLowerCase().includes(q)  ||
          i.email.toLowerCase().includes(q) ||
          school.name.toLowerCase().includes(q)
        )
      }))
      .filter(s => s.instructors.length > 0);
  });

  // ── Road canvas ────────────────────────────────────────────────────────────
  private rafId: number | null = null;
  private offset = 0;
  private resizeHandler: (() => void) | null = null;

  ngAfterViewInit() {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    this.startRoad();
  }

  ngOnDestroy() {
    if (this.rafId !== null) cancelAnimationFrame(this.rafId);
    if (this.resizeHandler) window.removeEventListener('resize', this.resizeHandler);
  }

  // ── Keyboard: ESC closes modal ─────────────────────────────────────────────
  @HostListener('keydown.escape')
  onEscape() {
    if (this.showDemoModal()) this.showDemoModal.set(false);
  }

  // ── Demo helpers ───────────────────────────────────────────────────────────
  openDemoModal()  { this.showDemoModal.set(true);  this.demoSearch.set(''); }
  closeDemoModal() { this.showDemoModal.set(false); }

  copyToClipboard(text: string, key: string) {
    navigator.clipboard.writeText(text).then(() => {
      this.copiedKey.set(key);
      setTimeout(() => this.copiedKey.set(null), 2000);
    });
  }

  schoolHue(name: string): number {
    let h = 0;
    for (const c of name) h = (h * 31 + c.charCodeAt(0)) & 0xffff;
    return h % 360;
  }

  // ── Existing auth method ───────────────────────────────────────────────────
  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading.set(true);
    this.error.set(null);

    const { email, password } = this.form.value;

    const profiles: Record<string, { passwordHash: string; instructorId: string; name: string; schoolName: string; schoolId: string }> =
      JSON.parse(localStorage.getItem('instructor_profiles') ?? '{}');
    const saved = profiles[email!];

    if (!saved) {
      this.error.set('No account found for this email. Please register first.');
      this.loading.set(false);
      return;
    }

    if (saved.passwordHash !== btoa(password!)) {
      this.error.set('Incorrect password. Please try again.');
      this.loading.set(false);
      return;
    }

    const { passwordHash: _, ...session } = saved;
    sessionStorage.setItem('instructor_session', JSON.stringify({ ...session, email }));
    this.loading.set(false);
    this.router.navigate(['/instructor/dashboard']);
  }

  // ── Road canvas animation ──────────────────────────────────────────────────
  private startRoad() {
    const canvas = this.canvasRef.nativeElement;
    const ctx    = canvas.getContext('2d')!;

    const resize = () => {
      canvas.width  = canvas.offsetWidth;
      canvas.height = canvas.offsetHeight;
    };
    resize();
    this.resizeHandler = resize;
    window.addEventListener('resize', resize);

    const loop = () => {
      this.offset += 0.014;
      if (this.offset >= 1) this.offset -= 1;
      this.drawRoad(ctx, canvas);
      this.rafId = requestAnimationFrame(loop);
    };
    this.rafId = requestAnimationFrame(loop);
  }

  private drawRoad(ctx: CanvasRenderingContext2D, canvas: HTMLCanvasElement) {
    const W = canvas.width;
    const H = canvas.height;
    ctx.clearRect(0, 0, W, H);

    const vx   = W * 0.5;
    const vy   = H * 0.40;
    const topW = W * 0.07;
    const botW = W * 1.35;

    ctx.beginPath();
    ctx.moveTo(vx - topW / 2, vy);
    ctx.lineTo(vx + topW / 2, vy);
    ctx.lineTo(vx + botW / 2, H);
    ctx.lineTo(vx - botW / 2, H);
    ctx.closePath();
    ctx.fillStyle = '#0f1a0f';
    ctx.fill();

    ctx.lineWidth = 1;
    ctx.strokeStyle = 'rgba(255,255,255,0.10)';
    ctx.beginPath();
    ctx.moveTo(vx - topW / 2, vy);
    ctx.lineTo(vx - botW * 0.88 / 2, H);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(vx + topW / 2, vy);
    ctx.lineTo(vx + botW * 0.88 / 2, H);
    ctx.stroke();

    const N     = 9;
    const FILL  = 0.42;
    const roadH = H - vy;

    for (let i = -1; i <= N + 1; i++) {
      const t0  = (i + this.offset) / N;
      const t1  = t0 + FILL / N;
      const tc0 = Math.max(0, Math.min(1, t0));
      const tc1 = Math.max(0, Math.min(1, t1));
      if (tc1 <= tc0) continue;

      const y0 = vy + roadH * Math.pow(tc0, 1.6);
      const y1 = vy + roadH * Math.pow(tc1, 1.6);
      if (y0 > H || y1 < vy) continue;

      const frac0 = (y0 - vy) / roadH;
      const frac1 = (y1 - vy) / roadH;
      const rw0   = topW + (botW - topW) * frac0;
      const rw1   = topW + (botW - topW) * frac1;
      const dw0   = rw0 * 0.035;
      const dw1   = rw1 * 0.035;

      ctx.beginPath();
      ctx.moveTo(vx - dw0 / 2, y0);
      ctx.lineTo(vx + dw0 / 2, y0);
      ctx.lineTo(vx + dw1 / 2, y1);
      ctx.lineTo(vx - dw1 / 2, y1);
      ctx.closePath();
      ctx.fillStyle = 'rgba(52, 211, 153, 0.90)';
      ctx.fill();
    }

    const glow = ctx.createRadialGradient(vx, vy, 0, vx, vy, W * 0.52);
    glow.addColorStop(0, 'rgba(52,211,153,0.07)');
    glow.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = glow;
    ctx.fillRect(0, 0, W, H);
  }
}
