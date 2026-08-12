import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api, ENERGY_LABELS, formatRsd, type ProductDetail } from '../api';
import { useCart } from '../cart';
import { Seo } from '../components/Seo';
import { productSeo } from '../seo';

export function ProductPage() {
  const { slug } = useParams();
  const { add } = useCart();
  const product = useQuery({
    queryKey: ['product', slug],
    queryFn: () => api<ProductDetail>(`/api/catalog/products/${slug}`),
    enabled: !!slug,
  });

  if (product.isLoading) return <div className="shell page-pad"><p>Učitavanje…</p></div>;
  if (product.isError || !product.data) {
    return (
      <div className="shell page-pad">
        <Seo title="Proizvod nije pronađen" path={`/proizvod/${slug ?? ''}`} noindex />
        <p className="error">Proizvod nije pronađen.</p>
      </div>
    );
  }

  const p = product.data;
  const image = p.images?.find((i) => i.isPrimary) ?? p.images?.[0];
  const imageUrl = image?.url;

  const specs: { label: string; value: string }[] = [
    p.coolingBtu > 0
      ? { label: 'Hlađenje', value: `${p.coolingCapacityKw} kW · ${p.coolingBtu.toLocaleString('sr-RS')} BTU` }
      : { label: 'Hlađenje', value: `${p.coolingCapacityKw} kW` },
    p.heatingCapacityKw > 0
      ? { label: 'Grejanje', value: `${p.heatingCapacityKw} kW${p.heatingBtu > 0 ? ` · ${p.heatingBtu.toLocaleString('sr-RS')} BTU` : ''}` }
      : { label: 'Grejanje', value: '-' },
    { label: 'Energetska klasa', value: ENERGY_LABELS[p.energyClassCooling] ?? '-' },
    p.coverageAreaSqm > 0
      ? { label: 'Površina', value: `do ${p.coverageAreaSqm} m²` }
      : { label: 'Površina', value: '-' },
    p.noiseLevelDb > 0
      ? { label: 'Buka', value: `${p.noiseLevelDb} dB` }
      : { label: 'Buka', value: '-' },
    { label: 'Inverter', value: p.isInverter ? 'Da' : 'Ne' },
    { label: 'WiFi', value: p.hasWifi ? 'Da' : 'Ne' },
    { label: 'SKU', value: p.sku },
  ];

  return (
    <div className="shell page-pad">
      <Seo {...productSeo(p)} />

      <nav className="pdp-crumb" aria-label="Breadcrumb">
        <Link to="/katalog">Katalog</Link>
        <span>/</span>
        <Link to={`/kategorija/${p.categorySlug}`}>{p.categoryName}</Link>
        <span>/</span>
        <span>{p.brandName}</span>
      </nav>

      <div className="pdp">
        <div className="pdp-gallery">
          <div className="pdp-media">
            {p.isInverter && <span className="tag">INVERTER</span>}
            {image?.url ? (
              <img src={image.url} alt={image.altText ?? `${p.name} klima uređaj`} />
            ) : (
              <span className="muted">{p.brandName}</span>
            )}
          </div>
        </div>

        <div className="pdp-info">
          <p className="pdp-brand">{p.brandName}</p>
          <h1 className="pdp-title">{p.name}</h1>
          {p.shortDescription && <p className="pdp-lead">{p.shortDescription}</p>}

          <div className="pdp-buy">
            <div className="pdp-price">{formatRsd(p.price)}</div>
            <button
              className="btn pdp-cta"
              onClick={() => add({ productId: p.id, quantity: 1, name: p.name, price: p.price, slug: p.slug })}
            >
              Dodaj u korpu
            </button>
          </div>

          <dl className="pdp-specs">
            {specs.map((s) => (
              <div key={s.label} className="pdp-spec">
                <dt>{s.label}</dt>
                <dd>{s.value}</dd>
              </div>
            ))}
          </dl>
        </div>
      </div>

      {p.description && (
        <section className="pdp-desc">
          <h2>Opis</h2>
          <p>{p.description}</p>
        </section>
      )}
    </div>
  );
}
