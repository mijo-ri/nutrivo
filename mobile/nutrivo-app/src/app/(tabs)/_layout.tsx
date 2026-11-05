import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { Tabs } from 'expo-router';
import { StyleSheet, useWindowDimensions } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { theme } from '@/shared/theme/theme';
import { AppHeader } from '@/shared/ui/AppHeader';
import { AppText } from '@/shared/ui/AppText';

export const unstable_settings = {
    anchor: 'index',
};

export default function AppTabs() {
    const insets = useSafeAreaInsets();
    const { fontScale } = useWindowDimensions();

    return (
        <Tabs
            backBehavior="initialRoute"
            screenOptions={({ route }) => ({
                header: ({ options }) => (
                    <AppHeader title={options.title ?? route.name} />
                ),
                sceneStyle: { backgroundColor: theme.colors.background },
                tabBarPosition: 'bottom',
                tabBarLabelPosition: 'below-icon',
                tabBarActiveTintColor: theme.colors.primary,
                tabBarInactiveTintColor: theme.colors.textSecondary,
                tabBarLabel: ({ color, children }) => (
                    <AppText
                        accessible={false}
                        variant="bodySm"
                        style={[styles.tabLabel, { color }]}
                    >
                        {children}
                    </AppText>
                ),
                tabBarStyle: [
                    styles.tabBar,
                    { height: Math.ceil(56 * Math.max(1, fontScale)) + insets.bottom },
                ],
                tabBarItemStyle: styles.tabItem,
            })}
        >
            <Tabs.Screen
                name="index"
                options={{
                    title: 'Tagebuch',
                    tabBarAccessibilityLabel: 'Tagebuch',
                    tabBarIcon: ({ color }) => (
                        <MaterialDesignIcons
                            name="calendar-blank-outline"
                            size={22}
                            color={color}
                            accessible={false}
                            aria-hidden
                        />
                    ),
                }}
            />
            <Tabs.Screen
                name="foods"
                options={{
                    title: 'Lebensmittel',
                    tabBarAccessibilityLabel: 'Lebensmittel',
                    tabBarIcon: ({ color }) => (
                        <MaterialDesignIcons
                            name="food-apple-outline"
                            size={22}
                            color={color}
                            accessible={false}
                            aria-hidden
                        />
                    ),
                }}
            />
            <Tabs.Screen
                name="targets"
                options={{
                    title: 'Ziele',
                    tabBarAccessibilityLabel: 'Ziele',
                    tabBarIcon: ({ color }) => (
                        <MaterialDesignIcons
                            name="tune"
                            size={22}
                            color={color}
                            accessible={false}
                            aria-hidden
                        />
                    ),
                }}
            />
        </Tabs>
    );
}

const styles = StyleSheet.create({
    tabBar: {
        backgroundColor: theme.colors.background,
        borderTopColor: theme.colors.outlineVariant,
        elevation: 0,
        shadowOpacity: 0,
    },
    tabItem: {
        flex: 1,
        minHeight: 44,
    },
    tabLabel: {
        textAlign: 'center',
        width: '100%',
    },
});
