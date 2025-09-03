export interface RegisterRequestDto {
  fullName: string;
  companyId: string;
  email: string;
  department: string;
  position: string;
}

export interface RegisterRequestResponse {
  success: boolean;
  message: string;
}