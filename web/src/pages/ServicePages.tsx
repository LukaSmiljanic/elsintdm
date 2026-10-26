import { Link } from 'react-router-dom';
import { FaqList } from '../components/Faq';
import { Seo } from '../components/Seo';
import {
  PRANJE_FAQ,
  SERVIS_FAQ,
  UGRADNJA_FAQ,
  pranjeSeo,
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
            i uklanja neprijatan miris. Peremo filtere, isparivač i kondenzator. Više o tome šta
            obuhvata{' '}
            <Link to="/pranje-klime-novi-sad">pranje klime u Novom Sadu</Link>, a termin možete
            rezervisati na stranici <Link to="/zakazivanje">zakazivanja</Link>.
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

export function PranjeKlimePage() {
  return (
    <>
      <Seo {...pranjeSeo()} />
      <ServiceHero
        kicker="ElsInt · Novi Sad"
        title="Pranje klime Novi Sad"
        lead="Dubinsko pranje unutrašnje i spoljne jedinice na vašoj adresi u Novom Sadu. Čist vazduh, bolje hlađenje i manja potrošnja struje."
      />
      <div className="shell seo-page">
        <article className="seo-copy stack">
          <p>
            <strong>Pranje klime u Novom Sadu</strong> radimo na adresi — u stanovima,
            kućama i poslovnim prostorima. Prljav filter i isparivač smanjuju protok vazduha,
            povećavaju potrošnju i šire neprijatan miris po prostoriji. Redovno pranje vraća
            klimu u stanje u kakvom treba da radi.
          </p>

          <h2>Šta obuhvata pranje klime</h2>
          <section className="services seo-points">
            <article className="service-card">
              <h3>Unutrašnja jedinica</h3>
              <p>Pranje filtera, isparivača i ventilatora, uz zaštitu zida i poda od prljave vode.</p>
            </article>
            <article className="service-card">
              <h3>Spoljna jedinica</h3>
              <p>Čišćenje kondenzatora od prašine, lišća i nečistoće, kada joj se bezbedno može prići.</p>
            </article>
            <article className="service-card">
              <h3>Provera rada</h3>
              <p>Provera odvoda kondenza i rada uređaja posle pranja. Ako primetimo kvar, kažemo vam odmah.</p>
            </article>
          </section>

          <h2>Zašto je redovno pranje važno</h2>
          <ul className="seo-bullets">
            <li>Klima hladi i greje jače, jer vazduh slobodno prolazi kroz isparivač.</li>
            <li>Manja potrošnja struje — kompresor ne radi duže nego što mora.</li>
            <li>Nema ustajalog mirisa i buđi u prostoriji.</li>
            <li>Duži vek uređaja i manje kvarova usred sezone.</li>
          </ul>

          <h2>Pranje klime po Novom Sadu</h2>
          <p>
            Dolazimo na Liman, Telep, Detelinaru, Grbavicu, Petrovaradin, Veternik, Futog i
            ostale delove grada. Pranje možete zakazati zajedno sa{' '}
            <Link to="/servis-klime-novi-sad">servisom klime</Link> ili dopunom freona, ako je
            potrebno.
          </p>
          <AreaList />

          <div className="row" style={{ marginTop: '0.5rem' }}>
            <Link className="btn" to="/zakazivanje">
              Zakažite pranje
            </Link>
            <Link className="btn secondary on-light" to="/servis-klime-novi-sad">
              Servis klime
            </Link>
          </div>
        </article>

        <section className="panel stack" style={{ marginTop: '2rem' }}>
          <h2 className="seo-h2">Česta pitanja o pranju klime</h2>
          <FaqList items={PRANJE_FAQ} />
        </section>
      </div>
    </>
  );
}
