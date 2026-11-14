export type Nutrition = Readonly<{
  calories: number;
  protein: number;
  carbohydrates: number;
  fat: number;
}>;

const numberFormat = new Intl.NumberFormat('de-CH', {
  maximumFractionDigits: 0,
});

export function formatNutrition(value: number): string {
  return numberFormat.format(value);
}
