import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { collectRoutes } from '../.prerender/collect.js';

const dist = new URL('../dist/', import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1');
const template = await readFile(join(dist, 'index.html'), 'utf8');

if (!template.includes('<!--seo-->') || !template.includes('<!--app-->')) {
  console.error('[prerender] index.html is missing the <!--seo--> or <!--app--> markers.');
  process.exit(1);
}

const routes = await collectRoutes();

for (const route of routes) {
  // Function replacers: a plain string would let "$$", "$&" and friends corrupt the content.
  const html = template
    .replace(/<!--seo-->[\s\S]*?<!--\/seo-->/, () => `<!--seo-->\n    ${route.head}\n    <!--/seo-->`)
    .replace('<!--app-->', () => route.body);

  // "/" is dist/index.html, everything else becomes dist/<path>.html. Netlify serves that at
  // /<path> with a 200; a dist/<path>/index.html would instead 301 to /<path>/, which
  // contradicts the canonical URL and the sitemap (both without the trailing slash).
  const target = route.path === '/'
    ? join(dist, 'index.html')
    : join(dist, `${route.path.replace(/^\//, '')}.html`);

  await mkdir(dirname(target), { recursive: true });
  await writeFile(target, html, 'utf8');
}

console.log(`[prerender] wrote ${routes.length} page(s)`);
