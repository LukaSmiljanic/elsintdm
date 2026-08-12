export const API_BASE = import.meta.env.VITE_API_URL ?? '';

export async function api<T>(path: string, options?: RequestInit): Promise<T> {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...(options?.headers ?? {}),
  };
  const token = localStorage.getItem('elsint_token');
  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${path}`, { ...options, headers });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) throw sessionExpired();
    const body = await res.json().catch(() => ({}));
    throw new Error(body.message ?? `HTTP ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

/**
 * An expired admin token still sits in localStorage, so without this the admin
 * pages would silently render empty tables as if the data was gone.
 */
function sessionExpired() {
  localStorage.removeItem('elsint_token');
  if (window.location.pathname.startsWith('/admin') && !window.location.pathname.endsWith('/login')) {
    window.location.href = '/admin/login?expired=1';
  }
  return new Error('Sesija je istekla. Prijavite se ponovo.');
}

/** Multipart POST — the browser sets Content-Type (with boundary) itself. */
export async function apiUpload<T>(path: string, form: FormData): Promise<T> {
  const headers: Record<string, string> = {};
  const token = localStorage.getItem('elsint_token');
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const res = await fetch(`${API_BASE}${path}`, { method: 'POST', body: form, headers });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) throw sessionExpired();
    const body = await res.json().catch(() => ({}));
    throw new Error(body.message ?? `HTTP ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export type Category = {
  id: string;
  name: string;
  slug: string;
  description?: string;
  metaTitle?: string;
  metaDescription?: string;
};

export type ProductListItem = {
  id: string;
  name: string;
  slug: string;
  sku: string;
  price: number;
  brandName: string;
  categoryName: string;
  coolingCapacityKw: number;
  heatingCapacityKw: number;
  coolingBtu: number;
  energyClassCooling: number;
  isInverter: boolean;
  hasWifi: boolean;
  noiseLevelDb: number;
  coverageAreaSqm: number;
  primaryImageUrl?: string;
  availableStock: number;
};

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type ProductDetail = ProductListItem & {
  shortDescription?: string;
  description?: string;
  metaTitle?: string;
  metaDescription?: string;
  vatRate: number;
  brandSlug: string;
  categorySlug: string;
  coolingBtu: number;
  heatingBtu: number;
  energyClassHeating: number;
  coolingType: number;
  images: { url: string; altText?: string; isPrimary: boolean }[];
  attributes: { name: string; value: string }[];
};

export type CartItem = { productId: string; quantity: number; name: string; price: number; slug: string };

export const ENERGY_LABELS = ['A+++', 'A++', 'A+', 'A', 'B', 'C', 'D'];

export function formatRsd(value: number) {
  return new Intl.NumberFormat('sr-RS', { style: 'currency', currency: 'RSD', maximumFractionDigits: 0 }).format(value);
}
