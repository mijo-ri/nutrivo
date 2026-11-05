// Illustrative catalogue values per 100 g or 100 ml, independent of diary portions.
export type ExampleFood = Readonly<{
    id: string;
    name: string;
    brand?: string;
    referenceUnit: 'g' | 'ml';
    kilocaloriesPer100: number;
}>;

export const exampleFoods: readonly ExampleFood[] = [
    {
        id: 'apple',
        name: 'Apfel',
        referenceUnit: 'g',
        kilocaloriesPer100: 52,
    },
    {
        id: 'banana',
        name: 'Banane',
        referenceUnit: 'g',
        kilocaloriesPer100: 89,
    },
    {
        id: 'broccoli',
        name: 'Brokkoli',
        referenceUnit: 'g',
        kilocaloriesPer100: 34,
    },
    {
        id: 'oats-alpen',
        name: 'Haferflocken',
        brand: 'Alpenkorn',
        referenceUnit: 'g',
        kilocaloriesPer100: 370,
    },
    {
        id: 'oats-morgen',
        name: 'Haferflocken',
        brand: 'Morgenmühle',
        referenceUnit: 'g',
        kilocaloriesPer100: 372,
    },
    {
        id: 'oat-drink',
        name: 'Haferdrink ungesüsst',
        brand: 'Morgenmühle',
        referenceUnit: 'ml',
        kilocaloriesPer100: 43,
    },
    {
        id: 'quark',
        name: 'Magerquark',
        brand: 'Alpenhof',
        referenceUnit: 'g',
        kilocaloriesPer100: 67,
    },
    {
        id: 'milk',
        name: 'Milch',
        brand: 'Alpenhof',
        referenceUnit: 'ml',
        kilocaloriesPer100: 64,
    },
    {
        id: 'olive-oil',
        name: 'Olivenöl',
        brand: 'Sonnenhain',
        referenceUnit: 'g',
        kilocaloriesPer100: 900,
    },
    {
        id: 'orange-juice',
        name: 'Orangensaft',
        brand: 'Sonnenhain',
        referenceUnit: 'ml',
        kilocaloriesPer100: 45,
    },
    {
        id: 'rice',
        name: 'Reis, trocken',
        referenceUnit: 'g',
        kilocaloriesPer100: 350,
    },
    {
        id: 'tofu',
        name: 'Räuchertofu mit Kräutern und Sonnenblumenkernen',
        brand: 'Grüne Küche Naturkost',
        referenceUnit: 'g',
        kilocaloriesPer100: 170,
    },
    {
        id: 'water',
        name: 'Wasser',
        referenceUnit: 'ml',
        kilocaloriesPer100: 0,
    },
    {
        id: 'whey',
        name: 'Whey Protein Vanille',
        brand: 'Kraftquelle',
        referenceUnit: 'g',
        kilocaloriesPer100: 380,
    },
];

const foodOrder = new Intl.Collator('de-CH', { sensitivity: 'base' });

export function searchExampleFoods(search: string): ExampleFood[] {
    const query = search.trim().toLocaleLowerCase('de-CH');

    if (!query) {
        return [];
    }

    return exampleFoods
        .filter(food => food.name.toLocaleLowerCase('de-CH').includes(query)
            || food.brand?.toLocaleLowerCase('de-CH').includes(query))
        .sort((a, b) => foodOrder.compare(a.name, b.name)
            || foodOrder.compare(a.brand ?? '', b.brand ?? ''));
}
