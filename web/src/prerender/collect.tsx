import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderToString } from 'react-dom/server';
import { StaticRouter } from 'react-router-dom';
import { AppRoutes } from '../App';
import type { Category, PagedResult, ProductDetail, ProductListItem } from '../api';
import { CartProvider } from '../cart';
import { catalogProductsQuery } from '../pages/CatalogPage';
import {
  bookingSeo,
  catalogSeo,
  homeSeo,
  pranjeSeo,
  privacySeo,
  productSeo,
  resolveSeo,
  servisSeo,
  ugradnjaSeo,
  type SeoInput,
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

/** React Query cache entries to preload, keyed exactly as the page's own useQuery calls. */
type Seed = [queryKey: unknown[], data: unknown][];

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

/** Heading plus a line of text: what a page falls back to if rendering the real component fails. */
function bodyHtml(heading: string, lines: string[]) {
  const paragraphs = lines.filter(Boolean).map((line) => `<p>${escapeHtml(line)}</p>`).join('');
  return `<div class="shell page-pad"><h1>${escapeHtml(heading)}</h1>${paragraphs}</div>`;
}

/**
 * Renders the real page (layout, navigation, copy, product lists) so crawlers that don't run
 * JavaScript, and Google's first pass before rendering, see the same content and internal links
 * as visitors. The client mounts with createRoot, so this markup is replaced rather than hydrated.
 */
function renderPage(path: string, seed: Seed, fallback: string) {
  const client = new QueryClient({ defaultOptions: { queries: { staleTime: Infinity, retry: false } } });
  for (const [key, data] of seed) {
    if (data != null) client.setQueryData(key, data);
  }
  try {
    return renderToString(
      <QueryClientProvider client={client}>
        <CartProvider>
          <StaticRouter location={path}>
            <AppRoutes />
          </StaticRouter>
        </CartProvider>
      </QueryClientProvider>
    );
  } catch (err) {
    console.warn(`[prerender] ${path} render failed, using fallback: ${(err as Error).message}`);
    return fallback;
  } finally {
    client.clear();
  }
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
  const catalogQuery = (categorySlug?: string) =>
    catalogProductsQuery({ categorySlug, sortBy: 'price_asc' }).toString();

  const [categories, brands, homeProducts, catalogProducts, fieldServices] = await Promise.all([
    fetchJson<Category[]>('/api/catalog/categories'),
    fetchJson<{ id: string; name: string; slug: string }[]>('/api/catalog/brands'),
    fetchJson<PagedResult<ProductListItem>>('/api/catalog/products?pageSize=6&sortBy=price_asc'),
    fetchJson<PagedResult<ProductListItem>>(`/api/catalog/products?${catalogQuery()}`),
    fetchJson<unknown[]>('/api/services'),
  ]);
  const shared: Seed = [
    [['categories'], categories],
    [['brands'], brands],
  ];

  const routes: PrerenderedRoute[] = [
    {
      path: '/',
      head: headHtml(homeSeo()),
      body: renderPage('/', [...shared, [['home-products'], homeProducts]], bodyHtml('Ugradnja i servis klime u Novom Sadu', [
        'Montaža split sistema, pranje, dopuna freona i prodaja klima uređaja. Dolazimo na vašu adresu u Novom Sadu.',
      ])),
    },
    {
      path: '/katalog',
      head: headHtml(catalogSeo()),
      body: renderPage('/katalog', [...shared, [['products', catalogQuery()], catalogProducts]], bodyHtml('Katalog klima uređaja', [
        'Split, inverter i mobilne klime za Novi Sad. Filtrirajte po brendu, snazi i energetskoj klasi.',
      ])),
    },
    {
      path: '/ugradnja-klime-novi-sad',
      head: headHtml(ugradnjaSeo()),
      body: renderPage('/ugradnja-klime-novi-sad', shared, bodyHtml('Ugradnja klime Novi Sad', [
        'Profesionalna montaža split i inverter klima uređaja u Novom Sadu. Dogovoreni termin i čist završetak.',
      ])),
    },
    {
      path: '/servis-klime-novi-sad',
      head: headHtml(servisSeo()),
      body: renderPage('/servis-klime-novi-sad', shared, bodyHtml('Servis klime Novi Sad', [
        'Pranje, dijagnostika, dopuna freona i popravka klima uređaja na vašoj adresi u Novom Sadu.',
      ])),
    },
    {
      path: '/pranje-klime-novi-sad',
      head: headHtml(pranjeSeo()),
      body: renderPage('/pranje-klime-novi-sad', shared, bodyHtml('Pranje klime Novi Sad', [
        'Dubinsko pranje unutrašnje i spoljne jedinice na vašoj adresi u Novom Sadu.',
      ])),
    },
    {
      path: '/zakazivanje',
      head: headHtml(bookingSeo()),
      body: renderPage('/zakazivanje', [...shared, [['field-services'], fieldServices]], bodyHtml('Zakazivanje servisa i montaže klime Novi Sad', [
        'Zakažite pranje, servis ili ugradnju klime u Novom Sadu. Termin se privremeno rezerviše, a potvrda ide pozivom.',
      ])),
    },
    {
      path: '/privatnost',
      head: headHtml(privacySeo()),
      body: renderPage('/privatnost', shared, bodyHtml('Politika privatnosti', [
        'Obrada ličnih podataka kupaca pri prodaji i montaži klima uređaja.',
      ])),
    },
  ];

  for (const category of categories ?? []) {
    const path = `/kategorija/${category.slug}`;
    const products = await fetchJson<PagedResult<ProductListItem>>(`/api/catalog/products?${catalogQuery(category.slug)}`);
    routes.push({
      path,
      head: headHtml(catalogSeo(category)),
      body: renderPage(path, [...shared, [['products', catalogQuery(category.slug)], products]], bodyHtml(category.name, [category.description ?? ''])),
    });
  }

  const products = await fetchJson<{ items: ProductListItem[] }>('/api/catalog/products?page=1&pageSize=500');
  for (const product of products?.items ?? []) {
    const path = `/proizvod/${product.slug}`;
    const detail = await fetchJson<ProductDetail>(`/api/catalog/products/${product.slug}`);
    const fallback = bodyHtml(product.name, [
      detail?.shortDescription || detail?.description || `${product.name} - klima uređaj ${product.brandName}.`,
      `Cena: ${formatRsd(product.price)}`,
    ]);
    routes.push({
      path,
      // Without the detail record the page component would render "not found", so keep the plain fallback.
      head: headHtml(productSeo(detail ?? { ...product, categorySlug: '' })),
      body: detail ? renderPage(path, [...shared, [['product', product.slug], detail]], fallback) : fallback,
    });
  }

  return routes;
}
