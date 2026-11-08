export type ServingSize = Readonly<{
    id: string;
    quantity: number;
    unit: string;
    label?: string;
    equivalentQuantity: number;
}>;

// Local preview data; the backend remains authoritative when integration is added.
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

const foodOrder = new Intl.Collator('de-CH', { sensitivity: 'base' });
const numberFormat = new Intl.NumberFormat('de-CH', { maximumFractionDigits: 2 });
const quantityFormat = new Intl.NumberFormat('de-CH', { maximumSignificantDigits: 21 });

export function formatFoodNumber(value: number): string {
    return numberFormat.format(value);
}

export function formatFoodQuantity(value: number): string {
    return quantityFormat.format(value);
}

export function searchFoods(foods: readonly Food[], search: string): Food[] {
    const query = search.trim().toLocaleLowerCase('de-CH');

    return foods
        .filter(food => food.name.toLocaleLowerCase('de-CH').includes(query)
            || food.brand?.toLocaleLowerCase('de-CH').includes(query))
        .sort((a, b) => foodOrder.compare(a.name, b.name)
            || foodOrder.compare(a.brand ?? '', b.brand ?? ''));
}
