import { http } from "./http";
import type {
  AdminScoreSheetDto,
  CourseCreatedDto,
  CourseDetailsDto,
  CourseListItemDto,
  EvaluatorProfileDto,
} from "@/types/admin";

export interface CreateEvaluatorRequest {
  evaluatorName: string;
}

export interface ResetCourseRequest {
  resetReason?: string;
}

export interface CreateCourseRequest {
  organizerCompanyName: string;
  holdingDate: string;
  teamCount: number;
  teamNames: string[];
  excelFile: File;
}

export async function getCourses(): Promise<CourseListItemDto[]> {
  const response = await http.get<CourseListItemDto[]>("/api/admin/courses");
  return response.data;
}

export async function getCourseDetails(
  courseId: string,
): Promise<CourseDetailsDto> {
  const response = await http.get<CourseDetailsDto>(
    `/api/admin/courses/${courseId}`,
  );
  return response.data;
}

export async function getEvaluators(
  courseId: string,
): Promise<EvaluatorProfileDto[]> {
  const response = await http.get<EvaluatorProfileDto[]>(
    `/api/admin/courses/${courseId}/evaluators`,
  );

  return response.data;
}

export async function createEvaluator(
  courseId: string,
  request: CreateEvaluatorRequest,
): Promise<EvaluatorProfileDto> {
  const response = await http.post<EvaluatorProfileDto>(
    `/api/admin/courses/${courseId}/evaluators`,
    request,
  );

  return response.data;
}

export async function unfinalizeEvaluator(
  courseId: string,
  evaluatorId: string,
): Promise<EvaluatorProfileDto> {
  const response = await http.post<EvaluatorProfileDto>(
    `/api/admin/courses/${courseId}/evaluators/${evaluatorId}/unfinalize`,
  );

  return response.data;
}

export async function resetCourse(
  courseId: string,
  request: ResetCourseRequest,
): Promise<unknown> {
  const response = await http.post(
    `/api/admin/courses/${courseId}/reset`,
    request,
  );
  return response.data;
}

export async function getScoreSheet(
  courseId: string,
): Promise<AdminScoreSheetDto> {
  const response = await http.get<AdminScoreSheetDto>(
    `/api/admin/courses/${courseId}/score-sheet`,
  );

  return response.data;
}

export function getExportUrl(courseId: string): string {
  return `/api/admin/courses/${courseId}/export`;
}

export async function createCourse(
  request: CreateCourseRequest,
): Promise<CourseCreatedDto> {
  const formData = new FormData();

  formData.append("organizerCompanyName", request.organizerCompanyName);
  formData.append("holdingDate", request.holdingDate);
  formData.append("teamCount", String(request.teamCount));

  for (const teamName of request.teamNames) {
    formData.append("teamNames", teamName);
  }

  formData.append("excelFile", request.excelFile);

  const response = await http.post<CourseCreatedDto>(
    "/api/admin/courses",
    formData,
  );

  return response.data;
}

export async function deleteCourse(courseId: string): Promise<unknown> {
  const response = await http.delete(`/api/admin/courses/${courseId}`);
  return response.data;
}
