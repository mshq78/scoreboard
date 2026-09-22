import { http } from "./http";
import type {
  EvaluatorBootstrapDto,
  EvaluatorSyncRequest,
  EvaluatorSyncResponseDto,
} from "@/types/evaluator";

export async function getEvaluatorBootstrap(
  token: string,
): Promise<EvaluatorBootstrapDto> {
  const response = await http.get<EvaluatorBootstrapDto>(
    `/api/evaluator/${token}/bootstrap`,
  );

  return response.data;
}

export async function syncEvaluator(
  token: string,
  request: EvaluatorSyncRequest,
): Promise<EvaluatorSyncResponseDto> {
  const response = await http.post<EvaluatorSyncResponseDto>(
    `/api/evaluator/${token}/sync`,
    request,
  );

  return response.data;
}
