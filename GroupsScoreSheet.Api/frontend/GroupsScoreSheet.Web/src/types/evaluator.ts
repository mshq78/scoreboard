export interface EvaluatorBootstrapDto {
  evaluator: EvaluatorProfileBootstrapDto;
  course: EvaluatorCourseBootstrapDto;
  round: EvaluationRoundBootstrapDto;
  teams: EvaluatorTeamBootstrapDto[];
  events: EvaluatorEventBootstrapDto[];
  scores: EvaluatorScoreBootstrapDto[];
  comments: EvaluatorEventCommentBootstrapDto[];
  isReadOnly: boolean;
  linkStatus: string;
}

export interface EvaluatorProfileBootstrapDto {
  id: string;
  evaluatorName: string;
  evaluatorToken: string;
  status: string;
  firstOpenedAt: string | null;
  lastSyncedAt: string | null;
  finalSyncedAt: string | null;
}

export interface EvaluatorCourseBootstrapDto {
  id: string;
  organizerCompanyName: string;
  holdingDate: string;
  status: string;
}

export interface EvaluationRoundBootstrapDto {
  id: string;
  roundNumber: number;
  status: string;
  createdAt: string;
}

export interface EvaluatorTeamBootstrapDto {
  id: string;
  name: string;
  displayOrder: number;
}

export interface EvaluatorEventBootstrapDto {
  id: string;
  name: string;
  displayOrder: number;
  indicators: EvaluatorIndicatorBootstrapDto[];
}

export interface EvaluatorIndicatorBootstrapDto {
  id: string;
  name: string;
  displayOrder: number;
}

export interface EvaluatorScoreBootstrapDto {
  id: string;
  teamId: string;
  eventId: string;
  indicatorId: string;
  value: number;
  clientUpdatedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface EvaluatorEventCommentBootstrapDto {
  id: string;
  teamId: string;
  eventId: string;
  commentText: string | null;
  clientUpdatedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface EvaluatorSyncRequest {
  courseId: string;
  roundId: string;
  isFinalSync: boolean;
  clientSyncedAt: string;
  scores: EvaluatorSyncScoreRequest[];
  comments: EvaluatorSyncCommentRequest[];
}

export interface EvaluatorSyncScoreRequest {
  teamId: string;
  eventId: string;
  indicatorId: string;
  value: number | null;
  clientUpdatedAt: string;
}

export interface EvaluatorSyncCommentRequest {
  teamId: string;
  eventId: string;
  commentText: string;
  clientUpdatedAt: string;
}

export interface EvaluatorSyncResponseDto {
  success: boolean;
  status: string;
  isFinalized: boolean;
  serverSyncedAt: string;
  rejectedReason: string | null;
  errors: string[];
}

export interface LocalEvaluationState {
  token: string;
  bootstrap: EvaluatorBootstrapDto;
  scores: Record<string, LocalScore>;
  comments: Record<string, LocalComment>;
  isFinalized: boolean;
  lastLocalUpdatedAt: string | null;
  lastSyncedAt: string | null;
}

export interface LocalScore {
  teamId: string;
  eventId: string;
  indicatorId: string;
  value: number | null;
  clientUpdatedAt: string;
}

export interface LocalComment {
  teamId: string;
  eventId: string;
  commentText: string;
  clientUpdatedAt: string;
}
