import { ApiError, apiRequest } from '@/shared/api/apiClient';

import type { Food, FoodInput, NewFood } from './food';

type ServingSizeDto = {
  id: string;
  quantity: number;
  unit: string;
  label: string | null;
  equivalentQuantity: number;
  kilocalories: number;
  proteinGrams: number;
  carbohydratesGrams: number;
  fatGrams: number;
};

type FoodDto = Omit<Food, 'brand' | 'referenceUnit' | 'servingSizes'> & {
  brand: string | null;
  referenceUnit: 1 | 2;
  servingSizes: ServingSizeDto[];
};

type FoodPageDto = {
  items: FoodDto[];
  page: number;
  pageSize: number;
  totalCount: number;
};

function toFood(dto: FoodDto): Food {
  if (dto.referenceUnit !== 1 && dto.referenceUnit !== 2) {
    throw new ApiError('Die API hat eine unbekannte Lebensmitteleinheit geliefert.');
  }
  return {
    ...dto,
    brand: dto.brand ?? undefined,
    referenceUnit: dto.referenceUnit === 1 ? 'g' : 'ml',
    servingSizes: dto.servingSizes.map((size) => ({
      id: size.id,
      quantity: size.quantity,
      unit: size.unit,
      label: size.label ?? undefined,
      equivalentQuantity: size.equivalentQuantity,
    })),
  };
}

export async function searchFoods(search: string, page: number, signal: AbortSignal) {
  const result = await apiRequest<FoodPageDto>(
    `/foods?search=${encodeURIComponent(search)}&page=${page}&pageSize=20`,
    { signal },
  );
  return { ...result, items: result.items.map(toFood) };
}

export async function getFood(id: string, signal: AbortSignal): Promise<Food> {
  return toFood(
    await apiRequest<FoodDto>(`/foods/${encodeURIComponent(id)}`, { signal }),
  );
}

export async function deleteFood(id: string): Promise<void> {
  return apiRequest<void>(`/foods/${encodeURIComponent(id)}`, { method: 'DELETE' });
}

export async function createFood(input: NewFood): Promise<{ id: string }> {
  return apiRequest('/foods', {
    method: 'POST',
    body: {
      ...input,
      brand: input.brand ?? null,
      referenceUnit: input.referenceUnit === 'g' ? 1 : 2,
      servingSizes: input.servingSizes.map((size) => ({
        quantity: size.quantity,
        unit: size.unit,
        label: size.label ?? null,
        equivalentQuantity: size.equivalentQuantity,
      })),
    },
  });
}

export async function updateFood(id: string, input: FoodInput): Promise<{ id: string }> {
  return apiRequest(`/foods/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: {
      ...input,
      brand: input.brand ?? null,
      referenceUnit: input.referenceUnit === 'g' ? 1 : 2,
      servingSizes: input.servingSizes.map((size) => ({
        id: size.id ?? null,
        quantity: size.quantity,
        unit: size.unit,
        label: size.label ?? null,
        equivalentQuantity: size.equivalentQuantity,
      })),
    },
  });
}

const validationMessages: Record<string, string> = {
  'ServingSize.Id.Empty':
    'Die Portions-ID ist ungültig. Bitte lade das Lebensmittel erneut.',
  'Food.ServingSize.Id.Duplicate':
    'Eine Portionsgrösse ist mehrfach vorhanden. Bitte lade das Lebensmittel erneut.',
  'Food.ServingSize.NotFound':
    'Eine Portionsgrösse ist nicht mehr verfügbar. Bitte lade das Lebensmittel erneut.',
  'Food.Name.Required': 'Bitte gib einen Namen ein.',
  'Food.Name.TooLong': 'Der Name darf höchstens 200 Zeichen enthalten.',
  'Food.Brand.TooLong': 'Die Marke darf höchstens 200 Zeichen enthalten.',
  'Food.ReferenceQuantity.Invalid': 'Die Bezugsmenge muss grösser als 0 sein.',
  'Food.ReferenceUnit.Invalid': 'Bitte wähle Gramm oder Milliliter.',
  'Food.NutritionalValues.Required': 'Bitte gib die Nährwerte an.',
  'NutritionalValues.Kilocalories.Negative': 'Kalorien dürfen nicht negativ sein.',
  'NutritionalValues.ProteinGrams.Negative': 'Protein darf nicht negativ sein.',
  'NutritionalValues.CarbohydratesGrams.Negative':
    'Kohlenhydrate dürfen nicht negativ sein.',
  'NutritionalValues.FatGrams.Negative': 'Fett darf nicht negativ sein.',
  'Food.ServingSize.Required': 'Bitte prüfe die Portionsgrössen.',
  'ServingSize.Quantity.Invalid': 'Die Portionsmenge muss grösser als 0 sein.',
  'ServingSize.Unit.Required': 'Bitte gib für jede Portion eine Einheit an.',
  'ServingSize.Unit.TooLong': 'Die Portionseinheit darf höchstens 200 Zeichen enthalten.',
  'ServingSize.Label.TooLong':
    'Die Portionsbezeichnung darf höchstens 200 Zeichen enthalten.',
  'ServingSize.EquivalentQuantity.Invalid':
    'Die entsprechende Menge muss grösser als 0 sein.',
  'Food.ServingSize.Nutrition.OutOfRange':
    'Die berechneten Portionsnährwerte sind zu gross. Bitte prüfe die Mengen.',
};

export function foodErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return (error.code && validationMessages[error.code]) || error.message;
  }
  return 'Die Anfrage konnte nicht abgeschlossen werden. Bitte versuche es erneut.';
}
