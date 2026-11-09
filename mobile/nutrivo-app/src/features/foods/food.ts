export type ServingSize = Readonly<{
    id: string;
    quantity: number;
    unit: string;
    label?: string;
    equivalentQuantity: number;
}>;

export type Food = Readonly<{
    id: string;
    name: string;
    brand?: string;
    referenceQuantity: number;
    referenceUnit: 'g' | 'ml';
    kilocalories: number;
    proteinGrams: number;
    carbohydratesGrams: number;
    fatGrams: number;
    servingSizes: readonly ServingSize[];
}>;

export type NewFood = Omit<Food, 'id' | 'servingSizes'> & {
    servingSizes: readonly Omit<ServingSize, 'id'>[];
};

export type FoodInput = Omit<NewFood, 'servingSizes'> & {
    servingSizes: readonly (Omit<ServingSize, 'id'> & { id?: string })[];
};

const numberFormat = new Intl.NumberFormat('de-CH', { maximumFractionDigits: 2 });
const quantityFormat = new Intl.NumberFormat('de-CH', { maximumSignificantDigits: 21 });

export function formatFoodNumber(value: number): string {
    return numberFormat.format(value);
}

export function formatFoodQuantity(value: number): string {
    return quantityFormat.format(value);
}
