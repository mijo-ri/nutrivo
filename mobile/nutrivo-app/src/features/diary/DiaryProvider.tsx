import {
  createContext,
  type PropsWithChildren,
  useCallback,
  useContext,
  useRef,
  useState,
} from 'react';
import { type DiaryEntry } from './diary';

type AddedEntry = Pick<DiaryEntry, 'id' | 'date' | 'mealId' | 'food'>;
type DiaryContextValue = {
  selectedDate: Date;
  setSelectedDate: (date: Date | ((previous: Date) => Date)) => void;
  lastAddedEntry?: AddedEntry;
  confirmAdded: (entry: AddedEntry) => void;
  claimConfirmation: (id: string) => boolean;
};
const DiaryContext = createContext<DiaryContextValue | undefined>(undefined);
export function DiaryProvider({ children }: PropsWithChildren) {
  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const [lastAddedEntry, confirmAdded] = useState<AddedEntry>();
  const confirmed = useRef<string | undefined>(undefined);
  const claimConfirmation = useCallback((id: string) => {
    if (confirmed.current === id) return false;
    confirmed.current = id;
    return true;
  }, []);
  return (
    <DiaryContext
      value={{
        selectedDate,
        setSelectedDate,
        lastAddedEntry,
        confirmAdded,
        claimConfirmation,
      }}
    >
      {children}
    </DiaryContext>
  );
}
export function useDiary(): DiaryContextValue {
  const context = useContext(DiaryContext);
  if (!context) throw new Error('useDiary must be used within DiaryProvider');
  return context;
}
