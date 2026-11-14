import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useFocusEffect } from 'expo-router';
import { useCallback } from 'react';
import { ApiError } from '@/shared/api/apiClient';
import { useIsClient } from '@/shared/hooks/useIsClient';
import { type Food } from '../foods/food';
import { type DiaryAmountDraft } from './diary';
import { type MealId } from './diaryMeals';
import {
  getDiaryDay,
  getDiaryEntry,
  logDiaryFood,
  removeDiaryEntry,
  updateDiaryEntry,
} from './diaryApi';

const keys = {
  day: (date: string) => ['diary', 'day', date] as const,
  entry: (id: string | undefined) => ['diary', 'entry', id?.toLowerCase()] as const,
};
export function useDiaryDay(date: string) {
  const client = useQueryClient();
  const active = useIsClient();
  const query = useQuery({
    queryKey: keys.day(date),
    queryFn: ({ signal }) => getDiaryDay(date, signal),
    enabled: active,
  });
  useFocusEffect(
    useCallback(() => {
      if (active) void client.invalidateQueries({ queryKey: keys.day(date) });
    }, [active, client, date]),
  );
  return query;
}
export function useDiaryEntry(id: string | string[] | undefined) {
  const client = useQueryClient();
  const active = useIsClient();
  const valid =
    typeof id === 'string' &&
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)
      ? id
      : undefined;
  const query = useQuery({
    queryKey: keys.entry(valid),
    staleTime: 0,
    refetchOnMount: 'always',
    queryFn: ({ signal }) => getDiaryEntry(valid!, signal),
    enabled: active && !!valid,
  });
  useFocusEffect(
    useCallback(() => {
      if (active && valid) void client.invalidateQueries({ queryKey: keys.entry(valid) });
    }, [active, client, valid]),
  );
  return { ...query, invalidId: !valid };
}
export function useLogDiaryFood(date: string, meal: MealId, food: Food) {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (draft: DiaryAmountDraft) => logDiaryFood(food, date, meal, draft),
    retry: false,
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: keys.day(date) });
    },
  });
}
export function useUpdateDiaryEntry(id: string, date: string) {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (draft: DiaryAmountDraft) => updateDiaryEntry(id, draft),
    retry: false,
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: keys.day(date) });
      void client.invalidateQueries({ queryKey: keys.entry(id) });
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 404)
        void client.invalidateQueries({ queryKey: keys.day(date) });
    },
  });
}
export function useRemoveDiaryEntry(id: string, date: string) {
  const client = useQueryClient();
  const refresh = () => {
    void client.invalidateQueries({ queryKey: keys.day(date) });
    client.removeQueries({ queryKey: keys.entry(id) });
  };
  return useMutation({
    mutationFn: () => removeDiaryEntry(id),
    retry: false,
    onSuccess: refresh,
    onError: (error) => {
      if (error instanceof ApiError && error.status === 404) refresh();
    },
  });
}
export function useReconcileDiaryDay(date: string) {
  const client = useQueryClient();
  return () =>
    client.fetchQuery({
      queryKey: keys.day(date),
      queryFn: ({ signal }) => getDiaryDay(date, signal),
      staleTime: 0,
    });
}
