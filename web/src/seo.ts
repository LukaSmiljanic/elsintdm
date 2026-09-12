export const SITE_URL = (import.meta.env.VITE_SITE_URL ?? 'https://elsintdm.rs').replace(/\/$/, '');
export const SITE_NAME = 'ElsInt';
export const BUSINESS_PHONE = '+381677627904';
export const BUSINESS_PHONE_DISPLAY = '+381 67 762 7904';
export const BUSINESS_CITY = 'Novi Sad';
export const DEFAULT_DESCRIPTION =
  'Ugradnja, montaža i servis klima uređaja u Novom Sadu. Split i inverter klime, pranje, dopuna freona. Zakažite termin ili pozovite ElsInt.';
const DEFAULT_IMAGE = '/images/brand/elsint-logo.png';
const BUSINESS_ID = `${SITE_URL}/#business`;

export type SeoInput = {
  title: string;
  description?: string;
  path?: string;
  image?: string;
  type?: 'website' | 'product' | 'article';
  noindex?: boolean;
  jsonLd?: Record<string, unknown> | Record<string, unknown>[];
};

export type SeoTag =
  | { tag: 'meta'; attr: 'name' | 'property'; key: string; content: string }
  | { tag: 'link'; rel: string; href: string };

export type ResolvedSeo = {
  title: string;
  tags: SeoTag[];
  jsonLd?: Record<string, unknown> | Record<string, unknown>[];
};

/** Single source of truth for the head of every page — used by the DOM at runtime and by the prerender step. */
export function resolveSeo({
  title,
  description = DEFAULT_DESCRIPTION,
  path = '/',
  image = `${SITE_URL}${DEFAULT_IMAGE}`,
  type = 'website',
  noindex = false,
  jsonLd,
}: SeoInput): ResolvedSeo {
  const fullTitle = title.includes(SITE_NAME) ? title : `${title} | ${SITE_NAME}`;
  const url = `${SITE_URL}${path.startsWith('/') ? path : `/${path}`}`;
  const absImage = image.startsWith('http') ? image : `${SITE_URL}${image}`;

  return {
    title: fullTitle,
    jsonLd,
    tags: [
      { tag: 'meta', attr: 'name', key: 'description', content: description },
      {
        tag: 'meta',
        attr: 'name',
        key: 'robots',
        content: noindex ? 'noindex, nofollow' : 'index, follow, max-image-preview:large',
      },
      { tag: 'link', rel: 'canonical', href: url },
      { tag: 'meta', attr: 'property', key: 'og:site_name', content: SITE_NAME },
      { tag: 'meta', attr: 'property', key: 'og:locale', content: 'sr_RS' },
      { tag: 'meta', attr: 'property', key: 'og:type', content: type },
      { tag: 'meta', attr: 'property', key: 'og:title', content: fullTitle },
      { tag: 'meta', attr: 'property', key: 'og:description', content: description },
      { tag: 'meta', attr: 'property', key: 'og:url', content: url },
      { tag: 'meta', attr: 'property', key: 'og:image', content: absImage },
      { tag: 'meta', attr: 'name', key: 'twitter:card', content: 'summary_large_image' },
      { tag: 'meta', attr: 'name', key: 'twitter:title', content: fullTitle },
      { tag: 'meta', attr: 'name', key: 'twitter:description', content: description },
      { tag: 'meta', attr: 'name', key: 'twitter:image', content: absImage },
    ],
  };
}

function upsertMeta(attr: 'name' | 'property', key: string, content: string) {
  let el = document.head.querySelector<HTMLMetaElement>(`meta[${attr}="${key}"]`);
  if (!el) {
    el = document.createElement('meta');
    el.setAttribute(attr, key);
    document.head.appendChild(el);
  }
  el.setAttribute('content', content);
}

function upsertLink(rel: string, href: string) {
  let el = document.head.querySelector<HTMLLinkElement>(`link[rel="${rel}"]`);
  if (!el) {
    el = document.createElement('link');
    el.setAttribute('rel', rel);
    document.head.appendChild(el);
  }
  el.setAttribute('href', href);
}

