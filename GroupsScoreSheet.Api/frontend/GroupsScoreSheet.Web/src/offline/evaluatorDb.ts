import { openDB, type DBSchema } from "idb";
import type {
  EvaluatorBootstrapDto,
  EvaluatorEventCommentBootstrapDto,
  EvaluatorScoreBootstrapDto,
  LocalComment,
  LocalEvaluationState,
  LocalScore,
} from "@/types/evaluator";

interface GroupsScoreSheetDb extends DBSchema {
  evaluations: {
    key: string;
    value: LocalEvaluationState;
  };
}

const DB_NAME = "groups-score-sheet-db";
const DB_VERSION = 1;
const STORE_NAME = "evaluations";

interface MergeResult<TValue> {
  values: Record<string, TValue>;
  hasPendingLocalChanges: boolean;
}

async function getDb() {
  return openDB<GroupsScoreSheetDb>(DB_NAME, DB_VERSION, {
    upgrade(db) {
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME);
      }
    },
  });
}

export function makeScoreKey(
  teamId: string,
  eventId: string,
  indicatorId: string,
): string {
  return `${teamId}|${eventId}|${indicatorId}`;
}

export function makeCommentKey(teamId: string, eventId: string): string {
  return `${teamId}|${eventId}`;
}

export async function getLocalEvaluation(
  token: string,
): Promise<LocalEvaluationState | undefined> {
  const db = await getDb();
  return db.get(STORE_NAME, token);
}

export async function saveBootstrapToLocal(
  token: string,
  bootstrap: EvaluatorBootstrapDto,
): Promise<LocalEvaluationState> {
  const existing = await getLocalEvaluation(token);

  const scoreMerge = mergeScores(existing?.scores ?? {}, bootstrap.scores);
  const commentMerge = mergeComments(existing?.comments ?? {}, bootstrap.comments);

  const hasPendingLocalChanges =
    scoreMerge.hasPendingLocalChanges || commentMerge.hasPendingLocalChanges;

  const state: LocalEvaluationState = {
    token,
    bootstrap,
    scores: scoreMerge.values,
    comments: commentMerge.values,
    isFinalized: bootstrap.isReadOnly,
    lastLocalUpdatedAt: hasPendingLocalChanges
      ? existing?.lastLocalUpdatedAt ?? new Date().toISOString()
      : null,
    lastSyncedAt: bootstrap.evaluator.lastSyncedAt,
  };

  const db = await getDb();
  await db.put(STORE_NAME, state, token);

  return state;
}

export async function saveLocalScore(
  token: string,
  score: LocalScore,
): Promise<LocalEvaluationState> {
  const state = await getRequiredState(token);

  const key = makeScoreKey(score.teamId, score.eventId, score.indicatorId);

  state.scores[key] = score;
  state.lastLocalUpdatedAt = new Date().toISOString();

  const db = await getDb();
  await db.put(STORE_NAME, state, token);

  return state;
}

export async function clearLocalScore(
  token: string,
  teamId: string,
  eventId: string,
  indicatorId: string,
): Promise<LocalEvaluationState> {
  return saveLocalScore(token, {
    teamId,
    eventId,
    indicatorId,
    value: null,
    clientUpdatedAt: new Date().toISOString(),
  });
}

export async function saveLocalComment(
  token: string,
  comment: LocalComment,
): Promise<LocalEvaluationState> {
  const state = await getRequiredState(token);

  const key = makeCommentKey(comment.teamId, comment.eventId);

  state.comments[key] = {
    ...comment,
    commentText: comment.commentText.trim(),
  };

  state.lastLocalUpdatedAt = new Date().toISOString();

  const db = await getDb();
  await db.put(STORE_NAME, state, token);

  return state;
}

