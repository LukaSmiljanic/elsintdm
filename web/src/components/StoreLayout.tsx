import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { api } from '../api';
import { useCart } from '../cart';

export function StoreLayout() {
  const { count } = useCart();
  const { pathname, hash } = useLocation();

  useEffect(() => {
    if (hash) {
      const id = decodeURIComponent(hash.slice(1));
      const t = window.setTimeout(() => {
        document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 80);
      return () => window.clearTimeout(t);
    }
    window.scrollTo(0, 0);
  }, [pathname, hash]);
  return (
    <>
      <div className="topbar">
        <div className="shell">
          <span>Ugradnja i servis klime · Novi Sad</span>
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
            <NavLink to="/ugradnja-klime-novi-sad">Ugradnja</NavLink>
            <NavLink to="/servis-klime-novi-sad">Servis</NavLink>
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
              ElsInt — ugradnja, montaža i servis klima uređaja u Novom Sadu. Jasne cene, dogovoreni termin.
            </p>
            <p style={{ margin: '0.6rem 0 0' }}>
              <a href="tel:+381677627904">+381 67 762 7904</a>
              <span> · Novi Sad</span>
            </p>
          </div>
          <div>
            <h4>Montaža</h4>
            <ul>
              <li><Link to="/ugradnja-klime-novi-sad">Ugradnja klime Novi Sad</Link></li>
              <li><Link to="/katalog">Katalog sa montažom</Link></li>
              <li><Link to="/zakazivanje">Zakaži montažu</Link></li>
            </ul>
          </div>
          <div>
            <h4>Servis</h4>
            <ul>
              <li><Link to="/servis-klime-novi-sad">Servis klime Novi Sad</Link></li>
              <li><Link to="/servis-klime-novi-sad#pranje">Pranje klime</Link></li>
              <li><Link to="/zakazivanje">Zakaži termin</Link></li>
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