function upsertJsonLd(data: Record<string, unknown> | Record<string, unknown>[]) {
  const id = 'elsint-jsonld';
  let el = document.getElementById(id) as HTMLScriptElement | null;
  if (!el) {
    el = document.createElement('script');
    el.id = id;
    el.type = 'application/ld+json';
    document.head.appendChild(el);
  }
  el.textContent = JSON.stringify(data);
}

export function applySeo(input: SeoInput) {
  const { title, tags, jsonLd } = resolveSeo(input);

  document.title = title;
  for (const tag of tags) {
    if (tag.tag === 'meta') upsertMeta(tag.attr, tag.key, tag.content);
    else upsertLink(tag.rel, tag.href);
  }
  if (jsonLd) upsertJsonLd(jsonLd);
  else document.getElementById('elsint-jsonld')?.remove();
}

export type FaqItem = { q: string; a: string };

export function organizationJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'HVACBusiness',
    '@id': BUSINESS_ID,
    name: SITE_NAME,
    url: SITE_URL,
    logo: `${SITE_URL}${DEFAULT_IMAGE}`,
    image: `${SITE_URL}${DEFAULT_IMAGE}`,
    description: DEFAULT_DESCRIPTION,
    telephone: BUSINESS_PHONE,
    priceRange: '$$',
    currenciesAccepted: 'RSD',
    paymentAccepted: 'Cash, Bank Transfer',
    address: {
      '@type': 'PostalAddress',
      addressLocality: BUSINESS_CITY,
      addressRegion: 'Vojvodina',
      addressCountry: 'RS',
    },
    geo: {
      '@type': 'GeoCoordinates',
      latitude: 45.267136,
      longitude: 19.833549,
    },
    areaServed: [
      { '@type': 'City', name: 'Novi Sad' },
      { '@type': 'City', name: 'Petrovaradin' },
      { '@type': 'City', name: 'Sremska Kamenica' },
      { '@type': 'AdministrativeArea', name: 'Južnobački okrug' },
    ],
    knowsLanguage: 'sr',
    hasOfferCatalog: {
      '@type': 'OfferCatalog',
      name: 'Usluge klima uređaja',
      itemListElement: [
        {
          '@type': 'Offer',
          itemOffered: {
            '@type': 'Service',
            name: 'Ugradnja klime',
            url: `${SITE_URL}/ugradnja-klime-novi-sad`,
          },
        },
        {
          '@type': 'Offer',
          itemOffered: {
            '@type': 'Service',
            name: 'Servis klime',
            url: `${SITE_URL}/servis-klime-novi-sad`,
          },
        },
        {
          '@type': 'Offer',
          itemOffered: {
            '@type': 'Service',
            name: 'Pranje klime',
            url: `${SITE_URL}/servis-klime-novi-sad#pranje`,
          },
        },
      ],
    },
  };
}

export function faqJsonLd(items: FaqItem[]) {
  return {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: items.map((item) => ({
      '@type': 'Question',
      name: item.q,
      acceptedAnswer: {
        '@type': 'Answer',
        text: item.a,
      },
    })),
  };
}

export function serviceJsonLd(input: {
  name: string;
  description: string;
  path: string;
  serviceType: string;
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Service',
    name: input.name,
    serviceType: input.serviceType,
    description: input.description,
    url: `${SITE_URL}${input.path}`,
    provider: { '@id': BUSINESS_ID },
    areaServed: { '@type': 'City', name: BUSINESS_CITY },
    brand: { '@type': 'Brand', name: SITE_NAME },
  };
}

export function productJsonLd(p: {
  name: string;
  description?: string;
  sku: string;
  slug: string;
  price: number;
  brandName: string;
  imageUrl?: string;
  available: boolean;
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Product',
    name: p.name,
    description: p.description || p.name,
    sku: p.sku,
    brand: { '@type': 'Brand', name: p.brandName },
    image: p.imageUrl ? [p.imageUrl.startsWith('http') ? p.imageUrl : `${SITE_URL}${p.imageUrl}`] : undefined,
    offers: {
      '@type': 'Offer',
      url: `${SITE_URL}/proizvod/${p.slug}`,
      priceCurrency: 'RSD',
      price: p.price,
      availability: p.available
        ? 'https://schema.org/InStock'
        : 'https://schema.org/OutOfStock',
      itemCondition: 'https://schema.org/NewCondition',
    },
  };
}

