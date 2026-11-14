import DateTimePicker, {
  DateTimePickerAndroid,
} from '@react-native-community/datetimepicker';
import { useEffect, useRef, useState } from 'react';
import { Platform, ScrollView } from 'react-native';

import { theme } from '@/shared/theme/theme';

import type { DiaryDatePickerProps } from './DiaryDatePicker.types';
import { DiaryDatePickerDialog } from './DiaryDatePickerDialog';

export function DiaryDatePicker(props: DiaryDatePickerProps) {
  return Platform.OS === 'android' ? (
    <AndroidDiaryDatePicker {...props} />
  ) : (
    <IosDiaryDatePicker {...props} />
  );
}

function AndroidDiaryDatePicker(props: DiaryDatePickerProps) {
  // One mounted instance represents one dialog, even if the screen rerenders.
  const initialProps = useRef(props);

  useEffect(() => {
    const { value, onConfirm, onCancel } = initialProps.current;
    let active = true;

    DateTimePickerAndroid.open({
      value,
      mode: 'date',
      display: 'default',
      onValueChange: (_event, date) => {
        if (active) {
          active = false;
          onConfirm(date);
        }
      },
      onDismiss: () => {
        if (active) {
          active = false;
          onCancel();
        }
      },
      onError: (error) => {
        console.error('Die Datumsauswahl konnte nicht geöffnet werden.', error);
        if (active) {
          active = false;
          onCancel();
        }
      },
    });

    return () => {
      if (active) {
        active = false;
        void DateTimePickerAndroid.dismiss('date');
      }
    };
  }, []);

  return null;
}

function IosDiaryDatePicker({ value, onConfirm, onCancel }: DiaryDatePickerProps) {
  const [draftDate, setDraftDate] = useState(() => new Date(value));

  return (
    <DiaryDatePickerDialog onCancel={onCancel} onConfirm={() => onConfirm(draftDate)}>
      {/* The native inline calendar has an intrinsic width, even in narrow windows. */}
      <ScrollView
        horizontal
        contentContainerStyle={{ flexGrow: 1, justifyContent: 'center' }}
      >
        <DateTimePicker
          value={draftDate}
          mode="date"
          display="inline"
          themeVariant="dark"
          accentColor={theme.colors.primary}
          onValueChange={(_event, date) => setDraftDate(date)}
        />
      </ScrollView>
    </DiaryDatePickerDialog>
  );
}
