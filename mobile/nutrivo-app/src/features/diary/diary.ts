import { type Food, formatFoodQuantity, type ServingSize } from '../foods/food';
import { parseFoodNumber } from '../foods/foodForm';
import { type Nutrition } from './nutrition';
import { type MealId, mealNames } from './diaryMeals';

export type DiaryAmount =
  | Readonly<{ kind: 'direct'; quantity: number }>
  | Readonly<{ kind: 'serving'; servingSizeId: string; servingCount: number }>;

export type DiaryAmountDraft = Readonly<{
  servingSizeId?: string;
  value: string;
}>;

export type DiaryEntry = Readonly<{
  id: string;
  date: string;
  mealId: MealId;
  food: Readonly<Pick<Food, 'id' | 'name' | 'brand' | 'referenceUnit'>>;
  quantity: number;
  serving?: Readonly<{ size: ServingSize; count: number }>;
  nutrition: Nutrition;
}>;

export type DiaryMeal = Readonly<{
  id: MealId;
  name: string;
  entries: readonly DiaryEntry[];
  nutrition: Nutrition;
}>;

export type DiaryCalculation =
  | {
      value: Pick<DiaryEntry, 'food' | 'quantity' | 'serving' | 'nutrition'>;
      error?: undefined;
    }
  | { value?: undefined; error: string };

export function toDiaryDate(date: Date): string {
  return `${String(date.getFullYear()).padStart(4, '0')}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

export function parseDiaryDate(value: unknown): Date | undefined {
  if (
    typeof value !== 'string' ||
    !/^\d{4}-\d{2}-\d{2}$/.test(value) ||
    value.startsWith('0000')
  ) {
    return undefined;
  }
  const date = new Date(`${value}T12:00:00`);
  return !Number.isNaN(date.getTime()) && toDiaryDate(date) === value ? date : undefined;
}

export function isMealId(value: unknown): value is MealId {
  return typeof value === 'string' && Object.hasOwn(mealNames, value);
}

export function calculateDiaryEntry(food: Food, amount: DiaryAmount): DiaryCalculation {
  const servingSize =
    amount.kind === 'serving'
      ? food.servingSizes.find((size) => size.id === amount.servingSizeId)
      : undefined;
  if (amount.kind === 'serving' && !servingSize) {
    return { error: 'Diese Portionsgrösse ist nicht verfügbar.' };
  }
  const count = amount.kind === 'serving' ? amount.servingCount : amount.quantity;
  if (!Number.isFinite(count) || count <= 0) {
    return { error: 'Eine gültige Zahl grösser als 0 eingeben.' };
  }
  const quantity = servingSize ? count * servingSize.equivalentQuantity : count;
  if (
    !Number.isFinite(quantity) ||
    quantity <= 0 ||
    !Number.isFinite(food.referenceQuantity) ||
    food.referenceQuantity <= 0
  ) {
    return { error: 'Die berechnete Menge liegt ausserhalb des unterstützten Bereichs.' };
  }

  // Local preview only. The backend remains authoritative on API integration.
  const scale = (value: number) =>
    quantity === food.referenceQuantity
      ? value
      : value * (quantity / food.referenceQuantity);
  const nutrition: Nutrition = {
    calories: scale(food.kilocalories),
    protein: scale(food.proteinGrams),
    carbohydrates: scale(food.carbohydratesGrams),
    fat: scale(food.fatGrams),
  };
  if (Object.values(nutrition).some((value) => !Number.isFinite(value) || value < 0)) {
    return {
      error: 'Die berechneten Nährwerte liegen ausserhalb des unterstützten Bereichs.',
    };
  }

  return {
    value: {
      food: {
        id: food.id,
        name: food.name,
        brand: food.brand,
        referenceUnit: food.referenceUnit,
      },
      quantity,
      serving: servingSize ? { size: { ...servingSize }, count } : undefined,
      nutrition,
    },
  };
}

export function previewDiaryEntry(food: Food, draft: DiaryAmountDraft): DiaryCalculation {
  const value = parseFoodNumber(draft.value);
  if (value === undefined || value <= 0) {
    return { error: 'Eine gültige Zahl grösser als 0 eingeben.' };
  }
  return calculateDiaryEntry(
    food,
    draft.servingSizeId === undefined
      ? { kind: 'direct', quantity: value }
      : { kind: 'serving', servingSizeId: draft.servingSizeId, servingCount: value },
  );
}

export function formatServingSize(
  size: ServingSize,
  unit: Food['referenceUnit'],
): string {
  return `${formatFoodQuantity(size.quantity)} ${size.unit}${size.label ? ` (${size.label})` : ''} = ${formatFoodQuantity(size.equivalentQuantity)} ${unit}`;
}

export function formatDiaryQuantity(entry: DiaryEntry): string {
  const direct = `${formatFoodQuantity(entry.quantity)} ${entry.food.referenceUnit}`;
  if (!entry.serving) {
    return direct;
  }
  const { size, count } = entry.serving;
  return `${formatFoodQuantity(count)} × ${formatFoodQuantity(size.quantity)} ${size.unit}${size.label ? ` (${size.label})` : ''} · ${direct}`;
}
