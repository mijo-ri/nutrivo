import { ApiError, apiRequest } from '@/shared/api/apiClient';

export type NutritionTargets = {
  kilocalories: number;
  proteinGrams: number;
  carbohydratesGrams: number;
  fatGrams: number;
};
export type TargetEnvelope = { targets: NutritionTargets | null };

async function request(
  method: 'GET' | 'PUT',
  body?: NutritionTargets,
  signal?: AbortSignal,
) {
  try {
    return await apiRequest<TargetEnvelope>('/nutrition-targets', {
      method,
      body,
      signal,
    });
  } catch (error) {
    if (signal?.aborted) throw error;
    if (error instanceof ApiError) {
      if (error.message.includes('API-Adresse')) throw error;
      if (method === 'PUT' && error.status === undefined) {
        throw new ApiError(
          'Die Verbindung wurde unterbrochen. Die Ziele wurden möglicherweise gespeichert. Prüfe die gespeicherten Werte, bevor du erneut speicherst.',
          undefined,
          'Targets.OutcomeUnknown',
        );
      }
      throw new ApiError(
        error.status === 400
          ? 'Bitte prüfe deine Zieleingaben.'
          : method === 'GET'
            ? 'Die Ziele konnten nicht geladen werden. Bitte versuche es erneut.'
            : 'Die Ziele konnten nicht gespeichert werden. Bitte versuche es erneut.',
        error.status,
        error.code,
      );
    }
    throw error;
  }
}
export const getTargets = (signal?: AbortSignal) => request('GET', undefined, signal);
export const setTargets = (targets: NutritionTargets) => request('PUT', targets);
