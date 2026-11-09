import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useFocusEffect } from 'expo-router';
import { useCallback, useEffect, useState } from 'react';

import { useIsClient } from '@/shared/hooks/useIsClient';

import type { FoodInput } from './food';
import { createFood, getFood, searchFoods, updateFood } from './foodsApi';

const foodKeys = {
    lists: ['foods', 'list'] as const,
    list: (search: string) => ['foods', 'list', search] as const,
    detail: (id: string | undefined) => ['foods', 'detail', id] as const,
};

function useScreenActive() {
    const isClient = useIsClient();
    const [focused, setFocused] = useState(false);
    useFocusEffect(useCallback(() => {
        setFocused(true);
        return () => setFocused(false);
    }, []));
    return isClient && focused;
}

export function useFoodSearch(search: string, enabled = true) {
    const active = useScreenActive();
    const term = search.trim();
    const [debounced, setDebounced] = useState(term);
    useEffect(() => {
        const timer = setTimeout(() => setDebounced(term), 300);
        return () => clearTimeout(timer);
    }, [term]);
    const isDebouncing = term !== debounced;
    const query = useInfiniteQuery({
        queryKey: foodKeys.list(term),
        queryFn: ({ pageParam, signal }) => searchFoods(term, pageParam, signal),
        initialPageParam: 1,
        getNextPageParam: last => last.items.length > 0 && last.page * last.pageSize < last.totalCount
            ? last.page + 1 : undefined,
        enabled: active && enabled && !isDebouncing,
    });
    const visible = enabled && !isDebouncing;
    return {
        foods: visible ? query.data?.pages.flatMap(page => page.items) ?? [] : [],
        totalCount: visible ? query.data?.pages[0]?.totalCount : undefined,
        loading: enabled && (isDebouncing || query.isPending),
        error: visible && !query.isFetchNextPageError ? query.error : null,
        moreError: visible && query.isFetchNextPageError ? query.error : null,
        loadingMore: visible && query.isFetchingNextPage,
        retry: () => query.isFetchNextPageError ? query.fetchNextPage({ cancelRefetch: false }) : query.refetch({ cancelRefetch: false }),
        loadMore: () => {
            if (active && visible && query.hasNextPage && !query.isFetching && !query.isError) {
                void query.fetchNextPage({ cancelRefetch: false });
            }
        },
    };
}

export function useFood(id: string | string[] | undefined, enabled = true) {
    const active = useScreenActive();
    const validId = typeof id === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)
        ? id : undefined;
    const query = useQuery({
        queryKey: foodKeys.detail(validId),
        queryFn: ({ signal }) => getFood(validId!, signal),
        enabled: active && enabled && validId !== undefined,
    });
    return { ...query, invalidId: validId === undefined };
}

export function useCreateFood() {
    const client = useQueryClient();
    return useMutation({
        mutationFn: createFood,
        retry: false,
        onSuccess: () => { void client.invalidateQueries({ queryKey: foodKeys.lists }); },
    });
}

export function useUpdateFood(id: string) {
    const client = useQueryClient();
    return useMutation({
        mutationFn: (input: FoodInput) => updateFood(id, input),
        retry: false,
        onSuccess: async () => {
            await client.cancelQueries({ queryKey: foodKeys.detail(id) });
            await Promise.all([
                client.invalidateQueries({ queryKey: foodKeys.detail(id), refetchType: 'all' }),
                client.invalidateQueries({ queryKey: foodKeys.lists }),
            ]);
        },
    });
}
