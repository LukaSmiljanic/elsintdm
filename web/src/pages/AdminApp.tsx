import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, apiUpload, formatRsd } from '../api';
import { isValidRsPhone } from '../validation';

type LoginResult = { token: string; email: string; fullName: string; role: string };
type OrderItem = {
  id: string;
  orderNumber: string;
  status: number;
  total: number;
  customerEmail: string;
  createdAtUtc: string;
  deliveryOption: number;
};
type Paged<T> = { items: T[]; totalCount: number };
type StockDto = {
  productId: string;
  productName: string;
  sku: string;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
  reserved: number;
  available: number;
};
type WarehouseDto = { id: string; name: string; code: string; city?: string; isActive: boolean };
type BrandDto = { id: string; name: string; slug: string; isActive: boolean };
type CategoryDto = { id: string; name: string; slug: string; isActive: boolean };
type AdminProduct = {
  id: string;
  sku: string;
  name: string;
  price: number;
  isActive: boolean;
  totalStock: number;
  primaryImageUrl?: string;
};
type AdminProductDetail = {
  id: string;
  sku: string;
  name: string;
  slug: string;
  shortDescription?: string;
  description?: string;
  brandId: string;
  categoryId: string;
  price: number;
  isActive: boolean;
  coolingCapacityKw: number;
  heatingCapacityKw: number;
  energyClassCooling: number;
  energyClassHeating: number;
  isInverter: boolean;
  hasWifi: boolean;
  noiseLevelDb: number;
  coverageAreaSqm: number;
  coolingType: number;
  totalStock: number;
  metaTitle?: string;
  metaDescription?: string;
};
type AdminProductImage = {
  id: string;
  url: string;
  altText?: string;
  isPrimary: boolean;
  sortOrder: number;
  isUploaded: boolean;
};
type MaterialStockLine = { warehouseId: string; warehouseName: string; quantity: number };
type MaterialItem = {
  id: string;
  code: string;
  name: string;
  unit: string;
  notes?: string;
  isActive: boolean;
  stocks: MaterialStockLine[];
};

const STATUS = ['Čeka uplatu', 'U obradi', 'Poslato', 'Montirano', 'Zatvoreno', 'Otkazano'];
const ENERGY = ['A+++', 'A++', 'A+', 'A', 'B', 'C', 'D'];

const EMPTY_PRODUCT: AdminProductDetail = {
  id: '',
  sku: '',
  name: '',
  slug: '',
  shortDescription: '',
  description: '',
  brandId: '',
  categoryId: '',
  price: 0,
  isActive: true,
  coolingCapacityKw: 0,
  heatingCapacityKw: 0,
  energyClassCooling: 0,
  energyClassHeating: 0,
  isInverter: true,
  hasWifi: false,
  noiseLevelDb: 0,
  coverageAreaSqm: 0,
  coolingType: 0,
  totalStock: 0,
};

function useAdminAuth() {
  return !!localStorage.getItem('elsint_token');
}

function slugify(value: string) {
  return value
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'dj')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');
}

export function AdminApp() {
  return (
    <Routes>
      <Route path="login" element={<AdminLogin />} />
      <Route path="*" element={<AdminGuard />} />
    </Routes>
  );
}

function AdminGuard() {
  if (!useAdminAuth()) return <Navigate to="/admin/login" replace />;
  return (
    <div className="admin-shell shell page-pad">
      <aside className="admin-nav">
        <strong className="brand" style={{ fontSize: '1.2rem' }}>ElsInt Admin</strong>
        <Link to="/admin">Porudžbine</Link>
        <Link to="/admin/bookings">Termini</Link>
        <Link to="/admin/services">Usluge</Link>
        <Link to="/admin/schedule">Raspored</Link>
        <Link to="/admin/products">Proizvodi</Link>
        <Link to="/admin/stock">Zalihe</Link>
        <Link to="/admin/materials">Materijal</Link>
        <Link to="/">← Storefront</Link>
        <button
          className="btn secondary"
          onClick={() => {
            localStorage.removeItem('elsint_token');
            window.location.href = '/admin/login';
          }}
        >
          Odjava
        </button>
      </aside>
      <div>
        <Routes>
          <Route index element={<AdminOrders />} />
          <Route path="bookings" element={<AdminBookings />} />
          <Route path="services" element={<AdminServices />} />
          <Route path="schedule" element={<AdminSchedule />} />
          <Route path="products" element={<AdminProducts />} />
          <Route path="stock" element={<AdminStock />} />
          <Route path="materials" element={<AdminMaterials />} />
        </Routes>
      </div>
    </div>
  );
}

function AdminLogin() {
  const navigate = useNavigate();
  const [error, setError] = useState('');
  const login = useMutation({
    mutationFn: (body: { email: string; password: string }) =>
      api<LoginResult>('/api/auth/login', { method: 'POST', body: JSON.stringify(body) }),
  });

  const onSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    const fd = new FormData(e.currentTarget);
    try {
      const result = await login.mutateAsync({
        email: String(fd.get('email')),
        password: String(fd.get('password')),
      });
      localStorage.setItem('elsint_token', result.token);
      navigate('/admin');
    } catch {
      setError('Pogrešan email ili lozinka.');
    }
  };

  return (
    <form className="panel stack" style={{ maxWidth: 420, margin: '3rem auto' }} onSubmit={onSubmit}>
      <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Admin prijava</h1>
      {new URLSearchParams(window.location.search).has('expired') && (
        <p className="muted" style={{ margin: 0 }}>
          Sesija je istekla — prijavite se ponovo. Podaci su sačuvani.
        </p>
      )}
      <label>Email<input name="email" type="email" autoComplete="username" required /></label>
      <label>Lozinka<input name="password" type="password" autoComplete="current-password" required /></label>
      {error && <p className="error">{error}</p>}
      <button className="btn" type="submit">Prijavi se</button>
    </form>
  );
}

type AnyQuery = { isLoading: boolean; isError: boolean; error: unknown; data: unknown };

/** Without this a failed request would just render an empty table, as if the data was deleted. */
function QueryStatus({ query, emptyText }: { query: AnyQuery; emptyText?: string }) {
  if (query.isLoading) return <p className="muted">Učitavanje…</p>;
  if (query.isError) {
    return <p className="error">{(query.error as Error)?.message || 'Greška pri učitavanju podataka.'}</p>;
  }
  const data = query.data as { items?: unknown[] } | unknown[] | undefined;
  const count = Array.isArray(data)
    ? data.length
    : Array.isArray(data?.items)
      ? data.items.length
      : undefined;
  if (count === 0 && emptyText) return <p className="muted">{emptyText}</p>;
  return null;
}

