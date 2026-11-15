import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';
import { FoodButton } from '../foods/FoodButton';

export function TargetQueryFeedback({
  loading,
  error,
  cached,
  onRetry,
}: {
  loading: boolean;
  error: Error | null;
  cached: boolean;
  onRetry: () => unknown;
}) {
  if (!loading && !error) return null;
  return (
    <Card>
      {loading && (
        <AppText accessibilityRole="progressbar">Ziele werden geladen …</AppText>
      )}
      {error && (
        <>
          <AppText accessibilityRole="alert">
            {cached
              ? 'Die angezeigten Ziele sind möglicherweise nicht mehr aktuell. '
              : ''}
            {error.message}
          </AppText>
          <FoodButton
            label="Ziele erneut laden"
            secondary
            onPress={() => {
              void onRetry();
            }}
          />
        </>
      )}
    </Card>
  );
}
