import type { PropsWithChildren } from 'react';
import { ScrollView, StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { theme } from '@/shared/theme/theme';

export function Screen({ children }: PropsWithChildren) {
    return (
        <SafeAreaView style={styles.screen}>
            <ScrollView
                style={styles.scroll}
                contentContainerStyle={styles.content}
            >
                {children}
            </ScrollView>
        </SafeAreaView>
    );
}

const styles = StyleSheet.create({
    screen: {
        flex: 1,
        backgroundColor: theme.colors.background,
    },
    scroll: { flex: 1 },
    content: {
        flexGrow: 1,
        width: '100%',
        maxWidth: 640,
        alignSelf: 'center',
        padding: theme.spacing.screen,
        gap: theme.spacing.xl,
    },
});
