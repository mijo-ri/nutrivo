import { createContext, type PropsWithChildren, useCallback, useContext, useRef, useState } from 'react';

import { type Food } from '../foods/food';
import { type DiaryAmountDraft, type DiaryEntry, type DiaryMeal, isMealId, parseDiaryDate, previewDiaryEntry, toDiaryDate } from './diary';
import { exampleMeals } from './diaryExample';
import { type MealId, mealNames } from './diaryMeals';

type AddEntryResult = { entry: DiaryEntry; error?: undefined } | { entry?: undefined; error: string };

type DiaryContextValue = {
    getDay: (date: string) => readonly DiaryMeal[];
    addEntry: (food: Food, date: string, mealId: MealId, draft: DiaryAmountDraft) => AddEntryResult;
    lastAddedEntry?: DiaryEntry;
    claimConfirmation: (entryId: string) => boolean;
};

const DiaryContext = createContext<DiaryContextValue | undefined>(undefined);

function initialEntries(): readonly DiaryEntry[] {
    const date = toDiaryDate(new Date());
    return exampleMeals.flatMap(meal => meal.entries.map((entry, index) => ({
        id: `example-${meal.id}-${index}`,
        date,
        mealId: meal.id,
        food: { id: `example-food-${meal.id}-${index}`, name: entry.name, referenceUnit: 'g' as const },
        quantity: entry.quantity,
        quantityNote: entry.quantityNote,
        nutrition: { ...entry.nutrition },
    })));
}

export function DiaryProvider({ children }: PropsWithChildren) {
    const [entries, setEntries] = useState(initialEntries);
    const [lastAddedEntry, setLastAddedEntry] = useState<DiaryEntry>();
    const nextId = useRef(1);
    const confirmedEntryId = useRef<string | undefined>(undefined);
    const claimConfirmation = useCallback((entryId: string) => {
        if (confirmedEntryId.current === entryId) return false;
        confirmedEntryId.current = entryId;
        return true;
    }, []);

    function addEntry(food: Food, date: string, mealId: MealId, draft: DiaryAmountDraft): AddEntryResult {
        if (!parseDiaryDate(date) || !isMealId(mealId)) {
            return { error: 'Datum oder Mahlzeit ist ungültig. Bitte im Tagebuch erneut beginnen.' };
        }
        const result = previewDiaryEntry(food, draft);
        if (!result.value) {
            return { error: result.error };
        }
        const entry: DiaryEntry = { ...result.value, id: `local-entry-${nextId.current++}`, date, mealId };
        setEntries(current => [...current, entry]);
        setLastAddedEntry(entry);
        return { entry };
    }

    function getDay(date: string): readonly DiaryMeal[] {
        return (Object.entries(mealNames) as [MealId, string][]).map(([id, name]) => ({
            id, name, entries: entries.filter(entry => entry.date === date && entry.mealId === id),
        }));
    }

    return <DiaryContext value={{ getDay, addEntry, lastAddedEntry, claimConfirmation }}>{children}</DiaryContext>;
}

export function useDiary(): DiaryContextValue {
    const context = useContext(DiaryContext);
    if (!context) {
        throw new Error('useDiary must be used within DiaryProvider');
    }
    return context;
}
