import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api, type Category, type PagedResult, type ProductListItem, formatRsd } from '../api';
import { FaqList } from '../components/Faq';
import { HomeSeo } from '../components/Seo';
import { HOME_FAQ } from '../seo';

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
            <h1>Ugradnja i servis klime u Novom Sadu</h1>
            <p>
              Montaža split sistema, pranje, dopuna freona i prodaja klima uređaja.
              Dolazimo na vašu adresu — zakažite termin ili pozovite.
            </p>
            <div className="row hero-cta">
              <Link className="btn" to="/zakazivanje">Zakažite termin</Link>
              <a className="btn secondary" href="tel:+381677627904">+381 67 762 7904</a>
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
          <Link to="/ugradnja-klime-novi-sad" className="service-card">
            <h2>Ugradnja klime Novi Sad</h2>
            <p>Montaža split i inverter sistema, sa dogovorenim terminom i čistim završetkom.</p>
          </Link>
          <Link to="/servis-klime-novi-sad" className="service-card">
            <h2>Servis klime Novi Sad</h2>
            <p>Pranje, dijagnostika, dopuna freona i popravka — da klima hladi i troši manje.</p>
          </Link>
          <Link to="/katalog" className="service-card">
            <h2>Prodaja klima uređaja</h2>
            <p>Inverter i on/off modeli sa jasnim cenama. Dostava ili dostava plus montaža.</p>
          </Link>
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
            Klima servis i montaža u Novom Sadu
          </h2>
          <p className="muted" style={{ margin: 0, maxWidth: '70ch' }}>
            ElsInt je lokalna firma za ugradnju, servis i prodaju klima uređaja u Novom Sadu.
            Radimo split i inverter modele (9000–24000 BTU), pranje unutrašnje i spoljne jedinice
            i montažu na stanovima i kućama. Dolazimo na Liman, Telep, Detelinaru, Petrovaradin,
            Veternik, Futog i ostale delove grada. Uporedite cene u katalogu, poručite online
            ili zakažite dolazak.
          </p>
          <div className="row">
            <Link className="btn" to="/zakazivanje">Zakažite uslugu</Link>
            <Link className="btn secondary on-light" to="/katalog">Katalog klima</Link>
          </div>
        </section>

        <section className="panel stack" style={{ marginBottom: '2rem' }}>
          <h2 style={{ margin: 0, fontFamily: 'var(--display)', textTransform: 'uppercase', color: 'var(--navy)' }}>
            Česta pitanja
          </h2>
          <FaqList items={HOME_FAQ} />
        </section>
      </div>
    </>
  );
}
