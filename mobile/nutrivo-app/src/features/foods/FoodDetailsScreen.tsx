import { useFocusEffect, useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useEffect, useRef, useState } from 'react';
import { BackHandler, Platform } from 'react-native';

import { ApiError } from '@/shared/api/apiClient';
import { useIsClient } from '@/shared/hooks/useIsClient';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { formatFoodNumber, formatFoodQuantity } from './food';
import { FoodButton } from './FoodButton';
import { FoodDeleteDialog } from './FoodDeleteDialog';
import { FoodPage } from './FoodPage';
import { FoodQueryFeedback } from './FoodQueryFeedback';
import { foodErrorMessage } from './foodsApi';
import { useDeleteFood, useDiscardMissingFood, useFood } from './foodsQueries';

export function FoodDetailsScreen() {
  const { id } = useLocalSearchParams<{ id?: string | string[] }>();
  return <FoodDetails key={typeof id === 'string' ? id : 'invalid'} id={id} />;
}

type DeleteStatus =
  'ready' | 'pending' | 'deleted' | 'missing' | 'uncertain' | 'checking';

function FoodDetails({ id }: { id: string | string[] | undefined }) {
  const router = useRouter();
  const isClient = useIsClient();
  const [status, setStatus] = useState<DeleteStatus>('ready');
  const [dialogFood, setDialogFood] = useState<{ id: string; name: string }>();
  const [error, setError] = useState<string>();
  const submitting = useRef(false);
  const focused = useRef(false);
  const mounted = useRef(false);
  const query = useFood(id, status === 'ready');
  const deletion = useDeleteFood();
  const discardMissingFood = useDiscardMissingFood();
  const food = query.data;
  const pending = status === 'pending';
  const readMissing = query.error instanceof ApiError && query.error.status === 404;
  const missing = status === 'missing' || readMissing;
  const uncertain = status === 'uncertain' || status === 'checking';

  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);

  // Remember confirmed absence before evicting the query, including stale cached details.
  if (readMissing && status === 'ready') {
    setStatus('missing');
    setDialogFood(undefined);
  }

  useFocusEffect(
    useCallback(() => {
      focused.current = true;
      return () => {
        focused.current = false;
      };
    }, []),
  );

  useFocusEffect(
    useCallback(() => {
      if (Platform.OS !== 'android') return;
      const listener = BackHandler.addEventListener(
        'hardwareBackPress',
        () => submitting.current,
      );
      return () => listener.remove();
    }, []),
  );

  useEffect(() => {
    if (status === 'missing' && typeof id === 'string') {
      void discardMissingFood(id);
    }
  }, [status, id, discardMissingFood]);

  async function confirmDelete() {
    if (submitting.current || !dialogFood || missing || status !== 'ready') return;
    const target = dialogFood;
    submitting.current = true;
    setStatus('pending');
    setError(undefined);
    try {
      await deletion.mutateAsync(target.id);
      if (!mounted.current) return;
      setStatus(focused.current ? 'deleted' : 'missing');
      setDialogFood(undefined);
      if (focused.current) router.dismissTo('/foods');
    } catch (failure) {
      if (!mounted.current) return;
      setDialogFood(undefined);
      setError(foodErrorMessage(failure));
      setStatus(
        failure instanceof ApiError &&
          failure.status === 404 &&
          failure.code === 'DeleteFood.NotFound'
          ? 'missing'
          : failure instanceof ApiError && failure.code === 'DeleteFood.OutcomeUnknown'
            ? 'uncertain'
            : 'ready',
      );
    } finally {
      submitting.current = false;
    }
  }

  async function checkAvailability() {
    if (submitting.current) return;
    submitting.current = true;
    setStatus('checking');
    try {
      await query.refetch({ throwOnError: true });
      if (!mounted.current) return;
      setStatus('ready');
      setError(undefined);
    } catch (failure) {
      if (
        failure instanceof ApiError &&
        failure.status === 404 &&
        typeof id === 'string'
      ) {
        await discardMissingFood(id);
        if (mounted.current) setStatus('missing');
      } else if (mounted.current) {
        setStatus('uncertain');
        setError(
          `Die Verfügbarkeit konnte nicht geprüft werden. ${foodErrorMessage(failure)}`,
        );
      }
    } finally {
      submitting.current = false;
    }
  }

  return (
    <FoodPage title="Lebensmitteldetails" backDisabled={pending || status === 'checking'}>
      {isClient &&
        (query.invalidId || missing ? (
          <Card>
            <AppText accessibilityRole="alert">
              {missing
                ? 'Dieses Lebensmittel ist nicht mehr verfügbar.'
                : 'Die Lebensmittel-ID fehlt oder ist ungültig.'}
            </AppText>
            <FoodButton
              label="Zur Lebensmittelübersicht"
              onPress={() => router.dismissTo('/foods')}
            />
          </Card>
        ) : (
          <>
            {!uncertain && status !== 'deleted' && (
              <FoodQueryFeedback
                loading={query.isPending}
                error={query.error}
                onRetry={query.refetch}
              />
            )}
            {!!error && <AppText accessibilityRole="alert">{error}</AppText>}
            {uncertain && (
              <Card>
                <AppText>
                  Das Lebensmittel wurde möglicherweise bereits gelöscht. Bitte prüfe die
                  Verfügbarkeit vor einem neuen Löschversuch.
                </AppText>
                <FoodButton
                  label={
                    status === 'checking'
                      ? 'Verfügbarkeit wird geprüft …'
                      : 'Verfügbarkeit prüfen'
                  }
                  disabled={status === 'checking'}
                  secondary
                  onPress={() => {
                    void checkAvailability();
                  }}
                />
              </Card>
            )}
            {food && status !== 'deleted' && (
              <>
                <Card>
                  <AppText variant="headlineLg" accessibilityRole="header">
                    {food.name}
                  </AppText>
                  {food.brand && (
                    <AppText variant="bodyLg" tone="secondary">
                      {food.brand}
                    </AppText>
                  )}
                </Card>
                <Card>
                  <AppText variant="headlineMd" accessibilityRole="header">
                    Nährwerte
                  </AppText>
                  <AppText tone="secondary">
                    Pro {formatFoodQuantity(food.referenceQuantity)} {food.referenceUnit}
                  </AppText>
                  <AppText variant="numericLg" tone="accent">
                    {formatFoodNumber(food.kilocalories)} kcal
                  </AppText>
                  <AppText>Protein: {formatFoodNumber(food.proteinGrams)} g</AppText>
                  <AppText>
                    Kohlenhydrate: {formatFoodNumber(food.carbohydratesGrams)} g
                  </AppText>
                  <AppText>Fett: {formatFoodNumber(food.fatGrams)} g</AppText>
                </Card>
                <Card>
                  <AppText variant="headlineMd" accessibilityRole="header">
                    Portionsgrössen
                  </AppText>
                  {food.servingSizes.length === 0 && (
                    <AppText tone="secondary">
                      Keine zusätzlichen Portionsgrössen erfasst.
                    </AppText>
                  )}
                  {food.servingSizes.map((serving) => (
                    <AppText key={serving.id}>
                      {formatFoodQuantity(serving.quantity)} {serving.unit}
                      {serving.label ? ` (${serving.label})` : ''} ={' '}
                      {formatFoodQuantity(serving.equivalentQuantity)}{' '}
                      {food.referenceUnit}
                    </AppText>
                  ))}
                </Card>
                <FoodButton
                  label="Bearbeiten"
                  disabled={pending || uncertain}
                  onPress={() =>
                    router.push({ pathname: '/food-edit', params: { id: food.id } })
                  }
                />
                <FoodButton
                  label="Löschen"
                  destructive
                  disabled={pending || uncertain}
                  onPress={() => {
                    setError(undefined);
                    setDialogFood({ id: food.id, name: food.name });
                  }}
                />
              </>
            )}
          </>
        ))}
      {dialogFood && !missing && (
        <FoodDeleteDialog
          name={dialogFood.name}
          pending={pending}
          onCancel={() => {
            if (!submitting.current) setDialogFood(undefined);
          }}
          onConfirm={() => {
            void confirmDelete();
          }}
        />
      )}
    </FoodPage>
  );
}
