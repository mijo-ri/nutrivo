import { useId, useRef, useState } from 'react';

import { theme } from '@/shared/theme/theme';

import type { DiaryDatePickerProps } from './DiaryDatePicker.types';
import { DiaryDatePickerDialog } from './DiaryDatePickerDialog';

export function DiaryDatePicker({ value, onConfirm, onCancel }: DiaryDatePickerProps) {
    const inputId = useId();
    const inputRef = useRef<HTMLInputElement>(null);
    const [draftValue, setDraftValue] = useState(() => (
        `${String(value.getFullYear()).padStart(4, '0')}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`
    ));
    const draftDate = parseLocalDate(draftValue);

    return (
        <DiaryDatePickerDialog
            onCancel={onCancel}
            confirmDisabled={!draftDate}
            onConfirm={() => {
                if (draftDate && inputRef.current?.validity.valid) {
                    onConfirm(draftDate);
                }
            }}
        >
            <label htmlFor={inputId} style={{ color: theme.colors.text, fontFamily: theme.typography.bodyMd.fontFamily }}>
                Tagebuchdatum
            </label>
            <input
                ref={inputRef}
                id={inputId}
                type="date"
                required
                value={draftValue}
                onChange={event => setDraftValue(event.currentTarget.value)}
                style={{
                    colorScheme: 'dark',
                    boxSizing: 'border-box',
                    width: '100%',
                    minWidth: 44,
                    minHeight: 44,
                    padding: theme.spacing.sm,
                    border: `1px solid ${theme.colors.outline}`,
                    borderRadius: theme.radii.sm,
                    backgroundColor: theme.colors.surfaceLow,
                    color: theme.colors.text,
                    font: 'inherit',
                    fontSize: 18,
                }}
            />
        </DiaryDatePickerDialog>
    );
}

function parseLocalDate(value: string): Date | null {
    if (!/^\d{4,}-\d{2}-\d{2}$/.test(value)) {
        return null;
    }

    const [year, month, day] = value.split('-').map(Number);
    // setFullYear also handles years 1–99 without the Date constructor's 1900 offset.
    const date = new Date(0);
    date.setFullYear(year, month - 1, day);
    date.setHours(0, 0, 0, 0);

    return year > 0 && date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
        ? date
        : null;
}
