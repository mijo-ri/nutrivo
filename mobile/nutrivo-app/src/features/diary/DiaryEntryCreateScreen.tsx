import { useLocalSearchParams, useRouter } from 'expo-router';
import { type ReactNode } from 'react';

import { FoodButton } from '@/features/foods/FoodButton';
import { FoodPage } from '@/features/foods/FoodPage';
import { useFood } from '@/features/foods/foodsQueries';
import { FoodQueryFeedback } from '@/features/foods/FoodQueryFeedback';
import { useIsClient } from '@/shared/hooks/useIsClient';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { isMealId, parseDiaryDate } from './diary';
import { type MealId } from './diaryMeals';
import { type Food } from '../foods/food';
import { DiaryAmountForm } from './DiaryAmountForm';
import { useLogDiaryFood } from './diaryQueries';
import { useDiary } from './DiaryProvider';

const dateFormat = new Intl.DateTimeFormat('de-CH', { dateStyle: 'long' });

export function DiaryEntryCreateScreen() {
  const { foodId, meal, date } = useLocalSearchParams<{
    foodId?: string | string[];
    meal?: string | string[];
    date?: string | string[];
  }>();
  const router = useRouter();
  const isClient = useIsClient();
  const parsedDate = parseDiaryDate(date);
  const validContext = isMealId(meal) && typeof date === 'string' && !!parsedDate;
  const query = useFood(foodId, validContext);
  const food = query.data;
  const valid = food && isMealId(meal) && typeof date === 'string' && parsedDate;

  function goBack() {
    if (router.canGoBack()) {
      router.back();
    } else {
      router.replace('/');
    }
  }

  const feedback = isClient && validContext && !query.invalidId && (
    <FoodQueryFeedback
      loading={query.isPending}
      error={query.error}
      onRetry={query.refetch}
    />
  );

  if (isClient && valid) {
    return (
      <CreateEntryForm
        key={`${food.id}-${meal}-${date}`}
        food={food}
        mealId={meal}
        date={date}
        feedback={feedback}
        onCancel={goBack}
        onAdded={() =>
          router.dismissTo({ pathname: '/food-selection', params: { meal, date } })
        }
      />
    );
  }

  return (
    <FoodPage title="Menge erfassen" onBack={goBack} backLabel="Zurück ohne Hinzufügen">
      {feedback}
      {isClient && (!validContext || query.invalidId) && (
        <Card>
          <AppText variant="headlineMd" accessibilityRole="header">
            Eintrag nicht möglich
          </AppText>
          <AppText tone="secondary">
            Lebensmittel, Datum oder Mahlzeit ist nicht verfügbar. Bitte wähle im Tagebuch
            erneut eine Mahlzeit und ein Lebensmittel aus.
          </AppText>
          <FoodButton label="Zum Tagebuch" onPress={() => router.dismissTo('/')} />
        </Card>
      )}
    </FoodPage>
  );
}

function CreateEntryForm({
  food,
  mealId,
  date,
  feedback,
  onCancel,
  onAdded,
}: {
  food: Food;
  mealId: MealId;
  date: string;
  feedback?: ReactNode;
  onCancel: () => void;
  onAdded: () => void;
}) {
  const router = useRouter();
  const mutation = useLogDiaryFood(date, mealId, food);
  const { confirmAdded, setSelectedDate } = useDiary();
  return (
    <DiaryAmountForm
      food={food}
      mealId={mealId}
      dateLabel={dateFormat.format(parseDiaryDate(date)!)}
      feedback={feedback}
      onCancel={onCancel}
      onSaved={onAdded}
      onSubmit={async (draft) => {
        const result = await mutation.mutateAsync(draft);
        confirmAdded({ id: result.id, date, mealId, food });
      }}
      onReconcile={() => {
        setSelectedDate(parseDiaryDate(date)!);
        router.dismissTo('/');
      }}
    />
  );
}