export function breadcrumbJsonLd(items: { name: string; path: string }[]) {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, i) => ({
      '@type': 'ListItem',
      position: i + 1,
      name: item.name,
      item: `${SITE_URL}${item.path}`,
    })),
  };
}

// —— Per-page descriptors ——
// Pages pass these straight to <Seo>, and the prerender step feeds them into resolveSeo.
// Keeping the wording here means the static HTML and the rendered page can never disagree.

export const HOME_FAQ: FaqItem[] = [
  {
    q: 'Koliko košta ugradnja klime u Novom Sadu?',
    a: 'Standardna montaža split sistema je po dogovoru, u zavisnosti od dužine cevi, sprata i dodatnih radova. Cenu dobijate pre početka posla — zakažite termin ili nas pozovite.',
  },
  {
    q: 'Da li radite servis i pranje klime u Novom Sadu?',
    a: 'Da. Radimo redovan servis, pranje unutrašnje i spoljne jedinice, dijagnostiku i dopunu freona na adresama u Novom Sadu i okolini.',
  },
  {
    q: 'U koje delove Novog Sada dolazite?',
    a: 'Dolazimo na Liman, Telep, Detelinaru, Grbavicu, Petrovaradin, Sremsku Kamenicu, Veternik, Futog i ostale delove grada, uz dogovor za bližu okolicu.',
  },
  {
    q: 'Kako da zakažem montažu ili servis?',
    a: 'Termin možete rezervisati online na stranici Zakazivanje ili pozivom. Potvrđujemo termin telefonom.',
  },
];

export function homeSeo(): SeoInput {
  return {
    title: 'Ugradnja i servis klime Novi Sad',
    description: DEFAULT_DESCRIPTION,
    path: '/',
    jsonLd: [organizationJsonLd(), faqJsonLd(HOME_FAQ)],
  };
}

export type SeoCategory = {
  name: string;
  slug: string;
  description?: string;
  metaTitle?: string;
  metaDescription?: string;
};

export function catalogSeo(category?: SeoCategory): SeoInput {
  const path = category ? `/kategorija/${category.slug}` : '/katalog';
  return {
    title: category
      ? category.metaTitle || `${category.name} - klima uređaji`
      : 'Katalog klima uređaja Novi Sad - cene i modeli',
    description:
      category?.metaDescription ||
      'Katalog klima uređaja za Novi Sad: split, inverter i mobilne klime. Filtrirajte po brendu i BTU snazi. Prodaja, dostava i montaža - ElsInt.',
    path,
    jsonLd: breadcrumbJsonLd([
      { name: 'Početna', path: '/' },
      { name: category?.name || 'Katalog', path },
    ]),
  };
}

export type SeoProduct = {
  name: string;
  slug: string;
  sku: string;
  price: number;
  brandName: string;
  categoryName: string;
  categorySlug: string;
  availableStock: number;
  shortDescription?: string;
  description?: string;
  metaTitle?: string;
  metaDescription?: string;
  images?: { url: string; altText?: string; isPrimary: boolean }[];
};

export function productSeo(p: SeoProduct): SeoInput {
  const image = p.images?.find((i) => i.isPrimary) ?? p.images?.[0];
  return {
    title: p.metaTitle || `${p.name} - cena i montaža`,
    description:
      p.metaDescription || `${p.name}. Prodaja i montaža klima uređaja u Novom Sadu - ElsInt.`,
    path: `/proizvod/${p.slug}`,
    image: image?.url,
    type: 'product',
    jsonLd: [
      productJsonLd({
        name: p.name,
        description: p.metaDescription || p.shortDescription || p.description,
        sku: p.sku,
        slug: p.slug,
        price: p.price,
        brandName: p.brandName,
        imageUrl: image?.url,
        available: p.availableStock > 0,
      }),
      breadcrumbJsonLd([
        { name: 'Početna', path: '/' },
        { name: 'Katalog', path: '/katalog' },
        { name: p.categoryName, path: `/kategorija/${p.categorySlug}` },
        { name: p.name, path: `/proizvod/${p.slug}` },
      ]),
    ],
  };
}

