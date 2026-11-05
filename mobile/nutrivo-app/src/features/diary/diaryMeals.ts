export const mealNames = {
    breakfast: 'Frühstück',
    lunch: 'Mittagessen',
    dinner: 'Nachtessen',
    snacks: 'Snacks',
} as const;

export type MealId = keyof typeof mealNames;

export function getMealName(value: unknown): string | undefined {
    return Object.entries(mealNames).find(([id]) => id === value)?.[1];
}
