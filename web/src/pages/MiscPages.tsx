import { Link, useSearchParams } from 'react-router-dom';
import { Seo } from '../components/Seo';
import { privacySeo } from '../seo';

export function ThankYouPage() {
  const [params] = useSearchParams();
  const order = params.get('order');
  return (
    <div className="shell page-pad stack" style={{ maxWidth: 560 }}>
      <Seo title="Hvala na porudžbini" path="/hvala" noindex />
      <h1 style={{ fontFamily: 'var(--display)', margin: 0, textTransform: 'uppercase', color: 'var(--navy)' }}>
        Hvala na porudžbini
      </h1>
      {order && <p>Broj porudžbine: <strong>{order}</strong></p>}
      <p className="muted">Kontaktiraćemo vas uskoro oko dostave{order ? ' i eventualne montaže' : ''}.</p>
      <Link className="btn" to="/katalog">Nazad u katalog</Link>
    </div>
  );
}

export function PrivacyPage() {
  return (
    <div className="shell page-pad">
      <Seo {...privacySeo()} />
      <article className="panel stack">
        <h1 style={{ fontFamily: 'var(--display)', margin: 0, textTransform: 'uppercase', color: 'var(--navy)' }}>
          Politika privatnosti
        </h1>
        <p>
          ElsInt (kontrolor podataka) obrađuje lične podatke kupaca radi izvršenja ugovora o kupoprodaji
          klima uređaja i pratećih usluga montaže/dostave, u skladu sa Zakonom o zaštiti podataka o ličnosti RS
          i GDPR principima.
        </p>
        <h2>Koje podatke prikupljamo</h2>
        <ul>
          <li>Identifikacioni i kontakt podaci (ime, email, telefon, adresa)</li>
          <li>Podaci o porudžbini i plaćanju</li>
          <li>Saglasnosti (kolačići, marketing)</li>
        </ul>
        <h2>Vaša prava</h2>
        <p>
          Imate pravo na uvid, ispravku, brisanje, ograničenje obrade i prenosivost. Za zahteve pišite na
          privacy@elsintdm.rs.
        </p>
        <p className="muted">Verzija politike: 1.0</p>
      </article>
    </div>
  );
}
