import { useLocalSearchParams, useRouter } from 'expo-router';
import { type ReactNode } from 'react';
import { FoodPage } from '../foods/FoodPage';
import { FoodButton } from '../foods/FoodButton';
import { AppText } from '@/shared/ui/AppText';
import { useIsClient } from '@/shared/hooks/useIsClient';
import { DiaryAmountForm } from './DiaryAmountForm';
import { type DiaryDetail } from './diaryApi';
import { useDiaryEntry, useUpdateDiaryEntry } from './diaryQueries';
import { DiaryQueryFeedback } from './DiaryQueryFeedback';
import { parseDiaryDate } from './diary';
import { useDiary } from './DiaryProvider';

const dateFormat = new Intl.DateTimeFormat('de-CH', { dateStyle: 'long' });
export function DiaryEntryEditScreen() {
  const { id } = useLocalSearchParams<{ id?: string | string[] }>();
  const router = useRouter();
  const isClient = useIsClient();
  const query = useDiaryEntry(id);
  const { setSelectedDate } = useDiary();
  function goBack() {
    if (query.data) setSelectedDate(parseDiaryDate(query.data.entry.date)!);
    if (router.canGoBack()) router.back();
    else router.replace('/');
  }
  const feedback = (
    <>
      {isClient &&
        (query.invalidId ? (
          <AppText accessibilityRole="alert">Ungültiger Tagebucheintrag.</AppText>
        ) : (
          <DiaryQueryFeedback
            loading={query.isPending || (!query.isFetchedAfterMount && !query.isError)}
            error={query.error}
            onRetry={query.refetch}
          />
        ))}
      {isClient && (query.invalidId || query.isError) && (
        <FoodButton label="Zum Tagebuch" secondary onPress={goBack} />
      )}
    </>
  );

  if (
    isClient &&
    query.data &&
    query.isFetchedAfterMount &&
    !(query.error && 'status' in query.error && query.error.status === 404)
  ) {
    return (
      <EditForm
        key={query.data.entry.id}
        detail={query.data}
        onCancel={goBack}
        feedback={feedback}
      />
    );
  }

  return (
    <FoodPage title="Menge bearbeiten" onBack={goBack} backLabel="Zurück ohne Änderung">
      {feedback}
    </FoodPage>
  );
}
function EditForm({
  detail,
  onCancel,
  feedback,
}: {
  detail: DiaryDetail;
  onCancel: () => void;
  feedback: ReactNode;
}) {
  const { entry, food, initialDraft } = detail;
  const router = useRouter();
  const mutation = useUpdateDiaryEntry(entry.id, entry.date);
  const { setSelectedDate } = useDiary();
  function returnToDay() {
    setSelectedDate(parseDiaryDate(entry.date)!);
    router.dismissTo('/');
  }
  return (
    <DiaryAmountForm
      food={food}
      mealId={entry.mealId}
      dateLabel={dateFormat.format(parseDiaryDate(entry.date)!)}
      initialDraft={initialDraft}
      editing
      feedback={feedback}
      onCancel={onCancel}
      onSubmit={(draft) => mutation.mutateAsync(draft)}
      onSaved={returnToDay}
      onReconcile={returnToDay}
    />
  );
}
