import { ActivityIndicator, View } from 'react-native';
import { AppText } from '@/shared/ui/AppText';
import { FoodButton } from '../foods/FoodButton';
import { diaryErrorMessage } from './diaryApi';

export function DiaryQueryFeedback({
  loading,
  error,
  onRetry,
}: {
  loading?: boolean;
  error?: unknown;
  onRetry: () => unknown;
}) {
  if (!loading && !error) return null;
  return (
    <View style={{ gap: 12 }}>
      {loading && <ActivityIndicator accessibilityLabel="Tagebuch wird geladen" />}
      {!!error && (
        <>
          <AppText accessibilityRole="alert">{diaryErrorMessage(error)}</AppText>
          <FoodButton
            label="Erneut laden"
            secondary
            onPress={() => {
              void onRetry();
            }}
          />
        </>
      )}
    </View>
  );
}
