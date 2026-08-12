import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import type { CartItem } from './api';

type CartContextValue = {
  items: CartItem[];
  add: (item: CartItem) => void;
  remove: (productId: string) => void;
  setQty: (productId: string, quantity: number) => void;
  clear: () => void;
  count: number;
};

const CartContext = createContext<CartContextValue | null>(null);
const STORAGE_KEY = 'elsint_cart';

function load(): CartItem[] {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]');
  } catch {
    return [];
  }
}

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>(load);

  const persist = (next: CartItem[]) => {
    setItems(next);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  };

  const value = useMemo<CartContextValue>(() => ({
    items,
    add: (item) => {
      const existing = items.find((i) => i.productId === item.productId);
      if (existing) {
        persist(items.map((i) => i.productId === item.productId ? { ...i, quantity: i.quantity + item.quantity } : i));
      } else {
        persist([...items, item]);
      }
    },
    remove: (productId) => persist(items.filter((i) => i.productId !== productId)),
    setQty: (productId, quantity) => {
      if (quantity <= 0) persist(items.filter((i) => i.productId !== productId));
      else persist(items.map((i) => i.productId === productId ? { ...i, quantity } : i));
    },
    clear: () => persist([]),
    count: items.reduce((sum, i) => sum + i.quantity, 0),
  }), [items]);

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error('useCart must be used within CartProvider');
  return ctx;
}
