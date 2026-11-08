import { createContext, type PropsWithChildren, useContext, useRef, useState } from 'react';

import type { Food, NewFood } from './food';
import { exampleFoods } from './foodExamples';

type FoodsContextValue = {
    foods: readonly Food[];
    addFood: (food: NewFood) => Food;
    updateFood: (id: string, food: NewFood) => Food | undefined;
    deleteFood: (id: string) => void;
};

const FoodsContext = createContext<FoodsContextValue | undefined>(undefined);

export function FoodsProvider({ children }: PropsWithChildren) {
    const [foods, setFoods] = useState<readonly Food[]>(exampleFoods);
    const nextId = useRef(1);
    const nextServingId = useRef(1);
    const currentFoods = useRef(foods);

    function commit(next: readonly Food[]) {
        // Keep sequential actions in the same event consistent before React renders.
        currentFoods.current = next;
        setFoods(next);
    }

    function addFood(input: NewFood): Food {
        const id = `local-food-${nextId.current++}`;
        const food: Food = {
            ...input,
            id,
            servingSizes: input.servingSizes.map((serving, index) => ({ ...serving, id: `${id}-serving-${index + 1}` })),
        };
        commit([...currentFoods.current, food]);
        return food;
    }

    function updateFood(id: string, input: NewFood): Food | undefined {
        const existing = currentFoods.current.find(food => food.id === id);
        if (!existing) return undefined;

        const retainedIds = new Set(existing.servingSizes.map(serving => serving.id));
        const food: Food = {
            ...input,
            id,
            servingSizes: input.servingSizes.map(serving => {
                const servingId = serving.id !== undefined && retainedIds.delete(serving.id)
                    ? serving.id : `local-serving-${nextServingId.current++}`;
                return { ...serving, id: servingId };
            }),
        };
        commit(currentFoods.current.map(item => item.id === id ? food : item));
        return food;
    }

    function deleteFood(id: string) {
        commit(currentFoods.current.filter(food => food.id !== id));
    }

    return <FoodsContext value={{ foods, addFood, updateFood, deleteFood }}>{children}</FoodsContext>;
}

export function useFoods(): FoodsContextValue {
    const context = useContext(FoodsContext);
    if (!context) {
        throw new Error('useFoods must be used within FoodsProvider');
    }
    return context;
}
