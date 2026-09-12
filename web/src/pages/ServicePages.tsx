import { Link } from 'react-router-dom';
import { FaqList } from '../components/Faq';
import { Seo } from '../components/Seo';
import {
  SERVIS_FAQ,
  UGRADNJA_FAQ,
  servisSeo,
  ugradnjaSeo,
} from '../seo';

const AREAS = [
  'Liman',
  'Telep',
  'Detelinara',
  'Grbavica',
  'Petrovaradin',
  'Sremska Kamenica',
  'Veternik',
  'Futog',
  'Kać',
  'Rumenka',
];

function ServiceHero({
  kicker,
  title,
  lead,
}: {
  kicker: string;
  title: string;
  lead: string;
}) {
  return (
    <section className="hero page-hero">
      <div className="shell">
        <div>
          <div className="hero-kicker">{kicker}</div>
          <h1>{title}</h1>
          <p>{lead}</p>
          <div className="row hero-cta">
            <Link className="btn" to="/zakazivanje">
              Zakažite termin
            </Link>
            <a className="btn secondary" href="tel:+381677627904">
              +381 67 762 7904
            </a>
          </div>
        </div>
      </div>
    </section>
  );
}

function AreaList() {
  return (
    <ul className="area-list">
      {AREAS.map((area) => (
        <li key={area}>{area}</li>
      ))}
    </ul>
  );
}

export function UgradnjaKlimePage() {
  return (
    <>
      <Seo {...ugradnjaSeo()} />
      <ServiceHero
        kicker="ElsInt · Novi Sad"
        title="Ugradnja klime Novi Sad"
        lead="Profesionalna montaža split i inverter klima uređaja u Novom Sadu. Dogovoreni termin, čist završetak i savet oko snage uređaja."
      />
      <div className="shell seo-page">
        <article className="seo-copy stack">
          <p>
            Tražite <strong>ugradnju klime u Novom Sadu</strong>? ElsInt radi montažu split
            sistema na kućama i stanovima — od Limana i Telepa do Petrovaradina i Sremske
            Kamenice. Uređaj možete uzeti iz našeg{' '}
            <Link to="/katalog">kataloga</Link> ili da ugradimo klimu koju već imate.
          </p>

          <h2>Montaža klima uređaja u Novom Sadu</h2>
          <p>
            Standardna montaža obuhvata postavljanje unutrašnje i spoljne jedinice, bušenje
            prolaza, povezivanje bakarnih cevi, vakuumiranje sistema i prvi start. Duži cevni
            set, dekor kanalice, nosači na fasadi ili rad na višim spratovima dogovaramo
            pre početka, da cena bude jasna.
          </p>

          <section className="services seo-points">
            <article className="service-card">
              <h3>Split i inverter</h3>
              <p>Ugrađujemo 9000–24000 BTU modele. Pomažemo da izaberete snagu prema kvadraturi.</p>
            </article>
            <article className="service-card">
              <h3>Termin po dogovoru</h3>
              <p>Zakazivanje online ili telefonom. Potvrda ide pozivom, bez čekanja „možda sutra“.</p>
            </article>
            <article className="service-card">
              <h3>Garancija na rad</h3>
              <p>Posle montaže proveravamo hlađenje i odvod kondenza. Garancija važi na izvedeni rad.</p>
            </article>
          </section>

          <h2>Kako izgleda ugradnja</h2>
          <ol className="seo-steps">
            <li>Pošaljete zahtev ili nas pozovete — dogovorimo model, mesto i termin.</li>
            <li>Na licu mesta proverimo nosač, odvod i dužinu instalacije.</li>
            <li>Montiramo unutrašnju i spoljnu jedinicu, vakuumiramo i pustimo sistem.</li>
            <li>Objasnimo korišćenje i ostavimo prostor uredan.</li>
          </ol>

          <h2>Gde radimo ugradnju klime</h2>
          <p>
            Dolazimo na adrese širom Novog Sada i bliže okoline. Ako niste sigurni da li
            pokrivamo vaš kraj, pozovite — javljamo odmah.
          </p>
          <AreaList />

          <div className="row" style={{ marginTop: '0.5rem' }}>
            <Link className="btn" to="/zakazivanje">
              Zakažite montažu
            </Link>
            <Link className="btn secondary on-light" to="/katalog">
              Pogledajte klime
            </Link>
          </div>
        </article>

        <section className="panel stack" style={{ marginTop: '2rem' }}>
          <h2 className="seo-h2">Česta pitanja o ugradnji klime</h2>
          <FaqList items={UGRADNJA_FAQ} />
        </section>
      </div>
    </>
  );
}

export function ServisKlimePage() {
  return (
    <>
      <Seo {...servisSeo()} />
      <ServiceHero
        kicker="ElsInt · Novi Sad"
        title="Servis klime Novi Sad"
        lead="Pranje, dijagnostika, dopuna freona i popravka klima uređaja na vašoj adresi u Novom Sadu. Zakažite termin online."
      />
      <div className="shell seo-page">
        <article className="seo-copy stack">
          <p>
            <strong>Servis klime u Novom Sadu</strong> radimo na terenu: pregled, čišćenje,
            otklanjanje kvarova i savet šta dalje. Ako klima slabo hladi, neprijatno miriše,
            curi voda ili se često gasi, zakažite dolazak — dijagnostiku radimo na licu mesta.
          </p>

          <h2 id="pranje">Pranje klime Novi Sad</h2>
          <p>
            Redovno pranje unutrašnje i spoljne jedinice vraća protok vazduha, smanjuje potrošnju
            i uklanja neprijatan miris. Peremo filtere, isparivač i kondenzator. Uslugu možete
            rezervisati kao „Pranje klime“ na stranici{' '}
            <Link to="/zakazivanje">zakazivanja</Link>.
          </p>

          <section className="services seo-points">
            <article className="service-card">
              <h3>Redovan servis</h3>
              <p>Pregled, čišćenje i provera rada pred sezonu, da klima traje i troši manje.</p>
            </article>
            <article className="service-card">
              <h3>Dopuna freona</h3>
              <p>Provera pritiska i curenja, zatim dopuna gasa prema specifikaciji uređaja.</p>
            </article>
            <article className="service-card">
              <h3>Popravka</h3>
              <p>Elektronika, ventilator, odvod kondenza i ostali kvarovi — cena nakon uvida.</p>
            </article>
          </section>

          <h2>Kada zove servis klime</h2>
          <ul className="seo-bullets">
            <li>Klima duva topao vazduh ili slabo hladi.</li>
            <li>Čuje se šum, klepet ili klima se često gasi.</li>
            <li>Sa unutrašnje jedinice curi voda.</li>
            <li>Prostorija ima vlažan ili ustajao miris kad je klima uključena.</li>
          </ul>

          <h2>Servis klime po Novom Sadu</h2>
          <p>
            Dolazimo na Liman, Telep, Detelinaru, Petrovaradin, Veternik, Futog i ostale delove
            grada. Termin birate online; potvrda ide telefonom.
          </p>
          <AreaList />

          <div className="row" style={{ marginTop: '0.5rem' }}>
            <Link className="btn" to="/zakazivanje">
              Zakažite servis
            </Link>
            <Link className="btn secondary on-light" to="/ugradnja-klime-novi-sad">
              Ugradnja klime
            </Link>
          </div>
        </article>

        <section className="panel stack" style={{ marginTop: '2rem' }}>
          <h2 className="seo-h2">Česta pitanja o servisu klime</h2>
          <FaqList items={SERVIS_FAQ} />
        </section>
      </div>
    </>
  );
}
