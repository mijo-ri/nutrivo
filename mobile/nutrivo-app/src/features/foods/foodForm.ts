import type { Food, FoodInput } from './food';

export type ServingDraft = {
  key: string;
  id?: string;
  quantity: string;
  unit: string;
  label: string;
  equivalentQuantity: string;
};

export type FoodDraft = {
  name: string;
  brand: string;
  referenceQuantity: string;
  referenceUnit: Food['referenceUnit'];
  kilocalories: string;
  proteinGrams: string;
  carbohydratesGrams: string;
  fatGrams: string;
  servingSizes: ServingDraft[];
};

export const emptyFoodDraft: FoodDraft = {
  name: '',
  brand: '',
  referenceQuantity: '100',
  referenceUnit: 'g',
  kilocalories: '',
  proteinGrams: '',
  carbohydratesGrams: '',
  fatGrams: '',
  servingSizes: [],
};

// Input formatting must round-trip the stored number, including very small/large values.
const inputNumber = (value: number) =>
  value.toLocaleString('en-US', { useGrouping: false, maximumSignificantDigits: 21 });

export function foodToDraft(food: Food): FoodDraft {
  return {
    name: food.name,
    brand: food.brand ?? '',
    referenceQuantity: inputNumber(food.referenceQuantity),
    referenceUnit: food.referenceUnit,
    kilocalories: inputNumber(food.kilocalories),
    proteinGrams: inputNumber(food.proteinGrams),
    carbohydratesGrams: inputNumber(food.carbohydratesGrams),
    fatGrams: inputNumber(food.fatGrams),
    servingSizes: food.servingSizes.map((serving) => ({
      key: `existing-${serving.id}`,
      id: serving.id,
      quantity: inputNumber(serving.quantity),
      unit: serving.unit,
      label: serving.label ?? '',
      equivalentQuantity: inputNumber(serving.equivalentQuantity),
    })),
  };
}

export type FoodFormErrors = Record<string, string>;

// Reject partial parses, grouping separators, scientific notation and incomplete decimals.
export function parseFoodNumber(input: string): number | undefined {
  const text = input.trim();
  if (!/^\d+(?:[.,]\d+)?$/.test(text)) {
    return undefined;
  }
  const number = Number(text.replace(',', '.'));
  return Number.isFinite(number) ? number : undefined;
}

export function validateFoodDraft(
  draft: FoodDraft,
):
  | { food: FoodInput; errors: FoodFormErrors }
  | { food?: undefined; errors: FoodFormErrors } {
  const errors: FoodFormErrors = {};

  function text(key: string, value: string, required = false): string {
    const trimmed = value.trim();
    if (required && !trimmed) {
      errors[key] = 'Bitte ausfüllen.';
    } else if (trimmed.length > 200) {
      errors[key] = 'Höchstens 200 Zeichen eingeben.';
    }
    return trimmed;
  }

  function number(key: string, value: string, positive = false): number {
    const parsed = parseFoodNumber(value);
    if (parsed === undefined) {
      errors[key] = 'Eine gültige, nicht negative Zahl eingeben.';
    } else if (positive && parsed <= 0) {
      errors[key] = 'Die Menge muss grösser als 0 sein.';
    }
    return parsed ?? 0;
  }

  const food: FoodInput = {
    name: text('name', draft.name, true),
    brand: text('brand', draft.brand) || undefined,
    referenceQuantity: number('referenceQuantity', draft.referenceQuantity, true),
    referenceUnit: draft.referenceUnit,
    kilocalories: number('kilocalories', draft.kilocalories),
    proteinGrams: number('proteinGrams', draft.proteinGrams),
    carbohydratesGrams: number('carbohydratesGrams', draft.carbohydratesGrams),
    fatGrams: number('fatGrams', draft.fatGrams),
    servingSizes: draft.servingSizes.map((serving) => ({
      id: serving.id,
      quantity: number(`${serving.key}.quantity`, serving.quantity, true),
      unit: text(`${serving.key}.unit`, serving.unit, true),
      label: text(`${serving.key}.label`, serving.label) || undefined,
      equivalentQuantity: number(
        `${serving.key}.equivalentQuantity`,
        serving.equivalentQuantity,
        true,
      ),
    })),
  };

  return Object.keys(errors).length ? { errors } : { food, errors };
}
