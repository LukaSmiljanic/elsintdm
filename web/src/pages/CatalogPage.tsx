import { Link, useParams, useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api, ENERGY_LABELS, formatRsd, type Category, type PagedResult, type ProductListItem } from '../api';
import { Seo } from '../components/Seo';
import { catalogSeo } from '../seo';

type Brand = { id: string; name: string; slug: string };

const BTU_OPTIONS = [9000, 12000, 18000, 24000];
const CATEGORY_FILTERS = [
  { slug: undefined as string | undefined, label: 'Sve' },
  { slug: 'split-sistemi', label: 'Split' },
  { slug: 'mobilni', label: 'Mobilni' },
];

export function CatalogPage() {
  const { slug } = useParams();
  const [params, setParams] = useSearchParams();
  const categorySlug = slug ?? params.get('category') ?? undefined;

  const brands = useQuery({
    queryKey: ['brands'],
    queryFn: () => api<Brand[]>('/api/catalog/brands'),
  });

  const categories = useQuery({
    queryKey: ['categories'],
    queryFn: () => api<Category[]>('/api/catalog/categories'),
  });

  const query = new URLSearchParams();
  if (categorySlug) query.set('categorySlug', categorySlug);
  const brandSlug = params.get('brand');
  const coolingBtu = params.get('btu');
  const sortBy = params.get('sort') ?? 'price_asc';
  const search = params.get('q') ?? '';
  if (brandSlug) query.set('brandSlug', brandSlug);
  if (coolingBtu) query.set('coolingBtu', coolingBtu);
  if (sortBy) query.set('sortBy', sortBy);
  if (search) query.set('search', search);
  query.set('pageSize', '24');

  const products = useQuery({
    queryKey: ['products', query.toString()],
    queryFn: () => api<PagedResult<ProductListItem>>(`/api/catalog/products?${query}`),
  });

  const setFilter = (key: string, value: string) => {
    const next = new URLSearchParams(params);
    if (!value) next.delete(key);
    else next.set(key, value);
    setParams(next);
  };

  const category = categories.data?.find((c) => c.slug === categorySlug);
  const categoryLabel = (s?: string) => {
    if (!s) return 'Sve';
    const known = CATEGORY_FILTERS.find((c) => c.slug === s);
    if (known) return known.label;
    return categories.data?.find((c) => c.slug === s)?.name ?? s;
  };

  return (
    <div className="shell page-pad">
    <Seo {...catalogSeo(category)} />
    <div className="stack">
      <h1 style={{ fontFamily: 'var(--display)', margin: 0, textTransform: 'uppercase', color: 'var(--navy)' }}>
        {category ? category.name : 'Katalog klima uređaja'}
      </h1>
      <p className="muted" style={{ margin: 0 }}>
        {category?.description ||
          'Split, inverter i mobilne klime za Novi Sad — jasne cene, dostava i montaža.'}
      </p>
      <div className="grid layout-2">
        <aside className="filters">
          <strong>Filteri</strong>
          <label>
            Pretraga
            <input value={search} onChange={(e) => setFilter('q', e.target.value)} placeholder="Naziv ili SKU" />
          </label>
          <label>
            Brend
            <select value={brandSlug ?? ''} onChange={(e) => setFilter('brand', e.target.value)}>
              <option value="">Svi brendovi</option>
              {brands.data?.map((b) => (
                <option key={b.id} value={b.slug}>{b.name}</option>
              ))}
            </select>
          </label>
          <label>
            Sortiranje
            <select value={sortBy} onChange={(e) => setFilter('sort', e.target.value)}>
              <option value="price_asc">Cena rastuće</option>
              <option value="price_desc">Cena opadajuće</option>
              <option value="power">Snaga</option>
              <option value="name">Naziv</option>
            </select>
          </label>

          <div>
            <strong style={{ display: 'block', marginBottom: '0.4rem' }}>Snaga (BTU)</strong>
            <div className="chips">
              <button
                type="button"
                className={`chip ${!coolingBtu ? 'active' : ''}`}
                onClick={() => setFilter('btu', '')}
              >
                Sve
              </button>
              {BTU_OPTIONS.map((btu) => (
                <button
                  key={btu}
                  type="button"
                  className={`chip ${coolingBtu === String(btu) ? 'active' : ''}`}
                  onClick={() => setFilter('btu', String(btu))}
                >
                  {btu.toLocaleString('sr-RS')}
                </button>
              ))}
            </div>
          </div>

          <div>
            <strong style={{ display: 'block', marginBottom: '0.4rem' }}>Tip</strong>
            <div className="chips">
              {CATEGORY_FILTERS.map((c) => (
                <Link
                  key={c.label}
                  className={`chip ${categorySlug === c.slug || (!categorySlug && !c.slug) ? 'active' : ''}`}
                  to={c.slug ? `/kategorija/${c.slug}` : '/katalog'}
                >
                  {c.label}
                </Link>
              ))}
            </div>
          </div>
        </aside>

        <section className="stack">
          <p className="muted">
            {products.data?.totalCount ?? 0} proizvoda
            {brandSlug || coolingBtu || categorySlug
              ? ` · ${[
                  brandSlug && brands.data?.find((b) => b.slug === brandSlug)?.name,
                  coolingBtu && `${Number(coolingBtu).toLocaleString('sr-RS')} BTU`,
                  categorySlug && categoryLabel(categorySlug),
                ].filter(Boolean).join(' · ')}`
              : ''}
          </p>
          <div className="grid products">
            {products.data?.items.map((p) => (
              <Link key={p.id} to={`/proizvod/${p.slug}`} className="product-card">
                <div className="media">
                  <span className="tag">{p.isInverter ? 'INVERTER' : 'ON/OFF'}</span>
                  {p.primaryImageUrl ? (
                    <img src={p.primaryImageUrl} alt={`${p.name} klima uređaj`} loading="lazy" />
                  ) : (
                    p.brandName
                  )}
                </div>
                <div className="body">
                  <h3>{p.name}</h3>
                  <div className="meta">
                    {p.coolingBtu > 0
                      ? `${p.coolingBtu.toLocaleString('sr-RS')} BTU · `
                      : ''}
                    {p.coolingCapacityKw} kW · {ENERGY_LABELS[p.energyClassCooling] ?? 'A'}
                  </div>
                  <div className="price">{formatRsd(p.price)}</div>
                </div>
              </Link>
            ))}
          </div>
        </section>
      </div>
    </div>
    </div>
  );
}
