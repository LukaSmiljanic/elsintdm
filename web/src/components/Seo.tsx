import { useEffect } from 'react';
import { applySeo, homeSeo, type SeoInput } from '../seo';

type Props = SeoInput;

/** Sets document title, meta, Open Graph, canonical and optional JSON-LD. */
export function Seo(props: Props) {
  useEffect(() => {
    applySeo(props);
  }, [
    props.title,
    props.description,
    props.path,
    props.image,
    props.type,
    props.noindex,
    JSON.stringify(props.jsonLd),
  ]);

  return null;
}

export function HomeSeo() {
  return <Seo {...homeSeo()} />;
}
