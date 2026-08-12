import {
  bookingSeo,
  catalogSeo,
  homeSeo,
  privacySeo,
  productSeo,
  resolveSeo,
  type SeoCategory,
  type SeoInput,
  type SeoProduct,
} from '../seo';

const API_BASE = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

export type PrerenderedRoute = {
  /** Route path, e.g. /proizvod/gree-lomo-12. */
  path: string;
  /** Replacement for the <!--seo--> block in index.html. */
  head: string;
  /** Replacement for the <!--app--> placeholder; React swaps it out on mount. */
  body: string;
};

function escapeHtml(value: string) {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

/** JSON-LD sits in a script tag, so only the closing-tag sequence needs neutralising. */
function escapeJsonLd(data: unknown) {
  return JSON.stringify(data).replace(/</g, '\\u003c');
}

function headHtml(input: SeoInput) {
  const { title, tags, jsonLd } = resolveSeo(input);
  const lines = [`<title>${escapeHtml(title)}</title>`];

  for (const tag of tags) {
    lines.push(
      tag.tag === 'meta'
        ? `<meta ${tag.attr}="${tag.key}" content="${escapeHtml(tag.content)}" />`
        : `<link rel="${tag.rel}" href="${escapeHtml(tag.href)}" />`
    );
  }
  if (jsonLd) {
    lines.push(`<script id="elsint-jsonld" type="application/ld+json">${escapeJsonLd(jsonLd)}</script>`);
  }

  return lines.join('\n    ');
}

function formatRsd(value: number) {
  return new Intl.NumberFormat('sr-RS', {
    style: 'currency',
    currency: 'RSD',
    maximumFractionDigits: 0,
  }).format(value);
}

/**
 * Minimal, honest copy of what the page shows, so the first crawl (before JavaScript runs)
 * finds a heading and a description instead of an empty shell. Deliberately kept to a
 * heading plus a line of text: anything richer would drift from the React components.
 */
function bodyHtml(heading: string, lines: string[]) {
  const paragraphs = lines.filter(Boolean).map((line) => `<p>${escapeHtml(line)}</p>`).join('');
  return `<div class="shell page-pad"><h1>${escapeHtml(heading)}</h1>${paragraphs}</div>`;
}

async function fetchJson<T>(path: string): Promise<T | null> {
  if (!API_BASE) return null;
  try {
    const res = await fetch(`${API_BASE}${path}`, { signal: AbortSignal.timeout(20000) });
    if (!res.ok) {
      console.warn(`[prerender] ${path} -> HTTP ${res.status}`);
      return null;
    }
    return (await res.json()) as T;
  } catch (err) {
    console.warn(`[prerender] ${path} failed: ${(err as Error).message}`);
    return null;
  }
}

export async function collectRoutes(): Promise<PrerenderedRoute[]> {
  const routes: PrerenderedRoute[] = [
    {
      path: '/',
      head: headHtml(homeSeo()),
      body: bodyHtml('ElsInt - klima uređaji, montaža i servis', [
        'Prodaja, montaža i servis klima uređaja. Split i mobilne klime, jasne cene, dostava i ugradnja.',
      ]),
    },
    {
      path: '/katalog',
      head: headHtml(catalogSeo()),
      body: bodyHtml('Katalog klima uređaja', [
        'Split, inverter i mobilne klime. Filtrirajte po brendu, snazi i energetskoj klasi.',
      ]),
    },
    {
      path: '/zakazivanje',
      head: headHtml(bookingSeo()),
      body: bodyHtml('Zakazivanje usluge', [
        'Izaberite uslugu, dan i sat. Termin se privremeno rezerviše, a potvrda ide pozivom.',
      ]),
    },
    {
      path: '/privatnost',
      head: headHtml(privacySeo()),
      body: bodyHtml('Politika privatnosti', [
        'Obrada ličnih podataka kupaca pri prodaji i montaži klima uređaja.',
      ]),
    },
  ];

  const categories = await fetchJson<SeoCategory[]>('/api/catalog/categories');
  for (const category of categories ?? []) {
    routes.push({
      path: `/kategorija/${category.slug}`,
      head: headHtml(catalogSeo(category)),
      body: bodyHtml(category.name, [category.description ?? '']),
    });
  }

  const products = await fetchJson<{ items: SeoProduct[] }>('/api/catalog/products?page=1&pageSize=500');
  for (const product of products?.items ?? []) {
    const detail = await fetchJson<SeoProduct>(`/api/catalog/products/${product.slug}`);
    const p = detail ?? product;
    routes.push({
      path: `/proizvod/${p.slug}`,
      head: headHtml(productSeo(p)),
      body: bodyHtml(p.name, [
        p.shortDescription || p.description || `${p.name} - klima uređaj ${p.brandName}.`,
        `Cena: ${formatRsd(p.price)}`,
      ]),
    });
  }

  return routes;
}
