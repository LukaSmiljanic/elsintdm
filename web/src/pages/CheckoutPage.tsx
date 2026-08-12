import { FormEvent, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { api, formatRsd, type CartItem } from '../api';
import { useCart } from '../cart';
import { Seo } from '../components/Seo';

type Quote = {
  lines: { productId: string; name: string; unitPrice: number; quantity: number; lineTotal: number }[];
  subtotal: number;
  vatAmount: number;
  shippingFee: number;
  installationFee: number;
  total: number;
};

type CheckoutResult = {
  orderId: string;
  orderNumber: string;
  total: number;
  paymentMethod: number;
};

type CustomerType = 'individual' | 'company';

export function CheckoutPage() {
  const { items, clear } = useCart();
  const navigate = useNavigate();
  const [customerType, setCustomerType] = useState<CustomerType>('individual');
  const [deliveryOption, setDeliveryOption] = useState(0);
  const [paymentMethod, setPaymentMethod] = useState(1);
  const [error, setError] = useState('');

  const quoteBody = useMemo(() => ({
    items: items.map((i) => ({ productId: i.productId, quantity: i.quantity })),
    deliveryOption,
  }), [items, deliveryOption]);

  const quote = useQuery({
    queryKey: ['quote', quoteBody],
    queryFn: () => api<Quote>('/api/cart/quote', { method: 'POST', body: JSON.stringify(quoteBody) }),
    enabled: items.length > 0,
  });

  const checkout = useMutation({
    mutationFn: (payload: Record<string, unknown>) =>
      api<CheckoutResult>('/api/checkout', { method: 'POST', body: JSON.stringify(payload) }),
  });

  if (items.length === 0) {
    return (
      <div className="shell page-pad checkout-empty">
        <Seo title="Porudžbina" path="/checkout" noindex />
        <h1>Porudžbina</h1>
        <p className="muted">Korpa je prazna.</p>
        <Link className="btn" to="/katalog">Nazad na katalog</Link>
      </div>
    );
  }

  const onSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    const fd = new FormData(e.currentTarget);
    const isCompany = customerType === 'company';
    const companyName = isCompany ? String(fd.get('companyName') || '').trim() : null;
    const taxId = isCompany ? String(fd.get('taxId') || '').trim() : null;

    if (isCompany && (!companyName || !taxId)) {
      setError('Za pravna lica unesite naziv firme i PIB.');
      return;
    }

    try {
      const result = await checkout.mutateAsync({
        email: fd.get('email'),
        firstName: fd.get('firstName'),
        lastName: fd.get('lastName'),
        phone: fd.get('phone'),
        companyName,
        taxId,
        addressLine1: fd.get('addressLine1'),
        addressLine2: fd.get('addressLine2') || null,
        city: fd.get('city'),
        postalCode: fd.get('postalCode'),
        deliveryOption,
        paymentMethod,
        preferredInstallationDate: fd.get('preferredInstallationDate') || null,
        installationNotes: fd.get('installationNotes') || null,
        notes: fd.get('notes') || null,
        marketingConsent: fd.get('marketingConsent') === 'on',
        privacyConsent: fd.get('privacyConsent') === 'on',
        items: items.map((i: CartItem) => ({ productId: i.productId, quantity: i.quantity })),
      });

      clear();
      navigate(`/hvala?order=${encodeURIComponent(result.orderNumber)}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Greška pri porudžbini');
    }
  };

  return (
    <div className="shell page-pad">
      <Seo title="Porudžbina" path="/checkout" noindex />
      <div className="checkout-head">
        <h1>Porudžbina</h1>
        <p className="muted">Popunite podatke - potvrdićemo porudžbinu telefonom ili emailom.</p>
      </div>

      <div className="checkout">
        <form className="checkout-form" onSubmit={onSubmit}>
          <section className="checkout-section">
            <h2>1. Tip kupca</h2>
            <div className="choice-row">
              <button
                type="button"
                className={`choice ${customerType === 'individual' ? 'active' : ''}`}
                onClick={() => setCustomerType('individual')}
              >
                <strong>Fizičko lice</strong>
                <span>Privatna kupovina</span>
              </button>
              <button
                type="button"
                className={`choice ${customerType === 'company' ? 'active' : ''}`}
                onClick={() => setCustomerType('company')}
              >
                <strong>Pravno lice</strong>
                <span>Firma · PIB · virman</span>
              </button>
            </div>
          </section>

          <section className="checkout-section">
            <h2>2. Kontakt</h2>
            <div className="field-grid two">
              <label className="field">
                <span>Ime</span>
                <input name="firstName" required autoComplete="given-name" />
              </label>
              <label className="field">
                <span>Prezime</span>
                <input name="lastName" required autoComplete="family-name" />
              </label>
              <label className="field">
                <span>Email</span>
                <input name="email" type="email" required autoComplete="email" />
              </label>
              <label className="field">
                <span>Telefon</span>
                <input name="phone" type="tel" required autoComplete="tel" placeholder="06x xxx xxxx" />
              </label>
            </div>

            {customerType === 'company' && (
              <div className="field-grid two company-fields">
                <label className="field">
                  <span>Naziv firme</span>
                  <input name="companyName" required autoComplete="organization" />
                </label>
                <label className="field">
                  <span>PIB</span>
                  <input name="taxId" required inputMode="numeric" placeholder="9 cifara" />
                </label>
              </div>
            )}
          </section>

          <section className="checkout-section">
            <h2>3. Adresa dostave</h2>
            <div className="field-grid">
              <label className="field">
                <span>Ulica i broj</span>
                <input name="addressLine1" required autoComplete="address-line1" />
              </label>
              <label className="field">
                <span>Sprat / stan / napomena adrese <em>(opciono)</em></span>
                <input name="addressLine2" autoComplete="address-line2" />
              </label>
              <div className="field-grid two">
                <label className="field">
                  <span>Grad</span>
                  <input name="city" required autoComplete="address-level2" />
                </label>
                <label className="field">
                  <span>Poštanski broj</span>
                  <input name="postalCode" required autoComplete="postal-code" />
                </label>
              </div>
            </div>
          </section>

          <section className="checkout-section">
            <h2>4. Dostava i montaža</h2>
            <div className="choice-stack">
              <label className={`choice-radio ${deliveryOption === 0 ? 'active' : ''}`}>
                <input type="radio" name="delivery" checked={deliveryOption === 0} onChange={() => setDeliveryOption(0)} />
                <span>
                  <strong>Samo dostava</strong>
                  <em>Dostava na adresu bez montaže</em>
                </span>
              </label>
              <label className={`choice-radio ${deliveryOption === 1 ? 'active' : ''}`}>
                <input type="radio" name="delivery" checked={deliveryOption === 1} onChange={() => setDeliveryOption(1)} />
                <span>
                  <strong>Dostava + montaža</strong>
                  <em>Profesionalna ugradnja na dogovoren termin</em>
                </span>
              </label>
            </div>
            {deliveryOption === 1 && (
              <div className="field-grid two install-fields">
                <label className="field">
                  <span>Željeni datum montaže</span>
                  <input name="preferredInstallationDate" type="date" />
                </label>
                <label className="field">
                  <span>Napomena za montažu</span>
                  <textarea name="installationNotes" rows={2} placeholder="npr. sprat, dužina cevi…" />
                </label>
              </div>
            )}
          </section>

          <section className="checkout-section">
            <h2>5. Plaćanje</h2>
            <div className="choice-stack">
              <label className={`choice-radio ${paymentMethod === 1 ? 'active' : ''}`}>
                <input type="radio" name="payment" checked={paymentMethod === 1} onChange={() => setPaymentMethod(1)} />
                <span>
                  <strong>Pouzeće</strong>
                  <em>Plaćanje pri preuzimanju</em>
                </span>
              </label>
              <label className={`choice-radio ${paymentMethod === 2 ? 'active' : ''}`}>
                <input type="radio" name="payment" checked={paymentMethod === 2} onChange={() => setPaymentMethod(2)} />
                <span>
                  <strong>Virman</strong>
                  <em>{customerType === 'company' ? 'Preporučeno za firme' : 'Uplata na račun pre isporuke'}</em>
                </span>
              </label>
            </div>
          </section>

          <section className="checkout-section">
            <h2>6. Napomena</h2>
            <label className="field">
              <span>Dodatna napomena <em>(opciono)</em></span>
              <textarea name="notes" rows={3} placeholder="Šta još treba da znamo…" />
            </label>
            <div className="consent-list">
              <label className="consent">
                <input name="privacyConsent" type="checkbox" required />
                <span>Slažem se sa <Link to="/privatnost">politikom privatnosti</Link></span>
              </label>
              <label className="consent">
                <input name="marketingConsent" type="checkbox" />
                <span>Želim da primam ponude i akcije emailom</span>
              </label>
            </div>
          </section>

          {error && <p className="error checkout-error">{error}</p>}

          <button className="btn block checkout-submit" type="submit" disabled={checkout.isPending}>
            {checkout.isPending ? 'Slanje…' : 'Potvrdi porudžbinu'}
          </button>
        </form>

        <aside className="checkout-summary">
          <h2>Pregled porudžbine</h2>
          <ul className="summary-lines">
            {quote.data?.lines.map((l) => (
              <li key={l.productId}>
                <span className="summary-name">{l.name} <em>× {l.quantity}</em></span>
                <span className="summary-price">{formatRsd(l.lineTotal)}</span>
              </li>
            ))}
          </ul>
          <div className="summary-totals">
            <div><span>Dostava</span><span>{formatRsd(quote.data?.shippingFee ?? 0)}</span></div>
            <div><span>Montaža</span><span>{formatRsd(quote.data?.installationFee ?? 0)}</span></div>
            <div className="summary-total"><span>Ukupno</span><strong>{formatRsd(quote.data?.total ?? 0)}</strong></div>
          </div>
          <p className="summary-note muted">Cene su sa PDV-om. Nakon porudžbine javljamo se radi potvrde.</p>
        </aside>
      </div>
    </div>
  );
}
