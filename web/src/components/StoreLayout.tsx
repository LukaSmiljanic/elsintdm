import { Link, NavLink, Outlet } from 'react-router-dom';
import { useState } from 'react';
import { api } from '../api';
import { useCart } from '../cart';

export function StoreLayout() {
  const { count } = useCart();
  return (
    <>
      <div className="topbar">
        <div className="shell">
          <span>Prodaja · montaža · servis klima uređaja</span>
          <span className="row" style={{ gap: '1rem' }}>
            <a href="tel:+381677627904">+381 67 762 7904</a>
            <Link to="/katalog">Katalog</Link>
          </span>
        </div>
      </div>
      <header className="site-header">
        <div className="shell nav">
          <Link to="/" className="brand" aria-label="ElsInt početna">
            <img src="/images/brand/elsint-logo.png" alt="ElsInt" />
          </Link>
          <nav className="nav-links">
            <NavLink to="/" end>Početna</NavLink>
            <NavLink to="/katalog">Katalog</NavLink>
            <NavLink to="/zakazivanje">Zakazivanje</NavLink>
            <NavLink to="/korpa">Korpa{count > 0 && <span className="badge">{count}</span>}</NavLink>
          </nav>
        </div>
      </header>
      <main>
        <Outlet />
      </main>
      <footer className="site-footer">
        <div className="shell footer-grid">
          <div>
            <img src="/images/brand/elsint-logo.png" alt="ElsInt" style={{ height: 72, marginBottom: 12 }} />
            <p style={{ margin: 0 }}>
              ElsInt - prodaja, montaža i servis klima uređaja. Brza ponuda, jasne cene, pouzdana ugradnja.
            </p>
          </div>
          <div>
            <h4>Montaža</h4>
            <ul>
              <li><Link to="/katalog">Redovne montaže</Link></li>
              <li><Link to="/checkout">Poruči sa montažom</Link></li>
            </ul>
          </div>
          <div>
            <h4>Servis</h4>
            <ul>
              <li><Link to="/zakazivanje">Zakaži termin</Link></li>
              <li><a href="tel:+381677627904">Hitne intervencije</a></li>
            </ul>
          </div>
          <div>
            <h4>Prodaja</h4>
            <ul>
              <li><Link to="/katalog">Katalog</Link></li>
              <li><Link to="/kategorija/split-sistemi">Split sistemi</Link></li>
              <li><Link to="/proizvod/ariston-aeres-net-35">Ariston Aeres Net</Link></li>
            </ul>
          </div>
        </div>
        <div className="shell footer-bottom">
          <span>© {new Date().getFullYear()} ElsInt. Sva prava zadržana.</span>
          <Link to="/privatnost">Privatnost</Link>
        </div>
      </footer>
      <CookieBanner />
    </>
  );
}

function CookieBanner() {
  const [visible, setVisible] = useState(() => localStorage.getItem('elsint_consent') !== '1');
  if (!visible) return null;

  const accept = async (accepted: boolean) => {
    localStorage.setItem('elsint_consent', accepted ? '1' : '0');
    try {
      await api('/api/consent', {
        method: 'POST',
        body: JSON.stringify({ consentType: 'cookies', policyVersion: '1.0', accepted }),
      });
    } catch {
      // non-blocking
    }
    setVisible(false);
  };

  return (
    <div className="cookie-banner">
      <p style={{ margin: 0 }}>
        Koristimo kolačiće radi rada sajta. Detalji u{' '}
        <Link to="/privatnost" style={{ textDecoration: 'underline' }}>Politici privatnosti</Link>.
      </p>
      <div className="row">
        <button className="btn" onClick={() => accept(true)}>Prihvatam</button>
        <button className="btn secondary" onClick={() => accept(false)}>Samo neophodni</button>
      </div>
    </div>
  );
}
