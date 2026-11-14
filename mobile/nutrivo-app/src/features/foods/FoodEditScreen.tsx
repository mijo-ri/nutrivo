import { useFocusEffect, useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback } from 'react';
import { BackHandler, Platform } from 'react-native';

import { ApiError } from '@/shared/api/apiClient';
import { useIsClient } from '@/shared/hooks/useIsClient';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { FoodButton } from './FoodButton';
import { foodToDraft } from './foodForm';
import { FoodForm } from './FoodFormView';
import { FoodPage } from './FoodPage';
import { FoodQueryFeedback } from './FoodQueryFeedback';
import { useFood, useUpdateFood } from './foodsQueries';

export function FoodEditScreen() {
  const { id } = useLocalSearchParams<{ id?: string | string[] }>();
  const router = useRouter();
  const isClient = useIsClient();
  const query = useFood(id);
  const update = useUpdateFood(typeof id === 'string' ? id : '');
  const food = query.data;
  const missing = query.error instanceof ApiError && query.error.status === 404;
  const returnToDetails = useCallback(() => {
    if (typeof id === 'string' && !query.invalidId) {
      router.dismissTo({ pathname: '/food-details', params: { id } });
    } else {
      router.dismissTo('/foods');
    }
  }, [id, query.invalidId, router]);

  useFocusEffect(
    useCallback(() => {
      if (Platform.OS !== 'android') return;
      const listener = BackHandler.addEventListener('hardwareBackPress', () => {
        if (!update.isPending) returnToDetails();
        return true;
      });
      return () => listener.remove();
    }, [update.isPending, returnToDetails]),
  );

  return (
    <FoodPage
      title="Lebensmittel bearbeiten"
      onBack={returnToDetails}
      backLabel="Zurück zu den Lebensmitteldetails"
      backDisabled={update.isPending}
    >
      {isClient &&
        (query.invalidId || missing ? (
          <Card>
            <AppText accessibilityRole="alert">
              {missing
                ? 'Dieses Lebensmittel wurde nicht gefunden.'
                : 'Die Lebensmittel-ID fehlt oder ist ungültig.'}
            </AppText>
            <FoodButton
              label="Zur Lebensmittelübersicht"
              onPress={() => router.dismissTo('/foods')}
            />
          </Card>
        ) : (
          <>
            <FoodQueryFeedback
              loading={query.isPending}
              error={query.error}
              onRetry={query.refetch}
            />
            {food && (
              <FoodForm
                key={food.id}
                initialDraft={foodToDraft(food)}
                saveLabel="Änderungen speichern"
                onSave={async (input) => {
                  await update.mutateAsync(input);
                  returnToDetails();
                }}
                onCancel={returnToDetails}
              />
            )}
          </>
        ))}
    </FoodPage>
  );
}
