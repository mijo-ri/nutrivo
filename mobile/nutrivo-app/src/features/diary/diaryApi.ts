import { ApiError, apiRequest } from '@/shared/api/apiClient';
import { type Food, type ServingSize } from '../foods/food';
import { parseFoodNumber } from '../foods/foodForm';
import { type DiaryAmountDraft, type DiaryEntry, type DiaryMeal } from './diary';
import { type MealId, mealNames } from './diaryMeals';
import { type Nutrition } from './nutrition';

type NutrientsDto = {
  kilocalories: number;
  proteinGrams: number;
  carbohydratesGrams: number;
  fatGrams: number;
};
type ServingDto = Omit<ServingSize, 'label'> & { label: string | null };
type EntryDto = NutrientsDto & {
  id: string;
  mealType: number;
  foodId: string;
  name: string;
  brand: string | null;
  quantity: number;
  referenceUnit: number;
  servingCount: number | null;
  servingSize: ServingDto | null;
};
type DayDto = {
  date: string;
  entries: EntryDto[];
  totals: NutrientsDto;
  mealTotals: { mealType: number; totals: NutrientsDto }[];
};
type DetailDto = Omit<EntryDto, 'foodId' | 'name' | 'brand'> & {
  date: string;
  food: NutrientsDto & {
    id: string;
    name: string;
    brand: string | null;
    referenceQuantity: number;
    referenceUnit: number;
  };
};
export type DiaryDay = { date: string; meals: readonly DiaryMeal[]; total: Nutrition };
export type DiaryDetail = {
  entry: DiaryEntry;
  food: Food;
  initialDraft: DiaryAmountDraft;
};
const mealIds = ['breakfast', 'lunch', 'dinner', 'snacks'] as const;
const toMeal = (value: number): MealId => {
  const meal = mealIds[value - 1];
  if (!meal) throw new ApiError('Die API hat eine unbekannte Mahlzeit geliefert.');
  return meal;
};
const toUnit = (value: number): Food['referenceUnit'] => {
  if (value !== 1 && value !== 2)
    throw new ApiError('Die API hat eine unbekannte Einheit geliefert.');
  return value === 1 ? 'g' : 'ml';
};
const nutrition = (dto: NutrientsDto): Nutrition => ({
  calories: dto.kilocalories,
  protein: dto.proteinGrams,
  carbohydrates: dto.carbohydratesGrams,
  fat: dto.fatGrams,
});
const serving = (dto: ServingDto): ServingSize => ({
  ...dto,
  label: dto.label ?? undefined,
});
function entry(dto: EntryDto, date: string): DiaryEntry {
  return {
    id: dto.id,
    date,
    mealId: toMeal(dto.mealType),
    food: {
      id: dto.foodId,
      name: dto.name,
      brand: dto.brand ?? undefined,
      referenceUnit: toUnit(dto.referenceUnit),
    },
    quantity: dto.quantity,
    nutrition: nutrition(dto),
    serving:
      dto.servingSize && dto.servingCount !== null
        ? { size: serving(dto.servingSize), count: dto.servingCount }
        : undefined,
  };
}

async function request<T>(
  path: string,
  options: Parameters<typeof apiRequest>[1] = {},
): Promise<T> {
  try {
    return await apiRequest<T>(path, options);
  } catch (error) {
    if (!(error instanceof ApiError) || options.signal?.aborted) throw error;
    if (
      error.status === undefined &&
      options.method &&
      !error.message.startsWith('Die API-Adresse')
    ) {
      throw new ApiError(
        'Die Verbindung wurde unterbrochen. Die Änderung wurde möglicherweise bereits gespeichert. Bitte prüfe das Tagebuch, bevor du erneut speicherst oder löschst.',
        undefined,
        'Diary.Write.OutcomeUnknown',
      );
    }
    if (error.status === 404)
      throw new ApiError(
        error.code === 'LogFood.Food.NotFound'
          ? 'Dieses Lebensmittel ist nicht mehr verfügbar. Bitte wähle es erneut aus.'
          : 'Dieser Tagebucheintrag ist nicht mehr vorhanden.',
        404,
        error.code,
      );
    if (error.status === 400)
      throw new ApiError(
        'Bitte prüfe die Menge. Sie muss grösser als 0 sein und zum gespeicherten Eintrag passen.',
        400,
        error.code,
      );
    throw error;
  }
}
export const diaryErrorMessage = (error: unknown) =>
  error instanceof Error
    ? error.message
    : 'Das Tagebuch konnte nicht geladen werden. Bitte versuche es erneut.';
export const isUncertainDiaryWrite = (error: unknown) =>
  error instanceof ApiError && error.code === 'Diary.Write.OutcomeUnknown';

export async function getDiaryDay(date: string, signal?: AbortSignal): Promise<DiaryDay> {
  const dto = await request<DayDto>(`/diary-entries?date=${encodeURIComponent(date)}`, {
    signal,
  });
  if (dto.date !== date)
    throw new ApiError('Die API hat Daten für ein anderes Datum geliefert.');
  const entries = dto.entries.map((item) => entry(item, dto.date));
  return {
    date: dto.date,
    total: nutrition(dto.totals),
    meals: mealIds.map((id) => {
      const total = dto.mealTotals.find((item) => toMeal(item.mealType) === id);
      if (!total) throw new ApiError('Die Mahlzeitensummen fehlen in der Antwort.');
      return {
        id,
        name: mealNames[id],
        entries: entries.filter((item) => item.mealId === id),
        nutrition: nutrition(total.totals),
      };
    }),
  };
}
export async function getDiaryEntry(
  id: string,
  signal: AbortSignal,
): Promise<DiaryDetail> {
  const dto = await request<DetailDto>(`/diary-entries/${encodeURIComponent(id)}`, {
    signal,
  });
  const result = entry(
    { ...dto, foodId: dto.food.id, name: dto.food.name, brand: dto.food.brand },
    dto.date,
  );
  return {
    entry: result,
    food: {
      ...dto.food,
      brand: dto.food.brand ?? undefined,
      referenceUnit: toUnit(dto.food.referenceUnit),
      servingSizes: dto.servingSize ? [serving(dto.servingSize)] : [],
    },
    initialDraft: {
      servingSizeId: dto.servingSize?.id,
      value: String(dto.servingCount ?? dto.quantity),
    },
  };
}
export async function logDiaryFood(
  food: Food,
  date: string,
  mealId: MealId,
  draft: DiaryAmountDraft,
) {
  const value = parseFoodNumber(draft.value);
  return request<{ id: string }>('/diary-entries', {
    method: 'POST',
    body: {
      foodId: food.id,
      date,
      mealType: mealIds.indexOf(mealId) + 1,
      ...(draft.servingSizeId
        ? { servingSizeId: draft.servingSizeId, servingCount: value }
        : { quantity: value, referenceUnit: food.referenceUnit === 'g' ? 1 : 2 }),
    },
  });
}
export async function updateDiaryEntry(id: string, draft: DiaryAmountDraft) {
  return request<void>(`/diary-entries/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: draft.servingSizeId
      ? { servingCount: parseFoodNumber(draft.value) }
      : { quantity: parseFoodNumber(draft.value) },
  });
}
export async function removeDiaryEntry(id: string) {
  return request<void>(`/diary-entries/${encodeURIComponent(id)}`, { method: 'DELETE' });
}
