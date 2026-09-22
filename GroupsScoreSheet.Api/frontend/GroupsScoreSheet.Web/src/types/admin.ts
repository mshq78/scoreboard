export interface CourseListItemDto {
  id: string;
  organizerCompanyName: string;
  holdingDate: string;
  status: string;
  teamCount: number;
  eventCount: number;
  indicatorCount: number;
  activeRoundNumber: number;
  evaluatorCount: number;
  finalizedEvaluatorCount: number;
  createdAt: string;
}

export interface CourseDetailsDto {
  id: string;
  organizerCompanyName: string;
  holdingDate: string;
  status: string;
  activeRound: EvaluationRoundDto | null;
  teams: CourseTeamDto[];
  events: CourseEventDto[];
  evaluatorSummary: CourseEvaluatorSummaryDto;
  createdAt: string;
  updatedAt: string | null;
}

export interface EvaluationRoundDto {
  id: string;
  roundNumber: number;
  status: string;
  createdAt: string;
  closedAt: string | null;
}

export interface CourseTeamDto {
  id: string;
  name: string;
  displayOrder: number;
}

export interface CourseEventDto {
  id: string;
  name: string;
  displayOrder: number;
  indicators: EventIndicatorDto[];
}

export interface EventIndicatorDto {
  id: string;
  name: string;
  displayOrder: number;
}

export interface CourseEvaluatorSummaryDto {
  totalEvaluators: number;
  activeEvaluators: number;
  finalizedEvaluators: number;
  invalidatedEvaluators: number;
}

export interface EvaluatorProfileDto {
  id: string;
  courseId: string;
  evaluationRoundId: string;
  roundNumber: number;
  evaluatorName: string;
  evaluatorToken: string;
  evaluatorUrl: string;
  status: string;
  firstOpenedAt: string | null;
  lastSyncedAt: string | null;
  finalSyncedAt: string | null;
  createdAt: string;
}

export interface AdminScoreSheetDto {
  courseId: string;
  organizerCompanyName: string;
  holdingDate: string;
  activeRound: {
    id: string;
    roundNumber: number;
    status: string;
    createdAt: string;
  };
  evaluatorCount: number;
  evaluatorWithSubmittedScoreCount: number;
  hasSubmittedScores: boolean;
  message: string | null;
  events: AdminScoreSheetEventDto[];
}

export interface AdminScoreSheetEventDto {
  eventId: string;
  eventName: string;
  eventDisplayOrder: number;
  indicators: AdminScoreSheetIndicatorDto[];
  rows: AdminScoreSheetTeamRowDto[];
}

export interface AdminScoreSheetIndicatorDto {
  indicatorId: string;
  indicatorName: string;
  indicatorDisplayOrder: number;
}

export interface AdminScoreSheetTeamRowDto {
  teamId: string;
  teamName: string;
  teamDisplayOrder: number;
  indicatorValues: AdminScoreSheetIndicatorValueDto[];
  total: number | null;
  average: number | null;
  combinedComments: string | null;
  commentLines: AdminScoreSheetCommentLineDto[];
}

export interface AdminScoreSheetIndicatorValueDto {
  indicatorId: string;
  indicatorName: string;
  averageScore: number | null;
  submittedScoreCount: number;
  evaluatorScores: AdminScoreSheetEvaluatorScoreDto[];
}

export interface AdminScoreSheetEvaluatorScoreDto {
  evaluatorProfileId: string;
  evaluatorName: string;
  score: number;
}

export interface AdminScoreSheetCommentLineDto {
  evaluatorName: string;
  commentText: string;
}

export interface CourseCreatedDto {
  id: string;
  organizerCompanyName: string;
  holdingDate: string;
  teamCount: number;
  eventCount: number;
  indicatorCount: number;
  activeRoundId: string;
  activeRoundNumber: number;
  createdAt: string;
}