export async function markLocalSynced(
  token: string,
  isFinalized: boolean,
  syncedAt: string,
): Promise<LocalEvaluationState> {
  const state = await getRequiredState(token);

  state.isFinalized = isFinalized;
  state.lastSyncedAt = syncedAt;
  state.lastLocalUpdatedAt = null;
  state.bootstrap.isReadOnly = isFinalized;
  state.bootstrap.evaluator.lastSyncedAt = syncedAt;
  state.bootstrap.linkStatus = isFinalized ? "Finalized" : "Active";

  for (const [key, score] of Object.entries(state.scores)) {
    if (score.value === null) {
      delete state.scores[key];
    }
  }

  for (const [key, comment] of Object.entries(state.comments)) {
    if (comment.commentText.trim().length === 0) {
      delete state.comments[key];
    }
  }

  const db = await getDb();
  await db.put(STORE_NAME, state, token);

  return state;
}

async function getRequiredState(token: string): Promise<LocalEvaluationState> {
  const state = await getLocalEvaluation(token);

  if (!state) {
    throw new Error("Local evaluation data was not found.");
  }

  return state;
}

function mergeScores(
  localScores: Record<string, LocalScore>,
  remoteScores: EvaluatorScoreBootstrapDto[],
): MergeResult<LocalScore> {
  const merged: Record<string, LocalScore> = {};
  const remoteMap = new Map<string, EvaluatorScoreBootstrapDto>();
  let hasPendingLocalChanges = false;

  for (const remoteScore of remoteScores) {
    const key = makeScoreKey(
      remoteScore.teamId,
      remoteScore.eventId,
      remoteScore.indicatorId,
    );

    remoteMap.set(key, remoteScore);

    merged[key] = {
      teamId: remoteScore.teamId,
      eventId: remoteScore.eventId,
      indicatorId: remoteScore.indicatorId,
      value: remoteScore.value,
      clientUpdatedAt: getRemoteTimestamp(remoteScore),
    };
  }

  for (const [key, localScore] of Object.entries(localScores)) {
    const remoteScore = remoteMap.get(key);
    const remoteTimestamp = getRemoteTimestamp(remoteScore);

    if (isLocalNewer(localScore.clientUpdatedAt, remoteTimestamp)) {
      merged[key] = localScore;
      hasPendingLocalChanges = true;
    }
  }

  return {
    values: merged,
    hasPendingLocalChanges,
  };
}

function mergeComments(
  localComments: Record<string, LocalComment>,
  remoteComments: EvaluatorEventCommentBootstrapDto[],
): MergeResult<LocalComment> {
  const merged: Record<string, LocalComment> = {};
  const remoteMap = new Map<string, EvaluatorEventCommentBootstrapDto>();
  let hasPendingLocalChanges = false;

  for (const remoteComment of remoteComments) {
    const key = makeCommentKey(remoteComment.teamId, remoteComment.eventId);

    remoteMap.set(key, remoteComment);

    merged[key] = {
      teamId: remoteComment.teamId,
      eventId: remoteComment.eventId,
      commentText: remoteComment.commentText ?? "",
      clientUpdatedAt: getRemoteTimestamp(remoteComment),
    };
  }

  for (const [key, localComment] of Object.entries(localComments)) {
    const remoteComment = remoteMap.get(key);
    const remoteTimestamp = getRemoteTimestamp(remoteComment);

    if (isLocalNewer(localComment.clientUpdatedAt, remoteTimestamp)) {
      merged[key] = localComment;
      hasPendingLocalChanges = true;
    }
  }

  return {
    values: merged,
    hasPendingLocalChanges,
  };
}

function getRemoteTimestamp(
  item?: {
    clientUpdatedAt: string | null;
    updatedAt?: string | null;
    createdAt?: string | null;
  },
): string {
  return item?.clientUpdatedAt ?? item?.updatedAt ?? item?.createdAt ?? "";
}

function isLocalNewer(localTimestamp: string, remoteTimestamp: string): boolean {
  if (!remoteTimestamp) {
    return true;
  }

  const localTime = Date.parse(localTimestamp);
  const remoteTime = Date.parse(remoteTimestamp);

  if (Number.isNaN(localTime)) {
    return false;
  }

  if (Number.isNaN(remoteTime)) {
    return true;
  }

  return localTime > remoteTime;
}
