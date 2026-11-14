import { mealNames, type MealId } from './diaryMeals';

// Illustrative, already portioned snapshots for the design preview only.
// Real nutrition calculations and rules remain authoritative in the backend.
export type Nutrition = Readonly<{
  calories: number;
  protein: number;
  carbohydrates: number;
  fat: number;
}>;

export type DiaryExampleEntry = Readonly<{
  name: string;
  quantity: number;
  quantityNote?: string;
  nutrition: Nutrition;
}>;

export type DiaryExampleMeal = Readonly<{
  id: MealId;
  name: string;
  entries: readonly DiaryExampleEntry[];
}>;

export const exampleTargets: Nutrition = {
  calories: 2400,
  protein: 180,
  carbohydrates: 260,
  fat: 70,
};

export const exampleMeals: readonly DiaryExampleMeal[] = [
  {
    id: 'breakfast',
    name: mealNames.breakfast,
    entries: [
      {
        name: 'Haferflocken',
        quantity: 50,
        nutrition: {
          calories: 185,
          protein: 7,
          carbohydrates: 30,
          fat: 4,
        },
      },
      {
        name: 'Magerquark',
        quantity: 100,
        nutrition: {
          calories: 67,
          protein: 12,
          carbohydrates: 4,
          fat: 0,
        },
      },
      {
        name: 'Whey Protein Vanille',
        quantity: 30,
        nutrition: {
          calories: 114,
          protein: 24,
          carbohydrates: 2,
          fat: 1,
        },
      },
      {
        name: 'Banane',
        quantity: 100,
        nutrition: {
          calories: 89,
          protein: 1,
          carbohydrates: 20,
          fat: 0,
        },
      },
      {
        name: 'Apfel',
        quantity: 150,
        nutrition: {
          calories: 78,
          protein: 0,
          carbohydrates: 17,
          fat: 0,
        },
      },
      {
        name: 'Erdnussmus',
        quantity: 20,
        nutrition: {
          calories: 122,
          protein: 5,
          carbohydrates: 2,
          fat: 10,
        },
      },
    ],
  },
  {
    id: 'lunch',
    name: mealNames.lunch,
    entries: [
      {
        name: 'Reis',
        quantity: 80,
        quantityNote: 'trocken',
        nutrition: {
          calories: 280,
          protein: 6,
          carbohydrates: 62,
          fat: 1,
        },
      },
      {
        name: 'Pouletbrust',
        quantity: 200,
        nutrition: {
          calories: 220,
          protein: 46,
          carbohydrates: 0,
          fat: 3,
        },
      },
      {
        name: 'Olivenöl',
        quantity: 15,
        nutrition: {
          calories: 135,
          protein: 0,
          carbohydrates: 0,
          fat: 15,
        },
      },
      {
        name: 'Brokkoli',
        quantity: 200,
        nutrition: {
          calories: 68,
          protein: 5,
          carbohydrates: 8,
          fat: 1,
        },
      },
    ],
  },
  {
    id: 'dinner',
    name: mealNames.dinner,
    entries: [
      {
        name: 'Quinoa',
        quantity: 80,
        quantityNote: 'trocken',
        nutrition: {
          calories: 294,
          protein: 11,
          carbohydrates: 51,
          fat: 5,
        },
      },
      {
        name: 'Tomaten',
        quantity: 100,
        nutrition: {
          calories: 18,
          protein: 1,
          carbohydrates: 3,
          fat: 0,
        },
      },
      {
        name: 'Gurke',
        quantity: 100,
        nutrition: {
          calories: 12,
          protein: 1,
          carbohydrates: 2,
          fat: 0,
        },
      },
      {
        name: 'Olivenöl',
        quantity: 15,
        nutrition: {
          calories: 135,
          protein: 0,
          carbohydrates: 0,
          fat: 15,
        },
      },
      {
        name: 'Räuchertofu',
        quantity: 200,
        nutrition: {
          calories: 340,
          protein: 38,
          carbohydrates: 4,
          fat: 20,
        },
      },
    ],
  },
  { id: 'snacks', name: mealNames.snacks, entries: [] },
];

export function sumNutrition(values: readonly Nutrition[]): Nutrition {
  return values.reduce<Nutrition>(
    (total, value) => ({
      calories: total.calories + value.calories,
      protein: total.protein + value.protein,
      carbohydrates: total.carbohydrates + value.carbohydrates,
      fat: total.fat + value.fat,
    }),
    { calories: 0, protein: 0, carbohydrates: 0, fat: 0 },
  );
}

const numberFormat = new Intl.NumberFormat('de-CH', {
  maximumFractionDigits: 0,
});

export function formatNutrition(value: number): string {
  return numberFormat.format(value);
}
