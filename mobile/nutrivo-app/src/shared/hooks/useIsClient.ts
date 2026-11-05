import { useSyncExternalStore } from 'react';

const subscribe = () => () => undefined;

// Keep the first browser render consistent with Expo's static HTML. Native
// renders and subsequent client navigation use the client snapshot immediately.
export function useIsClient(): boolean {
    return useSyncExternalStore(subscribe, () => true, () => false);
}
