import { ActivityIndicator, StyleSheet, View } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import { FoodButton } from './FoodButton';
import { foodErrorMessage } from './foodsApi';

export function FoodQueryFeedback({
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
    <View style={styles.feedback}>
      {loading && (
        <ActivityIndicator
          color={theme.colors.primary}
          accessibilityLabel="Lebensmittel werden geladen"
        />
      )}
      {!!error && (
        <>
          <AppText accessibilityRole="alert">{foodErrorMessage(error)}</AppText>
          <FoodButton
            label="Erneut versuchen"
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

const styles = StyleSheet.create({
  feedback: { paddingVertical: theme.spacing.lg, gap: theme.spacing.md },
});
