import { useState, useCallback } from 'react';

/**
 * useLocalList — manages a local list with add/update/remove operations,
 * useful for optimistic UI updates without re-fetching.
 *
 * Usage:
 *   const list = useLocalList(initialItems);
 *   list.items           // current array
 *   list.add(item)       // prepend to list
 *   list.update(id, data)// merge data into item with matching id
 *   list.remove(id)      // remove by id
 *   list.reset(items)    // replace entire list
 */
export function useLocalList(initialItems = []) {
  const [items, setItems] = useState(initialItems);

  const add    = useCallback((item)         => setItems(p => [item, ...p]), []);
  const update = useCallback((id, data)     => setItems(p => p.map(i => i.id === id ? { ...i, ...data } : i)), []);
  const remove = useCallback((id)           => setItems(p => p.filter(i => i.id !== id)), []);
  const reset  = useCallback((newItems = [])=> setItems(newItems), []);

  return { items, add, update, remove, reset };
}

/**
 * useToggle — boolean toggle.
 * Usage: const [open, toggleOpen, setOpen] = useToggle(false);
 */
export function useToggle(initial = false) {
  const [value, setValue] = useState(initial);
  const toggle = useCallback(() => setValue(v => !v), []);
  return [value, toggle, setValue];
}

/**
 * usePrevious — returns the previous value of a state.
 */
import { useRef, useEffect } from 'react';

export function usePrevious(value) {
  const ref = useRef(undefined);
  useEffect(() => { ref.current = value; });
  return ref.current;
}
