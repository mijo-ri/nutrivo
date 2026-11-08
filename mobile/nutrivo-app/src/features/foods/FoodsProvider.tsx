import { createContext, type PropsWithChildren, useContext, useRef, useState } from 'react';

import type { Food, NewFood } from './food';
import { exampleFoods } from './foodExamples';

type FoodsContextValue = {
    foods: readonly Food[];
    addFood: (food: NewFood) => Food;
};

const FoodsContext = createContext<FoodsContextValue | undefined>(undefined);

export function FoodsProvider({ children }: PropsWithChildren) {
    const [foods, setFoods] = useState<readonly Food[]>(exampleFoods);
    const nextId = useRef(1);

    function addFood(input: NewFood): Food {
        const id = `local-food-${nextId.current++}`;
        const food: Food = {
            ...input,
            id,
            servingSizes: input.servingSizes.map((serving, index) => ({ ...serving, id: `${id}-serving-${index + 1}` })),
        };
        setFoods(current => [...current, food]);
        return food;
    }

    return <FoodsContext value={{ foods, addFood }}>{children}</FoodsContext>;
}

export function useFoods(): FoodsContextValue {
    const context = useContext(FoodsContext);
    if (!context) {
        throw new Error('useFoods must be used within FoodsProvider');
    }
    return context;
}
