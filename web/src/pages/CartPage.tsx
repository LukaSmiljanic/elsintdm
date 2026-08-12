import { Link } from 'react-router-dom';
import { formatRsd } from '../api';
import { useCart } from '../cart';
import { Seo } from '../components/Seo';

export function CartPage() {
  const { items, setQty, remove, count } = useCart();
  const subtotal = items.reduce((s, i) => s + i.price * i.quantity, 0);

  if (count === 0) {
    return (
      <div className="shell page-pad stack">
        <Seo title="Korpa" path="/korpa" noindex />
        <h1 style={{ fontFamily: 'var(--display)', textTransform: 'uppercase', color: 'var(--navy)' }}>Korpa je prazna</h1>
        <Link className="btn" to="/katalog">Nastavi kupovinu</Link>
      </div>
    );
  }

  return (
    <div className="shell page-pad stack">
      <Seo title="Korpa" path="/korpa" noindex />
      <h1 style={{ fontFamily: 'var(--display)', margin: 0, textTransform: 'uppercase', color: 'var(--navy)' }}>Korpa</h1>
      {items.map((item) => (
        <div key={item.productId} className="panel row space">
          <div>
            <Link to={`/proizvod/${item.slug}`}><strong>{item.name}</strong></Link>
            <div className="meta">{formatRsd(item.price)}</div>
          </div>
          <div className="row">
            <input
              type="number"
              min={1}
              value={item.quantity}
              style={{ width: 72 }}
              onChange={(e) => setQty(item.productId, Number(e.target.value))}
            />
            <button className="btn secondary on-light" onClick={() => remove(item.productId)}>Ukloni</button>
          </div>
        </div>
      ))}
      <div className="panel row space">
        <strong>Međuzbir</strong>
        <strong className="price" style={{ margin: 0 }}>{formatRsd(subtotal)}</strong>
      </div>
      <Link className="btn" to="/checkout">Nastavi na plaćanje</Link>
    </div>
  );
}
