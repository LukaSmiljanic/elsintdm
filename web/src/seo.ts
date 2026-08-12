export const SITE_URL = (import.meta.env.VITE_SITE_URL ?? 'https://elsintdm.rs').replace(/\/$/, '');
export const SITE_NAME = 'ElsInt';
export const DEFAULT_DESCRIPTION =
  'ElsInt - prodaja, montaža i servis klima uređaja u Srbiji. Split i mobilne klime, jasne cene, dostava i ugradnja.';
const DEFAULT_IMAGE = '/images/brand/elsint-logo.png';

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
}

export function organizationJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'HVACBusiness',
    name: SITE_NAME,
    url: SITE_URL,
    logo: `${SITE_URL}${DEFAULT_IMAGE}`,
    description: DEFAULT_DESCRIPTION,
    telephone: '+381677627904',
    areaServed: {
      '@type': 'Country',
      name: 'Serbia',
    },
    priceRange: '$$',
    sameAs: [] as string[],
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

export function homeSeo(): SeoInput {
  return {
    title: 'Prodaja, montaža i servis klima uređaja',
    description: DEFAULT_DESCRIPTION,
    path: '/',
    jsonLd: organizationJsonLd(),
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
      : 'Katalog klima uređaja - cene i modeli',
    description:
      category?.metaDescription ||
      'Pregledajte katalog klima uređaja: split, inverter i mobilne klime. Filtrirajte po brendu i BTU snazi. ElsInt - prodaja i montaža.',
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
    description: p.metaDescription || `${p.name}. Prodaja i montaža klima uređaja - ElsInt.`,
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
    title: 'Zakazivanje usluge',
    description: 'Zakažite pranje, servis ili montažu klime. Termini prema dostupnosti, potvrda telefonom.',
    path: '/zakazivanje',
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
