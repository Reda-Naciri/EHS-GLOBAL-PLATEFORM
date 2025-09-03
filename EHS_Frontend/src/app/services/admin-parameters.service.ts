import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// Parameter interfaces matching backend DTOs
export interface Parameter {
  id?: number;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
}

export interface Zone extends Parameter {
  code: string;
}

export interface Department extends Parameter {
  code: string;
}

export interface InjuryType extends Parameter {
  code: string;
  category: string;
}

export interface Shift extends Parameter {
  startTime: string; // TimeSpan as string (HH:mm:ss)
  endTime: string;   // TimeSpan as string (HH:mm:ss)
  code: string;
}

// Create DTOs
export interface CreateZoneDto {
  name: string;
  description?: string;
  code: string;
  isActive: boolean;
}

export interface CreateDepartmentDto {
  name: string;
  description?: string;
  code: string;
  isActive: boolean;
}

export interface CreateInjuryTypeDto {
  name: string;
  description?: string;
  code: string;
  category: string;
  isActive: boolean;
}

export interface CreateShiftDto {
  name: string;
  description?: string;
  startTime: string;
  endTime: string;
  code: string;
  isActive: boolean;
}

// Update DTOs
export interface UpdateZoneDto {
  name?: string;
  description?: string;
  code?: string;
  isActive?: boolean;
}

export interface UpdateDepartmentDto {
  name?: string;
  description?: string;
  code?: string;
  isActive?: boolean;
}

export interface UpdateInjuryTypeDto {
  name?: string;
  description?: string;
  code?: string;
  category?: string;
  isActive?: boolean;
}

export interface UpdateShiftDto {
  name?: string;
  description?: string;
  startTime?: string;
  endTime?: string;
  code?: string;
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AdminParametersService {
  private baseUrl = environment.apiUrl + environment.endpoints.adminParameters;

  constructor(private http: HttpClient) {
    console.log('🔧 AdminParametersService: Constructor called');
    console.log('🔧 AdminParametersService: Base URL:', this.baseUrl);
  }

  // Zone methods
  getZones(): Observable<Zone[]> {
    const url = `${this.baseUrl}/zones`;
    console.log('🌐 Calling zones endpoint:', url);
    return this.http.get<Zone[]>(url);
  }

  getZone(id: number): Observable<Zone> {
    return this.http.get<Zone>(`${this.baseUrl}/zones/${id}`);
  }

  createZone(dto: CreateZoneDto): Observable<Zone> {
    return this.http.post<Zone>(`${this.baseUrl}/zones`, dto);
  }

  updateZone(id: number, dto: UpdateZoneDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/zones/${id}`, dto);
  }

  deleteZone(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/zones/${id}`);
  }

  // Department methods
  getDepartments(): Observable<Department[]> {
    const url = `${this.baseUrl}/departments`;
    console.log('🌐 Calling departments endpoint:', url);
    return this.http.get<Department[]>(url);
  }

  getDepartment(id: number): Observable<Department> {
    return this.http.get<Department>(`${this.baseUrl}/departments/${id}`);
  }

  createDepartment(dto: CreateDepartmentDto): Observable<Department> {
    return this.http.post<Department>(`${this.baseUrl}/departments`, dto);
  }

  updateDepartment(id: number, dto: UpdateDepartmentDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/departments/${id}`, dto);
  }

  deleteDepartment(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/departments/${id}`);
  }

  // Injury Type methods
  getInjuryTypes(): Observable<InjuryType[]> {
    const url = `${this.baseUrl}/injury-types`;
    console.log('🌐 Calling injury types endpoint:', url);
    return this.http.get<InjuryType[]>(url);
  }

  getInjuryType(id: number): Observable<InjuryType> {
    return this.http.get<InjuryType>(`${this.baseUrl}/injury-types/${id}`);
  }

  createInjuryType(dto: CreateInjuryTypeDto): Observable<InjuryType> {
    return this.http.post<InjuryType>(`${this.baseUrl}/injury-types`, dto);
  }

  updateInjuryType(id: number, dto: UpdateInjuryTypeDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/injury-types/${id}`, dto);
  }

  deleteInjuryType(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/injury-types/${id}`);
  }

  // Shift methods
  getShifts(): Observable<Shift[]> {
    const url = `${this.baseUrl}/shifts`;
    console.log('🌐 Calling shifts endpoint:', url);
    return this.http.get<Shift[]>(url);
  }

  getShift(id: number): Observable<Shift> {
    return this.http.get<Shift>(`${this.baseUrl}/shifts/${id}`);
  }

  createShift(dto: CreateShiftDto): Observable<Shift> {
    return this.http.post<Shift>(`${this.baseUrl}/shifts`, dto);
  }

  updateShift(id: number, dto: UpdateShiftDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/shifts/${id}`, dto);
  }

  deleteShift(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/shifts/${id}`);
  }
}