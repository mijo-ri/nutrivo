import { useEffect, useRef, useState } from 'react';
import { ApiError } from '@/shared/api/apiClient';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';
import { Screen } from '@/shared/ui/Screen';
import { FoodButton } from '../foods/FoodButton';
import { FoodFormField } from '../foods/FoodFormField';
import { TargetQueryFeedback } from './TargetQueryFeedback';
import { type NutritionTargets, type TargetEnvelope } from './targetsApi';
import { useCheckSavedTargets, useSetTargets, useTargets } from './targetsQueries';

const fields = [
  { key: 'kilocalories', label: 'Kalorien (kcal)' },
  { key: 'proteinGrams', label: 'Protein (g)' },
  { key: 'carbohydratesGrams', label: 'Kohlenhydrate (g)' },
  { key: 'fatGrams', label: 'Fett (g)' },
] as const;
type Draft = Record<keyof NutritionTargets, string>;
function toDraft(targets: NutritionTargets | null): Draft {
  return Object.fromEntries(
    fields.map(({ key }) => [
      key,
      targets
        ? targets[key].toLocaleString('en-US', {
            useGrouping: false,
            maximumSignificantDigits: 21,
          })
        : '',
    ]),
  ) as Draft;
}

export function TargetsScreen() {
  const query = useTargets();
  return (
    <Screen>
      <Card>
        <AppText variant="headlineMd" accessibilityRole="header">
          Deine Ernährungsziele
        </AppText>
        <AppText tone="secondary">
          Deine aktuellen täglichen Ziele gelten für alle Tage, auch rückwirkend als
          Vergleich. Kalorien und Makronährstoffe legst du unabhängig fest.
        </AppText>
      </Card>
      <TargetQueryFeedback
        loading={query.isPending}
        error={query.error}
        cached={!!query.data}
        onRetry={query.refetch}
      />
      {query.data && <TargetsForm saved={query.data} />}
    </Screen>
  );
}

function TargetsForm({ saved }: { saved: TargetEnvelope }) {
  const [draft, setDraft] = useState(() => toDraft(saved.targets));
  const dirty = useRef(false);
  const pending = useRef(false);
  const [errors, setErrors] = useState<Partial<Draft>>({});
  const [message, setMessage] = useState('');
  const [uncertain, setUncertain] = useState(false);
  const [checked, setChecked] = useState<TargetEnvelope>();
  const [checking, setChecking] = useState(false);
  const mutation = useSetTargets();
  const checkSaved = useCheckSavedTargets();
  useEffect(() => {
    if (!dirty.current) setDraft(toDraft(saved.targets));
  }, [saved]);

  async function save() {
    if (pending.current || checking) return;
    const nextErrors: Partial<Draft> = {};
    const values = {} as NutritionTargets;
    for (const { key } of fields) {
      const text = draft[key].trim();
      const value = Number(text.replace(',', '.'));
      if (!/^\d+(?:[.,]\d+)?$/.test(text) || !Number.isFinite(value))
        nextErrors[key] = 'Eine gültige, nicht negative Zahl eingeben.';
      else values[key] = value;
    }
    setErrors(nextErrors);
    setMessage('');
    if (Object.keys(nextErrors).length) return;
    pending.current = true;
    setChecked(undefined);
    setUncertain(false);
    try {
      const result = await mutation.mutateAsync(values);
      dirty.current = false;
      setDraft(toDraft(result.targets));
      setMessage('Ziele gespeichert.');
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : 'Die Ziele konnten nicht gespeichert werden.',
      );
      setUncertain(error instanceof ApiError && error.code === 'Targets.OutcomeUnknown');
      if (error instanceof ApiError) {
        const field = fields.find(
          ({ key }) =>
            error.code ===
            `DailyNutritionTarget.${key[0].toUpperCase()}${key.slice(1)}.Negative`,
        );
        if (field) setErrors({ [field.key]: 'Das Ziel darf nicht negativ sein.' });
      }
    } finally {
      pending.current = false;
    }
  }
  async function check() {
    if (pending.current || checking) return;
    setChecking(true);
    try {
      setChecked(await checkSaved());
      setMessage('Gespeicherte Ziele geladen. Dein Entwurf bleibt erhalten.');
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : 'Die gespeicherten Ziele konnten nicht geprüft werden.',
      );
    } finally {
      setChecking(false);
    }
  }
  const disabled = mutation.isPending || checking;
  return (
    <Card>
      {fields.map(({ key, label }) => (
        <FoodFormField
          key={key}
          label={label}
          numeric
          value={draft[key]}
          disabled={disabled}
          error={errors[key]}
          onChangeText={(value) => {
            dirty.current = true;
            setDraft((current) => ({ ...current, [key]: value }));
            setErrors((current) => ({ ...current, [key]: undefined }));
            setMessage('');
            setChecked(undefined);
          }}
        />
      ))}
      <FoodButton
        label={mutation.isPending ? 'Wird gespeichert …' : 'Ziele speichern'}
        disabled={disabled}
        onPress={() => {
          void save();
        }}
      />
      {!!message && (
        <AppText accessibilityRole={mutation.isSuccess ? 'status' : 'alert'}>
          {message}
        </AppText>
      )}
      {uncertain && (
        <FoodButton
          label={checking ? 'Wird geprüft …' : 'Gespeicherte Ziele prüfen'}
          secondary
          disabled={disabled}
          onPress={() => {
            void check();
          }}
        />
      )}
      {checked && (
        <Card>
          <AppText accessibilityRole="header">Gespeicherte Ziele</AppText>
          {checked.targets ? (
            fields.map(({ key, label }) => (
              <AppText key={key}>
                {label}: {checked.targets![key]}
              </AppText>
            ))
          ) : (
            <AppText>Noch keine Ziele festgelegt.</AppText>
          )}
          <FoodButton
            label="Gespeicherte Werte übernehmen"
            secondary
            onPress={() => {
              dirty.current = false;
              setDraft(toDraft(checked.targets));
              setErrors({});
              setMessage('');
              setUncertain(false);
              setChecked(undefined);
            }}
          />
        </Card>
      )}
    </Card>
  );
}
