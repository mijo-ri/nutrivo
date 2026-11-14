import { focusManager, QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { type PropsWithChildren, useEffect, useState } from 'react';
import { AppState, Platform } from 'react-native';

import { ApiError } from '@/shared/api/apiClient';

export function FoodsProvider({ children }: PropsWithChildren) {
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            retry: (count, error) =>
              count < 1 &&
              !(
                error instanceof ApiError &&
                error.status !== undefined &&
                error.status < 500
              ),
          },
          mutations: { retry: false },
        },
      }),
  );

  useEffect(() => {
    if (Platform.OS === 'web') return;
    focusManager.setFocused(AppState.currentState === 'active');
    const subscription = AppState.addEventListener('change', (state) => {
      focusManager.setFocused(state === 'active');
    });
    return () => subscription.remove();
  }, []);

  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}
