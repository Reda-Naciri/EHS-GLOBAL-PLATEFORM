export interface CreateReportDto {
  reporterId: string;
  workShift: string;
  title: string;
  type: string;
  zone: string;
  incidentDateTime: string; // Backend expects DateTime, will be converted by Angular HttpClient
  description: string;
  injuredPersonsCount: number;
  injuredPersons: CreateInjuredPersonDto[];
  immediateActionsTaken?: string;
  actionStatus?: string;
  personInChargeOfActions?: string;
  dateActionsCompleted?: string; // Backend expects DateTime, will be converted by Angular HttpClient
  attachments?: File[];
}

export interface CreateInjuredPersonDto {
  name: string;
  department?: string;
  zoneOfPerson?: string;
  gender?: string;
  selectedBodyPart?: string;
  injuryType?: string;
  severity?: string;
  injuryDescription?: string;
  injuries: CreateInjuryDto[];
}

export interface CreateInjuryDto {
  bodyPartId: number;
  fractureTypeId: number;
  severity: string;
  description: string;
  bodyPart?: string; // Optional for backward compatibility
}

export interface ReportDetailDto {
  id: number;
  trackingNumber: string;
  title: string;
  type: string;
  reporterId: string;
  zone: string;
  status: string;
  createdAt: Date;
  incidentDateTime: Date;
  assignedHSE?: string;
  reportDateTime: Date;
  workShift: string;
  shift?: string;
  description: string;
  injuredPersonName?: string;
  injuredPersonDepartment?: string;
  injuredPersonZone?: string;
  bodyMapData?: string;
  injuryType?: string;
  injurySeverity?: string;
  immediateActionsTaken?: string;
  immediateActions?: string;
  actionStatus?: string;
  personInChargeOfActions?: string;
  dateActionsCompleted?: Date;
  injuredPersonsCount: number;
  injuredPersons: InjuredPersonDto[];
  correctiveActions: CorrectiveActionSummaryDto[];
  actions: ActionSummaryDto[];
  comments: CommentDto[];
  attachments: AttachmentDto[];
}

export interface ReportSummaryDto {
  id: number;
  trackingNumber: string;
  title: string;
  type: string;
  status: string;
  createdAt: Date;
  reporterId: string;
  zone: string;
  assignedHSE?: string;
  injuredPersonsCount: number;
  hasAttachments: boolean;
  actionsCount: number;
  correctiveActionsCount: number;
  injurySeverity?: string;
}

export interface RecentReportDto {
  id: number;
  trackingNumber: string;
  title: string;
  type: string;
  status: string;
  createdAt: Date;
  reporterId: string;
  reporterName?: string;
  zone: string;
  assignedHSE?: string;
  injurySeverity?: string;
  isUrgent: boolean;
}

export interface InjuredPersonDto {
  id: number;
  name: string;
  department?: string;
  zoneOfPerson?: string;
  gender?: string;
  selectedBodyPart?: string;
  injuryType?: string;
  severity?: string;
  injuryDescription?: string;
  injuries: InjuryDto[];
}

export interface InjuryDto {
  id: number;
  bodyPart: string;
  injuryType: string;
  severity: string;
  description: string;
  createdAt: Date;
}

export interface CorrectiveActionSummaryDto {
  id: number;
  title: string;
  description: string;
  status: string;
  dueDate?: Date;
  priority: string;
  hierarchy: string;
  assignedTo?: string;
  createdByHSEId: string;
  createdByName?: string;
  subActionsCount: number;
  progressPercentage: number;
  createdAt: Date;
}

export interface ActionSummaryDto {
  id: number;
  title: string;
  description: string;
  status: string;
  dueDate?: Date;
  priority: string;
  hierarchy: string;
  assignedTo?: string;
  createdById?: string;
  createdByName?: string;
  subActionsCount: number;
  progressPercentage: number;
  createdAt: Date;
}

export interface CommentDto {
  id: number;
  content: string;
  createdAt: Date;
  userName: string;
  avatar?: string;
}

export interface AttachmentDto {
  id: number;
  fileName: string;
  fileSize: number;
  fileType?: string;
  uploadedAt: Date;
  downloadUrl: string;
}

export interface ReportSubmissionResponseDto {
  success: boolean;
  message: string;
  reportId?: number;
  trackingNumber?: string;
}

// ===== SUB-ACTION INTERFACES =====
export interface SubActionDetailDto {
  id: number;
  title: string;
  description?: string;
  dueDate?: Date;
  status: string;
  assignedToId?: string;
  assignedToName?: string;
  createdAt: Date;
  updatedAt?: Date;
  
  // Corrective Action details (if this sub-action belongs to a corrective action)
  correctiveActionId?: number;
  correctiveActionTitle?: string;
  correctiveActionDescription?: string;
  correctiveActionDueDate?: Date;
  correctiveActionPriority?: string;
  correctiveActionHierarchy?: string;
  correctiveActionStatus?: string;
  correctiveActionAuthor?: string;
  correctiveActionCreatedAt?: Date;
}

export interface CreateSubActionDto {
  title: string;
  description?: string;
  dueDate?: Date;
  assignedToId?: string;
  // Status is automatically set to "Not Started" on creation
}

export interface UpdateSubActionDto {
  title?: string;
  description?: string;
  dueDate?: Date;
  assignedToId?: string;
  status?: string;
}

export interface ValidateReporterDto {
  reporterId: string;
}

export interface ReporterValidationResponseDto {
  isValid: boolean;
  reporterName?: string;
  department?: string;
  zone?: string;
  message: string;
}