function AdminOrders() {
  const qc = useQueryClient();
  const orders = useQuery({
    queryKey: ['admin-orders'],
    queryFn: () => api<Paged<OrderItem>>('/api/admin/orders?pageSize=50'),
  });
  const updateStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: number }) =>
      api(`/api/admin/orders/${id}/status`, { method: 'PUT', body: JSON.stringify({ status }) }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-orders'] }),
  });

  return (
    <div className="panel stack">
      <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Porudžbine</h1>
      <QueryStatus query={orders} emptyText="Nema porudžbina." />
      <table>
        <thead>
          <tr>
            <th>Broj</th>
            <th>Kupac</th>
            <th>Iznos</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {orders.data?.items.map((o) => (
            <tr key={o.id}>
              <td>{o.orderNumber}</td>
              <td>{o.customerEmail}</td>
              <td>{formatRsd(o.total)}</td>
              <td>{STATUS[o.status]}</td>
              <td>
                <select
                  defaultValue={o.status}
                  onChange={(e) => updateStatus.mutate({ id: o.id, status: Number(e.target.value) })}
                >
                  {STATUS.map((label, idx) => (
                    <option key={label} value={idx}>{label}</option>
                  ))}
                </select>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function AdminProducts() {
  const qc = useQueryClient();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const products = useQuery({
    queryKey: ['admin-products'],
    queryFn: () => api<Paged<AdminProduct>>('/api/admin/products?pageSize=100'),
  });

  const toggleActive = useMutation({
    mutationFn: async ({ id, isActive }: { id: string; isActive: boolean }) => {
      const detail = await api<AdminProductDetail>(`/api/admin/products/${id}`);
      await api(`/api/admin/products/${id}`, {
        method: 'PUT',
        body: JSON.stringify({ ...detail, isActive }),
      });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-products'] }),
  });

  return (
    <div className="panel stack">
      <div className="row" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Proizvodi</h1>
          <p className="muted" style={{ margin: 0 }}>Isključeni proizvodi nestaju sa kataloga, ali ostaju ovde.</p>
        </div>
        <button className="btn" type="button" onClick={() => { setEditingId(null); setCreating(true); }}>
          Dodaj klimu
        </button>
      </div>
      <QueryStatus query={products} emptyText="Nema proizvoda." />
      <table>
        <thead>
          <tr>
            <th>Slika</th>
            <th>SKU</th>
            <th>Naziv</th>
            <th>Cena</th>
            <th>Zaliha</th>
            <th>Vidljiv</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {products.data?.items.map((p) => (
            <tr key={p.id}>
              <td>
                {p.primaryImageUrl ? (
                  <img className="admin-thumb" src={p.primaryImageUrl} alt="" loading="lazy" />
                ) : (
                  <span className="admin-thumb admin-thumb-empty">nema</span>
                )}
              </td>
              <td>{p.sku}</td>
              <td>{p.name}</td>
              <td>{formatRsd(p.price)}</td>
              <td>{p.totalStock}</td>
              <td>
                <label className="check">
                  <input
                    type="checkbox"
                    checked={p.isActive}
                    disabled={toggleActive.isPending}
                    onChange={(e) => toggleActive.mutate({ id: p.id, isActive: e.target.checked })}
                  />
                  <span>{p.isActive ? 'Da' : 'Ne'}</span>
                </label>
              </td>
              <td>
                <button
                  className="btn secondary"
                  type="button"
                  onClick={() => { setCreating(false); setEditingId(p.id); }}
                >
                  Izmeni
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {(editingId || creating) && (
        <ProductEditForm
          productId={editingId}
          onClose={() => { setEditingId(null); setCreating(false); }}
          onCreated={(id) => {
            setCreating(false);
            setEditingId(id);
            qc.invalidateQueries({ queryKey: ['admin-products'] });
          }}
          onSaved={() => {
            setEditingId(null);
            setCreating(false);
            qc.invalidateQueries({ queryKey: ['admin-products'] });
          }}
        />
      )}
    </div>
  );
}

function ProductEditForm({
  productId,
  onClose,
  onCreated,
  onSaved,
}: {
  productId: string | null;
  onClose: () => void;
  onCreated: (id: string) => void;
  onSaved: () => void;
}) {
  const isCreate = !productId;
  const detail = useQuery({
    queryKey: ['admin-product', productId],
    queryFn: () => api<AdminProductDetail>(`/api/admin/products/${productId}`),
    enabled: !!productId,
  });
  const brands = useQuery({ queryKey: ['admin-brands'], queryFn: () => api<BrandDto[]>('/api/admin/brands') });
  const categories = useQuery({
    queryKey: ['admin-categories'],
    queryFn: () => api<CategoryDto[]>('/api/admin/categories'),
  });
  const warehouses = useQuery({
    queryKey: ['admin-warehouses'],
    queryFn: () => api<WarehouseDto[]>('/api/admin/warehouses'),
  });
  const [form, setForm] = useState<AdminProductDetail | null>(null);
  const [initialStock, setInitialStock] = useState('0');
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [slugManual, setSlugManual] = useState(false);

  useEffect(() => {
    setSlugManual(false);
    setError('');
    setFieldErrors({});
    setInitialStock('0');
    setForm(isCreate ? { ...EMPTY_PRODUCT } : null);
  }, [productId, isCreate]);

  useEffect(() => {
    if (detail.data) setForm(detail.data);
  }, [detail.data]);

  // New products need a brand/category preselected so the dropdowns aren't blank.
  useEffect(() => {
    if (!isCreate) return;
    setForm((prev) => {
      if (!prev) return prev;
      const brandId = prev.brandId || brands.data?.[0]?.id || '';
      const categoryId = prev.categoryId || categories.data?.[0]?.id || '';
      if (brandId === prev.brandId && categoryId === prev.categoryId) return prev;
      return { ...prev, brandId, categoryId };
    });
  }, [isCreate, brands.data, categories.data]);

  const save = useMutation({
    mutationFn: async (body: AdminProductDetail) => {
      if (!isCreate) {
        await api(`/api/admin/products/${body.id}`, { method: 'PUT', body: JSON.stringify(body) });
        return { id: body.id, created: false };
      }

      const newId = await api<string>('/api/admin/products', {
        method: 'POST',
        body: JSON.stringify({ ...body, id: null }),
      });

      const quantity = Number(initialStock);
      const warehouseId = warehouses.data?.[0]?.id;
      if (warehouseId && Number.isFinite(quantity) && quantity > 0) {
        await api('/api/admin/stock', {
          method: 'PUT',
          body: JSON.stringify({ productId: newId, warehouseId, quantity }),
        });
      }
      return { id: newId, created: true };
    },
    onSuccess: (result) => (result.created ? onCreated(result.id) : onSaved()),
    onError: (err: Error) => setError(err.message),
  });

  const validate = (values: AdminProductDetail) => {
    const next: Record<string, string> = {};
    if (!values.sku.trim()) next.sku = 'Unesite SKU (šifru).';
    if (!values.name.trim()) next.name = 'Unesite naziv.';
    if (!values.slug.trim()) next.slug = 'Slug je obavezan.';
    if (!(values.price > 0)) next.price = 'Cena mora biti veća od 0.';
    if (!values.brandId) next.brandId = 'Izaberite brend.';
    if (!values.categoryId) next.categoryId = 'Izaberite kategoriju.';
    return next;
  };

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKey);
    const prev = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = prev;
    };
  }, [onClose]);

  const set = <K extends keyof AdminProductDetail>(key: K, value: AdminProductDetail[K]) => {
    setForm((prev) => (prev ? { ...prev, [key]: value } : prev));
    setFieldErrors((prev) => {
      if (!prev[key as string]) return prev;
      const next = { ...prev };
      delete next[key as string];
      return next;
    });
  };

  return (
    <div
      className="admin-modal-backdrop"
      role="presentation"
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div className="admin-modal panel stack" role="dialog" aria-modal="true" aria-labelledby="product-edit-title">
        <div className="row" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
          <h2 id="product-edit-title" style={{ margin: 0, fontFamily: 'var(--display)' }}>
            {isCreate ? 'Nova klima' : 'Izmena proizvoda'}
          </h2>
          <button className="btn secondary" type="button" onClick={onClose}>Zatvori</button>
        </div>

        {detail.isLoading && <p className="muted">Učitavanje proizvoda…</p>}
        {detail.isError && (
          <p className="error">
            {(detail.error as Error)?.message || 'Neuspešno učitavanje proizvoda.'}
          </p>
        )}

        {form && (
          <form
            className="stack"
            noValidate
            onSubmit={(e) => {
              e.preventDefault();
              setError('');
              const problems = validate(form);
              setFieldErrors(problems);
              if (Object.keys(problems).length > 0) {
                setError('Popunite obeležena polja.');
                return;
              }
              save.mutate(form);
            }}
          >
            <div className="grid admin-product-form-grid">
              <label className={fieldErrors.sku ? 'invalid' : undefined}>
                SKU
                <input value={form.sku} onChange={(e) => set('sku', e.target.value)} />
                {fieldErrors.sku && <span className="field-error">{fieldErrors.sku}</span>}
              </label>
              <label className={fieldErrors.slug ? 'invalid' : undefined}>
                Slug
                <input
                  value={form.slug}
                  onChange={(e) => {
                    setSlugManual(true);
                    set('slug', e.target.value);
                  }}
                />
                {fieldErrors.slug && <span className="field-error">{fieldErrors.slug}</span>}
              </label>
              <label className={`span-2${fieldErrors.name ? ' invalid' : ''}`}>
                Naziv
                <input
                  value={form.name}
                  onChange={(e) => {
                    const name = e.target.value;
                    set('name', name);
                    if (!slugManual) set('slug', slugify(name));
                  }}
                />
                {fieldErrors.name && <span className="field-error">{fieldErrors.name}</span>}
              </label>
              <label className={fieldErrors.price ? 'invalid' : undefined}>
                Cena (RSD)
                <input type="number" min={0} step={1} value={form.price} onChange={(e) => set('price', Number(e.target.value))} />
                {fieldErrors.price && <span className="field-error">{fieldErrors.price}</span>}
              </label>
              {isCreate && (
                <label>
                  Početna zaliha (kom)
                  <input type="number" min={0} step={1} value={initialStock} onChange={(e) => setInitialStock(e.target.value)} />
                </label>
              )}
              <label className={fieldErrors.brandId ? 'invalid' : undefined}>
                Brand
                <select value={form.brandId} onChange={(e) => set('brandId', e.target.value)}>
                  <option value="">— izaberite —</option>
                  {brands.data?.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
                </select>
                {fieldErrors.brandId && <span className="field-error">{fieldErrors.brandId}</span>}
              </label>
              <label className={fieldErrors.categoryId ? 'invalid' : undefined}>
                Kategorija
                <select value={form.categoryId} onChange={(e) => set('categoryId', e.target.value)}>
                  <option value="">— izaberite —</option>
                  {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
                {fieldErrors.categoryId && <span className="field-error">{fieldErrors.categoryId}</span>}
              </label>
              <label>
                Vidljiv na shopu
                <select value={form.isActive ? '1' : '0'} onChange={(e) => set('isActive', e.target.value === '1')}>
                  <option value="1">Da</option>
                  <option value="0">Ne</option>
                </select>
              </label>
              <label>
                Hlađenje kW
                <input type="number" step={0.1} value={form.coolingCapacityKw} onChange={(e) => set('coolingCapacityKw', Number(e.target.value))} />
                <span className="meta">BTU se računa automatski iz kW.</span>
              </label>
              <label>Grejanje kW<input type="number" step={0.1} value={form.heatingCapacityKw} onChange={(e) => set('heatingCapacityKw', Number(e.target.value))} /></label>
              <label>
                Klasa hlađenje
                <select value={form.energyClassCooling} onChange={(e) => set('energyClassCooling', Number(e.target.value))}>
                  {ENERGY.map((label, idx) => <option key={label} value={idx}>{label}</option>)}
                </select>
              </label>
              <label>
                Klasa grejanje
                <select value={form.energyClassHeating} onChange={(e) => set('energyClassHeating', Number(e.target.value))}>
                  {ENERGY.map((label, idx) => <option key={label} value={idx}>{label}</option>)}
                </select>
              </label>
              <label>Površina m²<input type="number" step={0.1} value={form.coverageAreaSqm} onChange={(e) => set('coverageAreaSqm', Number(e.target.value))} /></label>
              <label>Buka dB<input type="number" step={0.1} value={form.noiseLevelDb} onChange={(e) => set('noiseLevelDb', Number(e.target.value))} /></label>
              <label>
                Tip
                <select
                  value={form.coolingType}
                  onChange={(e) => {
                    const coolingType = Number(e.target.value);
                    set('coolingType', coolingType);
                    set('isInverter', coolingType === 0);
                  }}
                >
                  <option value={0}>Inverter</option>
                  <option value={1}>On/Off</option>
                </select>
              </label>
              <label className="check">
                <input type="checkbox" checked={form.hasWifi} onChange={(e) => set('hasWifi', e.target.checked)} />
                <span>Wi-Fi</span>
              </label>
              <label className="span-2">
                Kratak opis
                <textarea value={form.shortDescription ?? ''} onChange={(e) => set('shortDescription', e.target.value)} rows={2} />
              </label>
              <label className="span-2">
                Opis
                <textarea value={form.description ?? ''} onChange={(e) => set('description', e.target.value)} rows={4} />
              </label>
            </div>
            {error && <p className="error">{error}</p>}
            <div className="row">
              <button className="btn" type="submit" disabled={save.isPending}>
                {save.isPending ? 'Čuvanje…' : isCreate ? 'Sačuvaj klimu' : 'Sačuvaj'}
              </button>
              <button className="btn secondary" type="button" onClick={onClose}>Otkaži</button>
            </div>
          </form>
        )}

        {productId ? (
          <ProductImagesEditor productId={productId} />
        ) : (
          <p className="muted" style={{ margin: 0 }}>
            Slike možete dodati odmah nakon što sačuvate klimu.
          </p>
        )}
      </div>
    </div>
  );
}

function ProductImagesEditor({ productId }: { productId: string }) {
  const qc = useQueryClient();
  const [error, setError] = useState('');
  const [urlDraft, setUrlDraft] = useState('');

  const images = useQuery({
    queryKey: ['admin-product-images', productId],
    queryFn: () => api<AdminProductImage[]>(`/api/admin/products/${productId}/images`),
  });

  const refresh = () => {
    qc.invalidateQueries({ queryKey: ['admin-product-images', productId] });
    qc.invalidateQueries({ queryKey: ['admin-products'] });
  };

  const upload = useMutation({
    mutationFn: (file: File) => {
      const form = new FormData();
      form.append('file', file);
      form.append('makePrimary', String((images.data?.length ?? 0) === 0));
      return apiUpload<string>(`/api/admin/products/${productId}/images`, form);
    },
    onSuccess: () => {
      setError('');
      refresh();
    },
    onError: (err: Error) => setError(err.message),
  });

  const addUrl = useMutation({
    mutationFn: (url: string) => {
      const form = new FormData();
      form.append('url', url);
      form.append('makePrimary', String((images.data?.length ?? 0) === 0));
      return apiUpload<string>(`/api/admin/products/${productId}/images`, form);
    },
    onSuccess: () => {
      setError('');
      setUrlDraft('');
      refresh();
    },
    onError: (err: Error) => setError(err.message),
  });

  const setPrimary = useMutation({
    mutationFn: (imageId: string) =>
      api(`/api/admin/products/${productId}/images/${imageId}/primary`, { method: 'PUT' }),
    onSuccess: refresh,
    onError: (err: Error) => setError(err.message),
  });

  const remove = useMutation({
    mutationFn: (imageId: string) =>
      api(`/api/admin/products/${productId}/images/${imageId}`, { method: 'DELETE' }),
    onSuccess: refresh,
    onError: (err: Error) => setError(err.message),
  });

  const busy = upload.isPending || addUrl.isPending || setPrimary.isPending || remove.isPending;

  return (
    <div className="stack admin-images">
      <h3 style={{ margin: 0, fontFamily: 'var(--display)' }}>Slike</h3>
      <p className="muted" style={{ margin: 0 }}>
        Prva (glavna) slika se prikazuje u katalogu. JPG/PNG/WEBP, do 6 MB.
      </p>

      <div className="row" style={{ gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
        <label className="btn secondary" style={{ marginBottom: 0 }}>
          {upload.isPending ? 'Slanje…' : 'Dodaj sliku sa računara'}
          <input
            type="file"
            accept="image/*"
            style={{ display: 'none' }}
            disabled={busy}
            onChange={(e) => {
              const file = e.target.files?.[0];
              e.target.value = '';
              if (file) upload.mutate(file);
            }}
          />
        </label>
        <input
          placeholder="ili nalepite URL slike"
          value={urlDraft}
          onChange={(e) => setUrlDraft(e.target.value)}
          style={{ flex: '1 1 220px', minWidth: 200 }}
        />
        <button
          className="btn secondary"
          type="button"
          disabled={busy || !urlDraft.trim()}
          onClick={() => addUrl.mutate(urlDraft.trim())}
        >
          Dodaj URL
        </button>
      </div>

      {error && <p className="error">{error}</p>}
      {images.isLoading && <p className="muted">Učitavanje slika…</p>}
      {!images.isLoading && (images.data?.length ?? 0) === 0 && (
        <p className="muted">Nema slika za ovaj proizvod.</p>
      )}

      <div className="admin-image-grid">
        {images.data?.map((img) => (
          <div key={img.id} className={`admin-image-card${img.isPrimary ? ' primary' : ''}`}>
            <img src={img.url} alt={img.altText ?? ''} loading="lazy" />
            <div className="admin-image-actions">
              {img.isPrimary ? (
                <span className="meta">Glavna</span>
              ) : (
                <button className="btn secondary" type="button" disabled={busy} onClick={() => setPrimary.mutate(img.id)}>
                  Glavna
                </button>
              )}
              <button
                className="btn secondary"
                type="button"
                disabled={busy}
                onClick={() => {
                  if (window.confirm('Obrisati sliku?')) remove.mutate(img.id);
                }}
              >
                Obriši
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function AdminStock() {
  const qc = useQueryClient();
  const [warehouseId, setWarehouseId] = useState('');
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const warehouses = useQuery({
    queryKey: ['admin-warehouses'],
    queryFn: () => api<WarehouseDto[]>('/api/admin/warehouses'),
  });
  const stock = useQuery({
    queryKey: ['admin-stock', warehouseId],
    queryFn: () => api<StockDto[]>(`/api/admin/stock${warehouseId ? `?warehouseId=${warehouseId}` : ''}`),
  });
  const adjust = useMutation({
    mutationFn: (body: { productId: string; warehouseId: string; quantity: number }) =>
      api('/api/admin/stock', { method: 'PUT', body: JSON.stringify(body) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-stock'] });
      qc.invalidateQueries({ queryKey: ['admin-products'] });
    },
  });

  const rows = stock.data ?? [];

  return (
    <div className="panel stack">
      <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Zalihe klima</h1>
      <QueryStatus query={stock} emptyText="Nema unetih zaliha." />
      {(warehouses.data?.length ?? 0) > 1 && (
        <label style={{ maxWidth: 280 }}>
          Magacin
          <select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)}>
            <option value="">Svi magacini</option>
            {warehouses.data?.map((w) => (
              <option key={w.id} value={w.id}>{w.name}</option>
            ))}
          </select>
        </label>
      )}
      <table>
        <thead>
          <tr>
            <th>Proizvod</th>
            {(warehouses.data?.length ?? 0) > 1 && <th>Magacin</th>}
            <th>Količina</th>
            <th>Rezervisano</th>
            <th>Dostupno</th>
            <th>Nova količina</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((s) => {
            const key = `${s.productId}-${s.warehouseId}`;
            const draft = drafts[key] ?? String(s.quantity);
            return (
              <tr key={key}>
                <td>{s.productName}<div className="meta">{s.sku}</div></td>
                {(warehouses.data?.length ?? 0) > 1 && <td>{s.warehouseName}</td>}
                <td>{s.quantity}</td>
                <td>{s.reserved}</td>
                <td>{s.available}</td>
                <td>
                  <div className="row" style={{ gap: 8, alignItems: 'center' }}>
                    <input
                      type="number"
                      min={0}
                      style={{ width: 90 }}
                      value={draft}
                      onChange={(e) => setDrafts((prev) => ({ ...prev, [key]: e.target.value }))}
                    />
                    <button
                      className="btn secondary"
                      type="button"
                      disabled={adjust.isPending || Number(draft) === s.quantity}
                      onClick={() =>
                        adjust.mutate({
                          productId: s.productId,
                          warehouseId: s.warehouseId,
                          quantity: Number(draft),
                        })
                      }
                    >
                      Sačuvaj
                    </button>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function AdminMaterials() {
  const qc = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [editing, setEditing] = useState<MaterialItem | null>(null);
  const [stockDrafts, setStockDrafts] = useState<Record<string, string>>({});
  const materials = useQuery({
    queryKey: ['admin-materials'],
    queryFn: () => api<MaterialItem[]>('/api/admin/materials'),
  });
  const warehouses = useQuery({
    queryKey: ['admin-warehouses'],
    queryFn: () => api<WarehouseDto[]>('/api/admin/warehouses'),
  });

  const upsert = useMutation({
    mutationFn: (body: { id?: string; code: string; name: string; unit: string; notes?: string; isActive: boolean }) =>
      body.id
        ? api(`/api/admin/materials/${body.id}`, {
            method: 'PUT',
            body: JSON.stringify({
              code: body.code,
              name: body.name,
              unit: body.unit,
              notes: body.notes,
              isActive: body.isActive,
            }),
          })
        : api<string>('/api/admin/materials', {
            method: 'POST',
            body: JSON.stringify({
              code: body.code,
              name: body.name,
              unit: body.unit,
              notes: body.notes,
              isActive: body.isActive,
            }),
          }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-materials'] });
      setShowCreate(false);
      setEditing(null);
    },
  });

  const adjustStock = useMutation({
    mutationFn: (body: { materialId: string; warehouseId: string; quantity: number }) =>
      api('/api/admin/materials/stock', { method: 'PUT', body: JSON.stringify(body) }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-materials'] }),
  });

  const warehouseList = (warehouses.data ?? []).filter((w) => w.isActive);
  const singleWarehouse = warehouseList.length <= 1;

  const flatRows = useMemo(() => {
    const list = materials.data ?? [];
    const rows: { material: MaterialItem; warehouseId: string; warehouseName: string; quantity: number }[] = [];
    for (const material of list) {
      if (warehouseList.length === 0) {
        rows.push({ material, warehouseId: '', warehouseName: '—', quantity: 0 });
        continue;
      }
      for (const wh of warehouseList) {
        const stock = material.stocks.find((s) => s.warehouseId === wh.id);
        rows.push({
          material,
          warehouseId: wh.id,
          warehouseName: wh.name,
          quantity: stock?.quantity ?? 0,
        });
      }
    }
    return rows;
  }, [materials.data, warehouseList]);

  return (
    <div className="panel stack">
      <div className="row" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Materijal</h1>
          <p className="muted" style={{ margin: 0 }}>Interni popis — nije na webshopu.</p>
          <QueryStatus query={materials} emptyText="Nema unetog materijala." />
        </div>
        <button className="btn" type="button" onClick={() => { setEditing(null); setShowCreate(true); }}>
          Dodaj materijal
        </button>
      </div>

      {(showCreate || editing) && (
        <MaterialForm
          initial={editing}
          onCancel={() => { setShowCreate(false); setEditing(null); }}
          onSubmit={(body) => upsert.mutate(body)}
          pending={upsert.isPending}
        />
      )}

      <table>
        <thead>
          <tr>
            <th>Šifra</th>
            <th>Naziv</th>
            <th>Jedinica</th>
            {!singleWarehouse && <th>Magacin</th>}
            <th>Količina</th>
            <th>Aktivan</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {flatRows.map(({ material, warehouseId, warehouseName, quantity }) => {
            const key = `${material.id}-${warehouseId}`;
            const draft = stockDrafts[key] ?? String(quantity);
            return (
              <tr key={key || material.id}>
                <td>{material.code}</td>
                <td>
                  {material.name}
                  {material.notes && <div className="meta">{material.notes}</div>}
                </td>
                <td>{material.unit}</td>
                {!singleWarehouse && <td>{warehouseName}</td>}
                <td>
                  {warehouseId ? (
                    <div className="row" style={{ gap: 8, alignItems: 'center' }}>
                      <input
                        type="number"
                        min={0}
                        step={0.001}
                        style={{ width: 100 }}
                        value={draft}
                        onChange={(e) => setStockDrafts((prev) => ({ ...prev, [key]: e.target.value }))}
                      />
                      <button
                        className="btn secondary"
                        type="button"
                        disabled={adjustStock.isPending || Number(draft) === quantity}
                        onClick={() =>
                          adjustStock.mutate({
                            materialId: material.id,
                            warehouseId,
                            quantity: Number(draft),
                          })
                        }
                      >
                        Sačuvaj
                      </button>
                    </div>
                  ) : (
                    quantity
                  )}
                </td>
                <td>{material.isActive ? 'Da' : 'Ne'}</td>
                <td>
                  <button
                    className="btn secondary"
                    type="button"
                    onClick={() => {
                      setShowCreate(false);
                      setEditing(material);
                    }}
                  >
                    Izmeni
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function MaterialForm({
  initial,
  onCancel,
  onSubmit,
  pending,
}: {
  initial: MaterialItem | null;
  onCancel: () => void;
  onSubmit: (body: { id?: string; code: string; name: string; unit: string; notes?: string; isActive: boolean }) => void;
  pending: boolean;
}) {
  const [code, setCode] = useState(initial?.code ?? '');
  const [name, setName] = useState(initial?.name ?? '');
  const [unit, setUnit] = useState(initial?.unit ?? 'kom');
  const [notes, setNotes] = useState(initial?.notes ?? '');
  const [isActive, setIsActive] = useState(initial?.isActive ?? true);

  useEffect(() => {
    setCode(initial?.code ?? '');
    setName(initial?.name ?? '');
    setUnit(initial?.unit ?? 'kom');
    setNotes(initial?.notes ?? '');
    setIsActive(initial?.isActive ?? true);
  }, [initial]);

  return (
    <form
      className="panel stack"
      onSubmit={(e) => {
        e.preventDefault();
        onSubmit({
          id: initial?.id,
          code,
          name,
          unit,
          notes: notes || undefined,
          isActive,
        });
      }}
    >
      <h2 style={{ margin: 0, fontFamily: 'var(--display)' }}>
        {initial ? 'Izmena materijala' : 'Novi materijal'}
      </h2>
      <div className="grid" style={{ gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
        <label>Šifra<input value={code} onChange={(e) => setCode(e.target.value)} required /></label>
        <label>Jedinica<input value={unit} onChange={(e) => setUnit(e.target.value)} required placeholder="kom / m / kg" /></label>
        <label style={{ gridColumn: '1 / -1' }}>Naziv<input value={name} onChange={(e) => setName(e.target.value)} required /></label>
        <label style={{ gridColumn: '1 / -1' }}>Napomena<textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} /></label>
        <label>
          Aktivan
          <select value={isActive ? '1' : '0'} onChange={(e) => setIsActive(e.target.value === '1')}>
            <option value="1">Da</option>
            <option value="0">Ne</option>
          </select>
        </label>
      </div>
      <div className="row">
        <button className="btn" type="submit" disabled={pending}>Sačuvaj</button>
        <button className="btn secondary" type="button" onClick={onCancel}>Otkaži</button>
      </div>
    </form>
  );
}

const BOOKING_STATUS = ['Rezervisan (hold)', 'Potvrđen', 'Završen', 'Otkazan', 'Istekao'];
const DAYS = ['Nedelja', 'Ponedeljak', 'Utorak', 'Sreda', 'Četvrtak', 'Petak', 'Subota'];

function bookingStatusMod(status: number) {
  const s = Math.min(Math.max(status, 0), 4);
  return `booking-status--${s}` as const;
}

type FieldServiceAdmin = {
  id: string;
  name: string;
  slug: string;
  description?: string;
  durationMinutes: number;
  price: number;
  isActive: boolean;
  sortOrder: number;
};

type AvailabilityRuleAdmin = {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isActive: boolean;
  label?: string;
  serviceIds: string[];
};

type BookingAdmin = {
  id: string;
  fieldServiceId: string;
  serviceName: string;
  customerName: string;
  customerPhone: string;
  customerEmail?: string;
  address?: string;
  notes?: string;
  startLocal: string;
  endLocal: string;
  status: number;
  holdExpiresAtUtc?: string;
  adminNotes?: string;
  materialLines: number;
};

type BookingMaterialUsage = {
  id: string;
  materialId: string;
  materialCode: string;
  materialName: string;
  unit: string;
  quantity: number;
  notes?: string;
  createdAtUtc: string;
};

function AdminBookings() {
  const qc = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<string>('0');
  const [materialsFor, setMaterialsFor] = useState<BookingAdmin | null>(null);
  const [creating, setCreating] = useState(false);
  const bookings = useQuery({
    queryKey: ['admin-bookings', statusFilter],
    queryFn: () =>
      api<BookingAdmin[]>(
        `/api/admin/bookings${statusFilter === '' ? '' : `?status=${statusFilter}`}`
      ),
  });
  const update = useMutation({
    mutationFn: ({ id, status }: { id: string; status: number }) =>
      api(`/api/admin/bookings/${id}/status`, {
        method: 'PUT',
        body: JSON.stringify({ status }),
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-bookings'] }),
  });

  return (
    <div className="panel stack">
      <div className="row" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Termini</h1>
        <button className="btn" type="button" onClick={() => setCreating(true)}>
          Novi termin (telefon)
        </button>
      </div>
      <QueryStatus query={bookings} emptyText="Nema zakazanih termina." />
      <p className="muted" style={{ margin: 0 }}>
        Novo zakazivanje stiže ovde kao „Rezervisan (hold)“. Potvrdite pozivom; hold ističe za ~4h.
        Termin dogovoren telefonom unesite preko „Novi termin“ — odmah je potvrđen i zauzima kalendar.
      </p>
      <label style={{ maxWidth: 260 }}>
        Status
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
          <option value="">Svi</option>
          {BOOKING_STATUS.map((label, idx) => (
            <option key={label} value={idx}>{label}</option>
          ))}
        </select>
      </label>

      <div className="booking-status-legend" aria-label="Legenda statusa termina">
        {BOOKING_STATUS.map((label, idx) => (
          <span key={label} className={`booking-status-pill ${bookingStatusMod(idx)}`}>
            {label}
          </span>
        ))}
      </div>

      <table className="admin-bookings-table">
        <thead>
          <tr>
            <th>Termin</th>
            <th>Usluga</th>
            <th>Klijent</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {bookings.data?.map((b) => (
            <tr key={b.id} className={`booking-row ${bookingStatusMod(b.status)}`}>
              <td>
                {b.startLocal}
                <div className="meta">do {b.endLocal.slice(11)}</div>
                {b.status === 0 && b.holdExpiresAtUtc && (
                  <div className="meta">hold do {new Date(b.holdExpiresAtUtc).toLocaleString('sr-RS')}</div>
                )}
              </td>
              <td>{b.serviceName}</td>
              <td>
                {b.customerName}
                <div className="meta">{b.customerPhone}</div>
                {b.address && <div className="meta">{b.address}</div>}
                {b.notes && <div className="meta">{b.notes}</div>}
              </td>
              <td>
                <span className={`booking-status-pill ${bookingStatusMod(b.status)}`}>
                  {BOOKING_STATUS[b.status] ?? b.status}
                </span>
              </td>
              <td>
                <div className="stack" style={{ gap: 6 }}>
                  <select
                    value={b.status}
                    onChange={(e) => update.mutate({ id: b.id, status: Number(e.target.value) })}
                  >
                    {BOOKING_STATUS.map((label, idx) => (
                      <option key={label} value={idx}>{label}</option>
                    ))}
                  </select>
                  <button className="btn secondary" type="button" onClick={() => setMaterialsFor(b)}>
                    Materijal{b.materialLines > 0 ? ` (${b.materialLines})` : ''}
                  </button>
                  <a className="btn secondary" href={`tel:${b.customerPhone}`}>Pozovi</a>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {materialsFor && (
        <BookingMaterialsModal
          booking={materialsFor}
          onClose={() => {
            setMaterialsFor(null);
            qc.invalidateQueries({ queryKey: ['admin-bookings'] });
          }}
        />
      )}

      {creating && (
        <ManualBookingModal
          onClose={() => setCreating(false)}
          onCreated={(status) => {
            setCreating(false);
            // Jump the filter to the new booking's status, otherwise it lands outside the current view.
            setStatusFilter(String(status));
            qc.invalidateQueries({ queryKey: ['admin-bookings'] });
          }}
        />
      )}
    </div>
  );
}

function todayLocalDate() {
  const now = new Date();
  const offset = now.getTimezoneOffset() * 60000;
  return new Date(now.getTime() - offset).toISOString().slice(0, 10);
}

function ManualBookingModal({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: (status: number) => void;
}) {
  const services = useQuery({
    queryKey: ['admin-field-services'],
    queryFn: () => api<FieldServiceAdmin[]>('/api/admin/field-services'),
  });

  const [serviceId, setServiceId] = useState('');
  const [date, setDate] = useState(todayLocalDate());
  const [time, setTime] = useState('09:00');
  const [duration, setDuration] = useState('');
  const [customerName, setCustomerName] = useState('');
  const [customerPhone, setCustomerPhone] = useState('');
  const [customerEmail, setCustomerEmail] = useState('');
  const [address, setAddress] = useState('');
  const [notes, setNotes] = useState('');
  const [status, setStatus] = useState(1);
  const [allowOverlap, setAllowOverlap] = useState(false);
  const [error, setError] = useState('');
  const [conflict, setConflict] = useState(false);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKey);
    const prev = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = prev;
    };
  }, [onClose]);

  const activeServices = services.data?.filter((s) => s.isActive) ?? [];
  const selected = activeServices.find((s) => s.id === serviceId);
  const effectiveDuration = duration.trim() ? Number(duration) : selected?.durationMinutes;

  const create = useMutation({
    mutationFn: () =>
      api<BookingAdmin>('/api/admin/bookings', {
        method: 'POST',
        body: JSON.stringify({
          serviceId,
          startLocal: `${date}T${time}`,
          durationMinutes: duration.trim() ? Number(duration) : null,
          customerName: customerName.trim(),
          customerPhone: customerPhone.trim(),
          customerEmail: customerEmail.trim() || null,
          address: address.trim() || null,
          notes: notes.trim() || null,
          adminNotes: 'Unet telefonom',
          status,
          allowOverlap,
        }),
      }),
    onSuccess: () => onCreated(status),
    onError: (err: Error) => {
      setError(err.message);
      setConflict(err.message.toLowerCase().includes('preklapa'));
    },
  });

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    if (!serviceId) return setError('Izaberite uslugu.');
    if (!date || !time) return setError('Unesite datum i vreme.');
    if (!customerName.trim()) return setError('Unesite ime klijenta.');
    if (!isValidRsPhone(customerPhone)) return setError('Unesite ispravan telefon (npr. 064 123 4567).');
    create.mutate();
  };

  return (
    <div
      className="admin-modal-backdrop"
      role="presentation"
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div className="admin-modal panel stack" role="dialog" aria-modal="true" aria-labelledby="manual-booking-title">
        <div className="row" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <h2 id="manual-booking-title" style={{ margin: 0, fontFamily: 'var(--display)' }}>
              Novi termin
            </h2>
            <p className="muted" style={{ margin: 0 }}>
              Za dogovor telefonom. Radno vreme iz Rasporeda se ne primenjuje, ali preklapanje se proverava.
            </p>
          </div>
          <button className="btn secondary" type="button" onClick={onClose}>Zatvori</button>
        </div>

        <form className="stack" noValidate onSubmit={submit}>
          <div className="grid admin-product-form-grid">
            <label className="span-2">
              Usluga
              <select
                value={serviceId}
                onChange={(e) => {
                  setServiceId(e.target.value);
                  setDuration('');
                }}
              >
                <option value="">— izaberite —</option>
                {activeServices.map((s) => (
                  <option key={s.id} value={s.id}>{s.name} ({s.durationMinutes} min)</option>
                ))}
              </select>
            </label>
            <label>
              Datum
              <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
            </label>
            <label>
              Vreme
              <input type="time" value={time} onChange={(e) => setTime(e.target.value)} />
            </label>
            <label>
              Trajanje (min)
              <input
                type="number"
                min={15}
                max={600}
                step={15}
                value={duration}
                placeholder={selected ? String(selected.durationMinutes) : ''}
                onChange={(e) => setDuration(e.target.value)}
              />
              <span className="meta">
                {selected ? `Prazno = ${selected.durationMinutes} min po usluzi.` : 'Prazno = trajanje usluge.'}
              </span>
            </label>
            <label>
              Status
              <select value={status} onChange={(e) => setStatus(Number(e.target.value))}>
                <option value={1}>Potvrđen</option>
                <option value={2}>Završen</option>
              </select>
              <span className="meta">„Završen“ za posao koji je već odrađen.</span>
            </label>
            <label>
              Ime i prezime
              <input value={customerName} onChange={(e) => setCustomerName(e.target.value)} />
            </label>
            <label>
              Telefon
              <input value={customerPhone} onChange={(e) => setCustomerPhone(e.target.value)} placeholder="064 123 4567" />
            </label>
            <label>
              Email (opciono)
              <input type="email" value={customerEmail} onChange={(e) => setCustomerEmail(e.target.value)} />
            </label>
            <label>
              Adresa
              <input value={address} onChange={(e) => setAddress(e.target.value)} />
            </label>
            <label className="span-2">
              Napomena
              <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={2} />
            </label>
          </div>

          {effectiveDuration && date && time && (
            <p className="muted" style={{ margin: 0 }}>
              Termin: {date} {time} — {effectiveDuration} min.
            </p>
          )}

          {error && <p className="error">{error}</p>}

          {conflict && (
            <label className="check">
              <input type="checkbox" checked={allowOverlap} onChange={(e) => setAllowOverlap(e.target.checked)} />
              <span>Dozvoli preklapanje sa postojećim terminom</span>
            </label>
          )}

          <div className="row">
            <button className="btn" type="submit" disabled={create.isPending}>
              {create.isPending ? 'Čuvanje…' : 'Sačuvaj termin'}
            </button>
            <button className="btn secondary" type="button" onClick={onClose}>Otkaži</button>
          </div>
        </form>
      </div>
    </div>
  );
}

function BookingMaterialsModal({ booking, onClose }: { booking: BookingAdmin; onClose: () => void }) {
  const qc = useQueryClient();
  const [materialId, setMaterialId] = useState('');
  const [quantity, setQuantity] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState('');

  const usage = useQuery({
    queryKey: ['booking-materials', booking.id],
    queryFn: () => api<BookingMaterialUsage[]>(`/api/admin/bookings/${booking.id}/materials`),
  });
  const materials = useQuery({
    queryKey: ['admin-materials'],
    queryFn: () => api<MaterialItem[]>('/api/admin/materials'),
  });

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKey);
    const prev = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = prev;
    };
  }, [onClose]);

  const refresh = () => {
    qc.invalidateQueries({ queryKey: ['booking-materials', booking.id] });
    qc.invalidateQueries({ queryKey: ['admin-materials'] });
  };

  const add = useMutation({
    mutationFn: (body: { materialId: string; quantity: number; notes?: string }) =>
      api<string>(`/api/admin/bookings/${booking.id}/materials`, {
        method: 'POST',
        body: JSON.stringify(body),
      }),
    onSuccess: () => {
      setError('');
      setQuantity('');
      setNotes('');
      refresh();
    },
    onError: (err: Error) => setError(err.message),
  });

  const remove = useMutation({
    mutationFn: (usageId: string) =>
      api(`/api/admin/bookings/${booking.id}/materials/${usageId}`, { method: 'DELETE' }),
    onSuccess: () => {
      setError('');
      refresh();
    },
    onError: (err: Error) => setError(err.message),
  });

  const stockFor = (id: string) => {
    const material = materials.data?.find((m) => m.id === id);
    if (!material) return null;
    const onHand = material.stocks.reduce((sum, s) => sum + s.quantity, 0);
    return { onHand, unit: material.unit };
  };

  const selectedStock = materialId ? stockFor(materialId) : null;
  const canRecord = booking.status === 1 || booking.status === 2;

  return (
    <div
      className="admin-modal-backdrop"
      role="presentation"
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div className="admin-modal panel stack" role="dialog" aria-modal="true" aria-labelledby="booking-materials-title">
        <div className="row" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <h2 id="booking-materials-title" style={{ margin: 0, fontFamily: 'var(--display)' }}>
              Potrošen materijal
            </h2>
            <p className="muted" style={{ margin: 0 }}>
              {booking.serviceName} · {booking.startLocal} · {booking.customerName}
            </p>
          </div>
          <button className="btn secondary" type="button" onClick={onClose}>Zatvori</button>
        </div>

        {!canRecord && (
          <p className="muted" style={{ margin: 0 }}>
            Materijal se upisuje samo na potvrđen ili završen termin. Ovaj termin je „
            {BOOKING_STATUS[booking.status] ?? booking.status}“ — prebacite status pa se forma otvara.
          </p>
        )}

        {canRecord && (
        <form
          className="stack"
          noValidate
          onSubmit={(e) => {
            e.preventDefault();
            setError('');
            const qty = Number(quantity.replace(',', '.'));
            if (!materialId) {
              setError('Izaberite materijal.');
              return;
            }
            if (!Number.isFinite(qty) || qty <= 0) {
              setError('Unesite količinu veću od 0.');
              return;
            }
            add.mutate({ materialId, quantity: qty, notes: notes.trim() || undefined });
          }}
        >
          <div className="grid admin-product-form-grid">
            <label>
              Materijal
              <select value={materialId} onChange={(e) => setMaterialId(e.target.value)}>
                <option value="">— izaberite —</option>
                {materials.data?.filter((m) => m.isActive).map((m) => {
                  const onHand = m.stocks.reduce((sum, s) => sum + s.quantity, 0);
                  return (
                    <option key={m.id} value={m.id}>
                      {m.name} ({m.code}) — na stanju {onHand} {m.unit}
                    </option>
                  );
                })}
              </select>
            </label>
            <label>
              Količina{selectedStock ? ` (${selectedStock.unit})` : ''}
              <input
                type="number"
                min={0}
                step={0.001}
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
              />
              {selectedStock && (
                <span className="meta">Na stanju: {selectedStock.onHand} {selectedStock.unit}</span>
              )}
            </label>
            <label className="span-2">
              Napomena (opciono)
              <input value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="npr. produžena cev 2m" />
            </label>
          </div>
          <div className="row">
            <button className="btn" type="submit" disabled={add.isPending}>
              {add.isPending ? 'Čuvanje…' : 'Dodaj i skini sa zaliha'}
            </button>
          </div>
        </form>
        )}

        {error && <p className="error">{error}</p>}

        <div className="admin-images">
          <h3 style={{ margin: 0, fontFamily: 'var(--display)' }}>Evidencija</h3>
          <QueryStatus query={usage} emptyText="Još nije upisan materijal za ovaj termin." />
          {(usage.data?.length ?? 0) > 0 && (
            <table>
              <thead>
                <tr>
                  <th>Materijal</th>
                  <th>Količina</th>
                  <th>Napomena</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {usage.data?.map((u) => (
                  <tr key={u.id}>
                    <td>
                      {u.materialName}
                      <div className="meta">{u.materialCode}</div>
                    </td>
                    <td>{u.quantity} {u.unit}</td>
                    <td>{u.notes ?? '—'}</td>
                    <td>
                      <button
                        className="btn secondary"
                        type="button"
                        disabled={remove.isPending}
                        onClick={() => {
                          if (window.confirm('Obrisati stavku? Količina se vraća na zalihe.')) remove.mutate(u.id);
                        }}
                      >
                        Obriši
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}

function AdminServices() {
  const qc = useQueryClient();
  const services = useQuery({
    queryKey: ['admin-field-services'],
    queryFn: () => api<FieldServiceAdmin[]>('/api/admin/field-services'),
  });
  const [editing, setEditing] = useState<FieldServiceAdmin | null>(null);
  const [creating, setCreating] = useState(false);

  const save = useMutation({
    mutationFn: (body: Partial<FieldServiceAdmin> & { name: string; slug: string; durationMinutes: number; price: number; isActive: boolean; sortOrder: number }) =>
      api('/api/admin/field-services', {
        method: 'POST',
        body: JSON.stringify({
          id: body.id ?? null,
          name: body.name,
          slug: body.slug,
          description: body.description ?? null,
          durationMinutes: body.durationMinutes,
          price: body.price,
          isActive: body.isActive,
          sortOrder: body.sortOrder,
        }),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-field-services'] });
      setEditing(null);
      setCreating(false);
    },
  });

  return (
    <div className="panel stack">
      <div className="row" style={{ justifyContent: 'space-between' }}>
        <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Usluge</h1>
        <button className="btn" type="button" onClick={() => { setEditing(null); setCreating(true); }}>Dodaj</button>
      </div>
      <QueryStatus query={services} emptyText="Nema unetih usluga." />
      {(creating || editing) && (
        <ServiceForm
          initial={editing}
          pending={save.isPending}
          onCancel={() => { setCreating(false); setEditing(null); }}
          onSubmit={(body) => save.mutate(body)}
        />
      )}
      <table>
        <thead>
          <tr>
            <th>Naziv</th>
            <th>Trajanje</th>
            <th>Cena</th>
            <th>Aktivna</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {services.data?.map((s) => (
            <tr key={s.id}>
              <td>{s.name}<div className="meta">{s.slug}</div></td>
              <td>{s.durationMinutes} min</td>
              <td>{s.price > 0 ? formatRsd(s.price) : 'po dogovoru'}</td>
              <td>{s.isActive ? 'Da' : 'Ne'}</td>
              <td>
                <button className="btn secondary" type="button" onClick={() => { setCreating(false); setEditing(s); }}>
                  Izmeni
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function ServiceForm({
  initial,
  onCancel,
  onSubmit,
  pending,
}: {
  initial: FieldServiceAdmin | null;
  onCancel: () => void;
  onSubmit: (body: {
    id?: string;
    name: string;
    slug: string;
    description?: string;
    durationMinutes: number;
    price: number;
    isActive: boolean;
    sortOrder: number;
  }) => void;
  pending: boolean;
}) {
  const [name, setName] = useState(initial?.name ?? '');
  const [slug, setSlug] = useState(initial?.slug ?? '');
  const [description, setDescription] = useState(initial?.description ?? '');
  const [durationMinutes, setDurationMinutes] = useState(initial?.durationMinutes ?? 60);
  const [price, setPrice] = useState(initial?.price ?? 0);
  const [isActive, setIsActive] = useState(initial?.isActive ?? true);
  const [sortOrder, setSortOrder] = useState(initial?.sortOrder ?? 0);

  useEffect(() => {
    setName(initial?.name ?? '');
    setSlug(initial?.slug ?? '');
    setDescription(initial?.description ?? '');
    setDurationMinutes(initial?.durationMinutes ?? 60);
    setPrice(initial?.price ?? 0);
    setIsActive(initial?.isActive ?? true);
    setSortOrder(initial?.sortOrder ?? 0);
  }, [initial]);

  return (
    <form
      className="panel stack"
      onSubmit={(e) => {
        e.preventDefault();
        onSubmit({
          id: initial?.id,
          name,
          slug: slug || slugify(name),
          description,
          durationMinutes,
          price,
          isActive,
          sortOrder,
        });
      }}
    >
      <h2 style={{ margin: 0, fontFamily: 'var(--display)' }}>{initial ? 'Izmena usluge' : 'Nova usluga'}</h2>
      <label>Naziv<input value={name} onChange={(e) => { setName(e.target.value); if (!initial) setSlug(slugify(e.target.value)); }} required /></label>
      <label>Slug<input value={slug} onChange={(e) => setSlug(e.target.value)} required /></label>
      <label>Opis<textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2} /></label>
      <div className="grid" style={{ gridTemplateColumns: '1fr 1fr 1fr', gap: '1rem' }}>
        <label>Trajanje (min)<input type="number" min={30} value={durationMinutes} onChange={(e) => setDurationMinutes(Number(e.target.value))} /></label>
        <label>Cena<input type="number" min={0} value={price} onChange={(e) => setPrice(Number(e.target.value))} /></label>
        <label>Redosled<input type="number" value={sortOrder} onChange={(e) => setSortOrder(Number(e.target.value))} /></label>
      </div>
      <label>
        Aktivna
        <select value={isActive ? '1' : '0'} onChange={(e) => setIsActive(e.target.value === '1')}>
          <option value="1">Da</option>
          <option value="0">Ne</option>
        </select>
      </label>
      <div className="row">
        <button className="btn" type="submit" disabled={pending}>Sačuvaj</button>
        <button className="btn secondary" type="button" onClick={onCancel}>Otkaži</button>
      </div>
    </form>
  );
}

function AdminSchedule() {
  const qc = useQueryClient();
  const rules = useQuery({
    queryKey: ['admin-availability'],
    queryFn: () => api<AvailabilityRuleAdmin[]>('/api/admin/availability-rules'),
  });
  const services = useQuery({
    queryKey: ['admin-field-services'],
    queryFn: () => api<FieldServiceAdmin[]>('/api/admin/field-services'),
  });
  const [dayOfWeek, setDayOfWeek] = useState(1);
  const [startTime, setStartTime] = useState('09:00');
  const [endTime, setEndTime] = useState('15:00');
  const [label, setLabel] = useState('');
  const [serviceIds, setServiceIds] = useState<string[]>([]);

  const save = useMutation({
    mutationFn: () =>
      api('/api/admin/availability-rules', {
        method: 'POST',
        body: JSON.stringify({
          id: null,
          dayOfWeek,
          startTime,
          endTime,
          isActive: true,
          label: label || null,
          serviceIds,
        }),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-availability'] });
      setLabel('');
      setServiceIds([]);
    },
  });

  const remove = useMutation({
    mutationFn: (id: string) => api(`/api/admin/availability-rules/${id}`, { method: 'DELETE' }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-availability'] }),
  });

  const toggleService = (id: string) => {
    setServiceIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
  };

  return (
    <div className="panel stack">
      <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Raspored dostupnosti</h1>
      <p className="muted" style={{ margin: 0 }}>
        Ako ne izaberete usluge, period važi za <strong>sve</strong> aktivne usluge. Primer: 09–15 samo pranje, 15–20 sve.
        Bez pravila za neki dan, javna stranica zakazivanja taj dan ne nudi ništa.
      </p>
      <QueryStatus query={rules} emptyText="Nema definisanih pravila — zakazivanje na sajtu neće imati termine dok ne dodate bar jedan dan." />

      <form
        className="panel stack"
        onSubmit={(e) => {
          e.preventDefault();
          save.mutate();
        }}
      >
        <h2 style={{ margin: 0, fontFamily: 'var(--display)', fontSize: '1.1rem' }}>Novo pravilo</h2>
        <div className="grid" style={{ gridTemplateColumns: '1fr 1fr 1fr', gap: '1rem' }}>
          <label>
            Dan
            <select value={dayOfWeek} onChange={(e) => setDayOfWeek(Number(e.target.value))}>
              {DAYS.map((d, idx) => <option key={d} value={idx}>{d}</option>)}
            </select>
          </label>
          <label>Od<input type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} required /></label>
          <label>Do<input type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} required /></label>
        </div>
        <label>Oznaka<input value={label} onChange={(e) => setLabel(e.target.value)} placeholder="npr. Pranje jutro" /></label>
        <div className="stack" style={{ gap: 8 }}>
          <strong>Usluge u periodu (prazno = sve)</strong>
          {services.data && services.data.length > 0 ? (
            <div className="check-list">
              {services.data.map((s) => (
                <label key={s.id} className="check">
                  <input type="checkbox" checked={serviceIds.includes(s.id)} onChange={() => toggleService(s.id)} />
                  <span>{s.name}</span>
                </label>
              ))}
            </div>
          ) : (
            <p className="muted" style={{ margin: 0 }}>Nema aktivnih usluga — dodajte ih u delu Usluge.</p>
          )}
          {serviceIds.length > 0 && (
            <button className="btn secondary" type="button" onClick={() => setServiceIds([])}>
              Očisti izbor ({serviceIds.length})
            </button>
          )}
        </div>
        <button className="btn" type="submit" disabled={save.isPending}>Dodaj pravilo</button>
      </form>

      <table>
        <thead>
          <tr>
            <th>Dan</th>
            <th>Period</th>
            <th>Usluge</th>
            <th>Oznaka</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {rules.data?.map((r) => (
            <tr key={r.id}>
              <td>{DAYS[r.dayOfWeek]}</td>
              <td>{r.startTime.slice(0, 5)}–{r.endTime.slice(0, 5)}</td>
              <td>
                {r.serviceIds.length === 0
                  ? 'Sve'
                  : r.serviceIds
                      .map((id) => services.data?.find((s) => s.id === id)?.name ?? id.slice(0, 6))
                      .join(', ')}
              </td>
              <td>{r.label}</td>
              <td>
                <button className="btn secondary" type="button" onClick={() => remove.mutate(r.id)}>
                  Obriši
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
