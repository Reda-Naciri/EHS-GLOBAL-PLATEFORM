export interface LoginDto {
  email: string;
  password: string;
}

export interface AuthResponseDto {
  success: boolean;
  message: string;
  token?: string;
  user?: UserDto;
}

export interface UserDto {
  id: string;
  companyId?: string; // Company ID like "TE0001" for user-facing identification  
  email: string;
  firstName?: string;
  lastName?: string;
  fullName?: string;
  role: string;
  department?: string;
  zone?: string;
  position?: string;
  dateOfBirth?: Date;
  avatar?: string;
  isActive?: boolean;
  deactivatedAt?: Date | string;
  deactivationReason?: string;
  accountCreatedAt?: Date;
  lastLoginAt?: Date;
  lastActivityAt?: Date;
  isOnline?: boolean;
  currentStatus?: string;
  [key: string]: any; // Allow any additional properties
}

export interface CreateUserDto {
  email: string;
  password?: string; // Optional - will be generated for HSE/Admin
  fullName: string;
  role: string;
  companyId: string; // Required
  department: string; // Required
  zone?: string;
  position: string; // Required
  dateOfBirth?: Date;
  // Legacy fields for backward compatibility
  firstName?: string;
  lastName?: string;
}

export interface UpdateUserDto {
  firstName: string;
  lastName: string;
  department: string;
  zone: string;
  position: string;
  dateOfBirth?: Date;
}

export interface UpdateUserRoleDto {
  role: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
}

export interface AdminResetPasswordDto {
  newPassword: string;
}