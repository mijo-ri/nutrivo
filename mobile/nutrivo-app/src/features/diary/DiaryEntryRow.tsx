import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { useRouter } from 'expo-router';
import { useCallback, useMemo, useRef, useState } from 'react';
import { Animated, PanResponder, Pressable, StyleSheet, View } from 'react-native';
import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';
import { FoodButton } from '../foods/FoodButton';
import { type DiaryEntry, formatDiaryQuantity } from './diary';
import { formatNutrition } from './nutrition';
import { diaryErrorMessage, isUncertainDiaryWrite } from './diaryApi';
import { useReconcileDiaryDay, useRemoveDiaryEntry } from './diaryQueries';

const actionWidth = 104;
export function DiaryEntryRow({
  entry,
  alternate,
}: {
  entry: DiaryEntry;
  alternate: boolean;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [checking, setChecking] = useState(false);
  const [readError, setReadError] = useState<unknown>();
  const [offset] = useState(() => new Animated.Value(0));
  const dragged = useRef(false);
  const submitting = useRef(false);
  const deletion = useRemoveDiaryEntry(entry.id, entry.date);
  const reconcile = useReconcileDiaryDay(entry.date);
  const uncertain = isUncertainDiaryWrite(deletion.error);
  const reveal = useCallback(
    (next: boolean) => {
      setOpen(next);
      Animated.timing(offset, {
        toValue: next ? -actionWidth : 0,
        duration: 160,
        useNativeDriver: false,
      }).start();
    },
    [offset],
  );
  const gesture = useMemo(
    () =>
      // PanResponder stores these callbacks; ref access happens only during gestures.
      // eslint-disable-next-line react-hooks/refs
      PanResponder.create({
        onMoveShouldSetPanResponder: (_, state) =>
          !deletion.isPending &&
          Math.abs(state.dx) > 12 &&
          Math.abs(state.dx) > Math.abs(state.dy) * 1.5,
        onPanResponderGrant: () => {
          dragged.current = true;
          offset.stopAnimation();
        },
        onPanResponderMove: (_, state) =>
          offset.setValue(
            Math.max(-actionWidth, Math.min(0, (open ? -actionWidth : 0) + state.dx)),
          ),
        onPanResponderRelease: (_, state) =>
          reveal((open ? -actionWidth : 0) + state.dx < -actionWidth / 2),
        onPanResponderTerminate: () => reveal(open),
        onPanResponderTerminationRequest: () => true,
      }),
    [open, offset, reveal, deletion.isPending],
  );
  async function remove() {
    if (submitting.current || uncertain) return;
    submitting.current = true;
    try {
      await deletion.mutateAsync();
    } catch {
      /* Keep the row and show the mutation error below. */
    } finally {
      submitting.current = false;
    }
  }
  async function checkDay() {
    setChecking(true);
    setReadError(undefined);
    try {
      await reconcile();
      deletion.reset();
    } catch (error) {
      setReadError(error);
    } finally {
      setChecking(false);
    }
  }
  return (
    <View>
      <View style={styles.clip}>
        <View
          style={styles.action}
          aria-hidden={!open}
          accessibilityElementsHidden={!open}
          importantForAccessibility={open ? 'auto' : 'no-hide-descendants'}
          pointerEvents={open ? 'auto' : 'none'}
        >
          <Pressable
            onPress={() => {
              void remove();
            }}
            disabled={deletion.isPending || uncertain || checking}
            accessibilityRole="button"
            accessibilityLabel={`${entry.food.name} aus Mahlzeit löschen`}
            accessibilityState={{
              disabled: deletion.isPending || uncertain || checking,
              busy: deletion.isPending,
            }}
            style={styles.deleteButton}
          >
            <MaterialDesignIcons
              name="delete-outline"
              size={22}
              color={theme.colors.onPrimary}
              accessible={false}
              aria-hidden
            />
            <AppText style={styles.deleteText}>
              {deletion.isPending ? 'Löscht …' : 'Löschen'}
            </AppText>
          </Pressable>
        </View>
        <Animated.View
          {...gesture.panHandlers}
          style={[
            styles.row,
            alternate && styles.alternate,
            { transform: [{ translateX: offset }] },
          ]}
        >
          <Pressable
            style={styles.entry}
            accessibilityRole="button"
            accessibilityLabel={`${entry.food.name}, ${formatDiaryQuantity(entry)}, ${formatNutrition(entry.nutrition.calories)} Kilokalorien bearbeiten`}
            accessibilityHint="Öffnet die gespeicherte Menge zur Bearbeitung"
            onPressIn={() => {
              dragged.current = false;
            }}
            onPress={() => {
              if (!dragged.current && !deletion.isPending)
                router.push({ pathname: '/diary-entry-edit', params: { id: entry.id } });
            }}
          >
            <View style={styles.food}>
              <AppText>{entry.food.name}</AppText>
              <AppText variant="bodySm" tone="secondary">
                {formatDiaryQuantity(entry)}
              </AppText>
            </View>
            <AppText variant="numericSm" style={styles.calories}>
              {formatNutrition(entry.nutrition.calories)} kcal
            </AppText>
          </Pressable>
          <Pressable
            onPress={() => reveal(!open)}
            accessibilityRole="button"
            accessibilityLabel={`Aktionen für ${entry.food.name}`}
            accessibilityState={{ expanded: open }}
            style={styles.more}
          >
            <MaterialDesignIcons
              name={open ? 'chevron-right' : 'dots-horizontal'}
              size={20}
              color={theme.colors.textSecondary}
              accessible={false}
              aria-hidden
            />
          </Pressable>
        </Animated.View>
      </View>
      {!!(deletion.error || readError) && (
        <View style={styles.feedback}>
          <AppText accessibilityRole="alert">
            {diaryErrorMessage(readError ?? deletion.error)}
          </AppText>
          {uncertain && (
            <FoodButton
              label={checking ? 'Wird geprüft …' : 'Tagebuch neu laden'}
              secondary
              disabled={checking}
              onPress={() => {
                void checkDay();
              }}
            />
          )}
        </View>
      )}
    </View>
  );
}
const styles = StyleSheet.create({
  clip: { overflow: 'hidden' },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: theme.colors.surface,
    minHeight: 56,
  },
  alternate: { backgroundColor: theme.colors.surfaceLow },
  entry: {
    flex: 1,
    minHeight: 56,
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
    paddingLeft: theme.spacing.md,
    paddingVertical: theme.spacing.sm,
  },
  food: { flex: 1, minWidth: 0 },
  calories: { flexShrink: 1, textAlign: 'right', maxWidth: '40%' },
  more: { minWidth: 44, minHeight: 48, alignItems: 'center', justifyContent: 'center' },
  action: {
    position: 'absolute',
    right: 0,
    top: 0,
    bottom: 0,
    width: actionWidth,
    backgroundColor: theme.colors.error,
  },
  deleteButton: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 48,
  },
  deleteText: { color: theme.colors.onPrimary },
  feedback: { padding: theme.spacing.md, gap: theme.spacing.sm },
});
