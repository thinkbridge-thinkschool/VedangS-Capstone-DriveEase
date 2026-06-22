export interface EnrollRequest {
  studentId: string;
  drivingSchoolId: string;
  fee: number;
}

export interface EnrollmentDto {
  id: string;
  studentId: string;
  drivingSchoolId: string;
  instructorId: string | null;
  fee: number;
  paymentStatus: string;
  status: string;
  enrolledAt: string;
  paymentConfirmedAt: string | null;
}
