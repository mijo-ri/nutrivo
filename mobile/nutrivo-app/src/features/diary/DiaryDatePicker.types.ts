export type DiaryDatePickerProps = {
  value: Date;
  onConfirm: (date: Date) => void;
  onCancel: () => void;
};
