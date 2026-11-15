import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useFocusEffect } from 'expo-router';
import { useCallback } from 'react';
import { useIsClient } from '@/shared/hooks/useIsClient';
import { getTargets, setTargets } from './targetsApi';

const key = ['nutrition-targets', 'current'] as const;
export function useTargets() {
  const client = useQueryClient();
  const active = useIsClient();
  const query = useQuery({
    queryKey: key,
    queryFn: ({ signal }) => getTargets(signal),
    enabled: active,
  });
  useFocusEffect(
    useCallback(() => {
      if (active) void client.invalidateQueries({ queryKey: key });
    }, [active, client]),
  );
  return query;
}
export function useSetTargets() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: setTargets,
    retry: false,
    onSuccess: async (saved) => {
      await client.cancelQueries({ queryKey: key });
      client.setQueryData(key, saved);
    },
  });
}
export function useCheckSavedTargets() {
  const client = useQueryClient();
  return () =>
    client.fetchQuery({
      queryKey: key,
      queryFn: ({ signal }) => getTargets(signal),
      staleTime: 0,
    });
}
