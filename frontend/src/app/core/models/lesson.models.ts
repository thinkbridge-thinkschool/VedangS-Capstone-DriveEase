export interface BookLessonRequest {
  enrollmentId: string;
  studentId: string;
  instructorId: string;
  scheduledAt: string;
  duration: string;
}

export interface LessonDto {
  id: string;
  enrollmentId: string;
  studentId: string;
  instructorId: string;
  scheduledAt: string;
  duration: string;
  status: string;
  notes: string | null;
}
