import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api, type Category, type PagedResult, type ProductListItem, formatRsd } from '../api';
import { HomeSeo } from '../components/Seo';

export function HomePage() {
  const categories = useQuery({
    queryKey: ['categories'],
    queryFn: () => api<Category[]>('/api/catalog/categories'),
  });
  const products = useQuery({
    queryKey: ['home-products'],
    queryFn: () => api<PagedResult<ProductListItem>>('/api/catalog/products?pageSize=6&sortBy=price_asc'),
  });

  return (
    <>
      <HomeSeo />
      <section className="hero">
        <div className="shell">
          <div>
            <div className="hero-kicker">Novi Sad · Srbija</div>
            <h1>Montaža i prodaja klima uređaja</h1>
            <p>
              Profesionalna ugradnja, servis i prodaja - obezbedite svežinu tokom cele godine.
              Brz sajt, jasne cene, bez čekanja.
            </p>
            <div className="row hero-cta">
              <Link className="btn" to="/katalog">Pogledaj katalog</Link>
              <a className="btn secondary" href="tel:+381677627904">Pozovite nas</a>
            </div>
          </div>
          <aside className="hero-panel">
            <strong>Zašto ElsInt?</strong>
            <div className="hero-stat"><span>Odgovor</span><span>isti dan</span></div>
            <div className="hero-stat"><span>Montaža</span><span>termin po dogovoru</span></div>
            <div className="hero-stat"><span>Garancija</span><span>na rad i uređaj</span></div>
            <div className="hero-stat"><span>Plaćanje</span><span>pouzeće · virman</span></div>
          </aside>
        </div>
      </section>

      <div className="shell">
        <section className="services">
          <article className="service-card">
            <h2>Montaža klima</h2>
            <p>Ugradnja split sistema, sa dogovorenim terminom i čistim završetkom.</p>
          </article>
          <article className="service-card">
            <h2>Servis klima uređaja</h2>
            <p>Redovan servis, čišćenje, dopuna freona i dijagnostika - da klima traje i troši manje.</p>
          </article>
          <article className="service-card">
            <h2>Prodaja klima</h2>
            <p>Inverter i on/off modeli sa jasnim cenama. Online porudžbina sa dostavom ili dostavom + montažom.</p>
          </article>
        </section>

        <section style={{ marginBottom: '2rem' }}>
          <div className="section-head">
            <h2>Iz ponude izdvajamo</h2>
            <Link to="/katalog">Celokupna ponuda →</Link>
          </div>
          <div className="grid products">
            {products.data?.items.map((p) => (
              <Link key={p.id} to={`/proizvod/${p.slug}`} className="product-card">
                <div className="media">
                  <span className="tag">{p.isInverter ? 'INVERTER' : 'ON/OFF'}</span>
                  {p.primaryImageUrl ? (
                    <img src={p.primaryImageUrl} alt={`${p.name} - klima uređaj ${p.brandName}`} loading="lazy" />
                  ) : (
                    p.brandName
                  )}
                </div>
                <div className="body">
                  <h3>{p.name}</h3>
                  <div className="meta">{p.brandName} · {p.coolingCapacityKw} kW · do {p.coverageAreaSqm} m²</div>
                  <div className="price">{formatRsd(p.price)}</div>
                </div>
              </Link>
            ))}
          </div>
        </section>

        <section style={{ marginBottom: '2.5rem' }}>
          <div className="section-head">
            <h2>Kategorije klima uređaja</h2>
          </div>
          <div className="chips">
            {categories.data?.map((c) => (
              <Link key={c.id} className="chip" to={`/kategorija/${c.slug}`}>{c.name}</Link>
            ))}
          </div>
        </section>

        <section className="panel stack" style={{ marginBottom: '2rem' }}>
          <h2 style={{ margin: 0, fontFamily: 'var(--display)', textTransform: 'uppercase', color: 'var(--navy)' }}>
            Klima uređaji - prodaja i montaža
          </h2>
          <p className="muted" style={{ margin: 0, maxWidth: '70ch' }}>
            ElsInt nudi prodaju i montažu klima uređaja širom Srbije: split sisteme, inverter modele
            (9000-24000 BTU) i mobilne klime. Uporedite cene u katalogu, poručite online (pouzeće ili virman)
            i zakažite ugradnju. Savetujemo oko snage uređaja prema kvadraturi prostorije.
          </p>
          <div className="row">
            <Link className="btn" to="/katalog">Katalog</Link>
            <Link className="btn secondary on-light" to="/checkout">Naruči online</Link>
          </div>
        </section>
      </div>
    </>
  );
}