export function bookingSeo(): SeoInput {
  return {
    title: 'Zakazivanje servisa klime Novi Sad',
    description:
      'Zakažite pranje, servis ili ugradnju klime u Novom Sadu. Izaberite termin online, potvrda ide telefonom.',
    path: '/zakazivanje',
  };
}

export const UGRADNJA_FAQ: FaqItem[] = [
  {
    q: 'Šta ulazi u standardnu ugradnju klime?',
    a: 'Standardna montaža split sistema obuhvata postavljanje unutrašnje i spoljne jedinice, bušenje, povezivanje, vakuumiranje i prvi start. Duži cevni set, kanalice ili nosači na fasadi dogovaraju se posebno.',
  },
  {
    q: 'Koliko traje montaža klime?',
    a: 'Uobičajena ugradnja split klime traje oko 2 sata, u zavisnosti od pristupa fasadi i dužine instalacije.',
  },
  {
    q: 'Da li možete da ugradite klimu koju već imam?',
    a: 'Da. Montiramo uređaje iz naše ponude i klime koje ste već kupili, nakon dogovora oko termina i uslova ugradnje.',
  },
];

export function ugradnjaSeo(): SeoInput {
  const path = '/ugradnja-klime-novi-sad';
  const description =
    'Ugradnja i montaža klima uređaja u Novom Sadu. Split sistemi, inverter klime, dogovoreni termin i čist završetak. Zakažite montažu kod ElsInt.';
  return {
    title: 'Ugradnja klime Novi Sad',
    description,
    path,
    jsonLd: [
      organizationJsonLd(),
      serviceJsonLd({
        name: 'Ugradnja klime Novi Sad',
        description,
        path,
        serviceType: 'Ugradnja i montaža klima uređaja',
      }),
      faqJsonLd(UGRADNJA_FAQ),
      breadcrumbJsonLd([
        { name: 'Početna', path: '/' },
        { name: 'Ugradnja klime Novi Sad', path },
      ]),
    ],
  };
}

export const SERVIS_FAQ: FaqItem[] = [
  {
    q: 'Koliko često treba servisirati klimu?',
    a: 'Preporuka je bar jednom godišnje, idealno pred sezonu. Ako klima slabo hladi, neprijatno miriše ili curi, zakažite servis odmah.',
  },
  {
    q: 'Šta uključuje pranje klime?',
    a: 'Pranje obuhvata unutrašnju i spoljnu jedinicu: filtere, isparivač i kondenzator, da klima hladi bolje i troši manje.',
  },
  {
    q: 'Da li radite dopunu freona u Novom Sadu?',
    a: 'Da. Nakon dijagnostike proveravamo curenje i, ako je potrebno, dopunjujemo gas prema specifikaciji uređaja.',
  },
];

export function servisSeo(): SeoInput {
  const path = '/servis-klime-novi-sad';
  const description =
    'Servis klime Novi Sad: pranje, dijagnostika, dopuna freona i popravka. Dolazak na adresu, jasna cena i zakazivanje online.';
  return {
    title: 'Servis klime Novi Sad',
    description,
    path,
    jsonLd: [
      organizationJsonLd(),
      serviceJsonLd({
        name: 'Servis klime Novi Sad',
        description,
        path,
        serviceType: 'Servis i pranje klima uređaja',
      }),
      faqJsonLd(SERVIS_FAQ),
      breadcrumbJsonLd([
        { name: 'Početna', path: '/' },
        { name: 'Servis klime Novi Sad', path },
      ]),
    ],
  };
}

export function privacySeo(): SeoInput {
  return {
    title: 'Politika privatnosti',
    description:
      'Politika privatnosti ElsInt - obrada ličnih podataka kupaca pri prodaji i montaži klima uređaja.',
    path: '/privatnost',
  };
}
