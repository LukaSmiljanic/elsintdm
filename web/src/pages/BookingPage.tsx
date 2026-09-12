import { FormEvent, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { api, formatRsd } from '../api';
import { Seo } from '../components/Seo';
import { bookingSeo } from '../seo';
import { isValidEmail, isValidRsMobile as isValidRsPhone } from '../validation';

type FieldService = {
  id: string;
  name: string;
  slug: string;
  description?: string;
  durationMinutes: number;
  price: number;
};

type Slot = {
  startUtc: string;
  endUtc: string;
  startLocal: string;
  endLocal: string;
};

type BookingResult = {
  id: string;
  serviceName: string;
  startLocal: string;
  endLocal: string;
  holdExpiresAtUtc?: string;
};

type FieldKey = 'service' | 'slot' | 'customerName' | 'customerPhone' | 'customerEmail' | 'address';
type FieldErrors = Partial<Record<FieldKey, string>>;

const DAY_NAMES = ['Ned', 'Pon', 'Uto', 'Sre', 'Čet', 'Pet', 'Sub'];
const MONTHS = ['jan', 'feb', 'mar', 'apr', 'maj', 'jun', 'jul', 'avg', 'sep', 'okt', 'nov', 'dec'];

function parseLocal(startLocal: string) {
  const [datePart, timePart] = startLocal.split(' ');
  const [y, m, d] = datePart.split('-').map(Number);
  const [hh, mm] = timePart.split(':').map(Number);
  return { y, m, d, hh, mm, dateKey: datePart, timeLabel: `${String(hh).padStart(2, '0')}:${String(mm).padStart(2, '0')}` };
}

function formatDayHeading(dateKey: string) {
  const [y, m, d] = dateKey.split('-').map(Number);
  const dt = new Date(y, m - 1, d);
  const today = new Date();
  const tomorrow = new Date();
  tomorrow.setDate(today.getDate() + 1);
  const sameDay = (a: Date, b: Date) =>
    a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();

  let prefix = DAY_NAMES[dt.getDay()];
  if (sameDay(dt, today)) prefix = 'Danas';
  else if (sameDay(dt, tomorrow)) prefix = 'Sutra';

  return { prefix, dayNum: d, month: MONTHS[m - 1], full: `${prefix}, ${d}. ${MONTHS[m - 1]}` };
}

export function BookingPage() {
  const [serviceId, setServiceId] = useState('');
  const [selectedDay, setSelectedDay] = useState('');
  const [slotUtc, setSlotUtc] = useState('');
  const [done, setDone] = useState<BookingResult | null>(null);
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [values, setValues] = useState({
    customerName: '',
    customerPhone: '',
    customerEmail: '',
    address: '',
    notes: '',
  });

  const services = useQuery({
    queryKey: ['field-services'],
    queryFn: () => api<FieldService[]>('/api/services'),
  });

  const slots = useQuery({
    queryKey: ['service-slots', serviceId],
    queryFn: () => api<Slot[]>(`/api/services/${serviceId}/slots?days=14`),
    enabled: !!serviceId,
  });

  const book = useMutation({
    mutationFn: (body: Record<string, unknown>) =>
      api<BookingResult>('/api/services/bookings', { method: 'POST', body: JSON.stringify(body) }),
    onSuccess: (data) => {
      setDone(data);
      setError('');
      setFieldErrors({});
    },
    onError: (err: Error) => setError(err.message),
  });

  const selectedService = services.data?.find((s) => s.id === serviceId);

  const days = useMemo(() => {
    const map = new Map<string, Slot[]>();
    for (const slot of slots.data ?? []) {
      const parsed = parseLocal(slot.startLocal);
      if (!map.has(parsed.dateKey)) map.set(parsed.dateKey, []);
      map.get(parsed.dateKey)!.push(slot);
    }
    return [...map.entries()].map(([dateKey, daySlots]) => ({
      dateKey,
      heading: formatDayHeading(dateKey),
      slots: daySlots,
      count: daySlots.length,
    }));
  }, [slots.data]);

  const activeDay = selectedDay && days.some((d) => d.dateKey === selectedDay)
    ? selectedDay
    : days[0]?.dateKey ?? '';

  const daySlots = days.find((d) => d.dateKey === activeDay)?.slots ?? [];
  const selectedSlot = (slots.data ?? []).find((s) => s.startUtc === slotUtc);

  const clearFieldError = (key: FieldKey) =>
    setFieldErrors((prev) => {
      if (!prev[key]) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });

  const pickService = (id: string) => {
    setServiceId(id);
    setSelectedDay('');
    setSlotUtc('');
    clearFieldError('service');
    clearFieldError('slot');
  };

  const setValue = (key: keyof typeof values, value: string) => {
    setValues((prev) => ({ ...prev, [key]: value }));
    if (key in fieldErrors) clearFieldError(key as FieldKey);
  };

  const validate = (): FieldErrors => {
    const next: FieldErrors = {};
    if (!serviceId) next.service = 'Izaberite uslugu.';
    if (!slotUtc) next.slot = 'Izaberite dan i sat termina.';

    const name = values.customerName.trim();
    if (!name) next.customerName = 'Unesite ime i prezime.';
    else if (name.length < 2) next.customerName = 'Ime je prekratko.';

    const phone = values.customerPhone.trim();
    if (!phone) next.customerPhone = 'Unesite broj telefona.';
    else if (!isValidRsPhone(phone))
      next.customerPhone = 'Unesite ispravan broj (npr. 067 xxx xxxx ili +38167…).';

    const email = values.customerEmail.trim();
    if (email && !isValidEmail(email)) next.customerEmail = 'Unesite ispravan email.';

    const address = values.address.trim();
    if (!address) next.address = 'Unesite adresu za dolazak.';
    else if (address.length < 5) next.address = 'Adresa je prekratka.';

    return next;
  };

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    const next = validate();
    setFieldErrors(next);
    if (Object.keys(next).length > 0) {
      setError('Popunite obeležena polja da biste poslali zahtev.');
      const first = document.querySelector('.field-error, .booking-step.has-error');
      first?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      return;
    }

    book.mutate({
      serviceId,
      startUtc: slotUtc,
      customerName: values.customerName.trim(),
      customerPhone: values.customerPhone.trim(),
      customerEmail: values.customerEmail.trim() || null,
      address: values.address.trim(),
      notes: values.notes.trim() || null,
    });
  };

  if (done) {
    return (
      <div className="shell page-pad">
        <Seo title="Zahtev primljen" path="/zakazivanje" noindex />
        <div className="panel stack" style={{ maxWidth: 640 }}>
          <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Zahtev primljen</h1>
          <p>
            Termin za <strong>{done.serviceName}</strong> ({done.startLocal}) je <strong>privremeno rezervisan</strong>.
            Kontaktiraćemo vas telefonom radi potvrde.
          </p>
          <p className="muted">
            Hold traje oko 4 sata. Ako se ne javimo / ne potvrdimo, slot se automatski oslobađa.
          </p>
          <div className="row">
            <a className="btn" href="tel:+381677627904">Pozovite nas</a>
            <Link className="btn secondary on-light" to="/">Početna</Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="shell page-pad">
      <Seo {...bookingSeo()} />
      <div className="booking-layout">
        <div className="panel stack booking-main">
          <div>
            <h1 style={{ fontFamily: 'var(--display)', margin: 0 }}>Zakazivanje servisa i montaže — Novi Sad</h1>
            <p className="muted" style={{ margin: '0.4rem 0 0' }}>
              Zakažite pranje, servis ili ugradnju klime u Novom Sadu. Izaberite dan i sat;
              termin se privremeno rezerviše, potvrda ide pozivom.
            </p>
          </div>

          <section className={`booking-step${fieldErrors.service ? ' has-error' : ''}`}>
            <div className="booking-step-label">1. Usluga</div>
            <div className="service-pick">
              {services.data?.map((s) => {
                const active = serviceId === s.id;
                return (
                  <button
                    key={s.id}
                    type="button"
                    className={`service-card${active ? ' active' : ''}`}
                    onClick={() => pickService(s.id)}
                  >
                    <strong>{s.name}</strong>
                    <span>{s.durationMinutes} min</span>
                    <span className="service-card-price">
                      {s.price > 0 ? formatRsd(s.price) : 'po dogovoru'}
                    </span>
                    {s.description && <em>{s.description}</em>}
                  </button>
                );
              })}
            </div>
            {fieldErrors.service && <p className="field-error">{fieldErrors.service}</p>}
          </section>

          {serviceId && (
            <section className={`booking-step${fieldErrors.slot ? ' has-error' : ''}`}>
              <div className="booking-step-label">2. Dan</div>
              {slots.isLoading && <p className="muted">Učitavanje kalendara…</p>}
              {!slots.isLoading && days.length === 0 && (
                <p className="muted">
                  Nema slobodnih termina u narednih 14 dana za ovu uslugu.
                  Ako je kalendar stvarno prazan, u adminu proverite <strong>Raspored</strong>
                  — bez pravila za dane (uto–sub) sajt ne nudi termine.
                </p>
              )}
              {days.length > 0 && (
                <div className="day-strip" role="listbox" aria-label="Dani">
                  {days.map((day) => {
                    const active = activeDay === day.dateKey;
                    return (
                      <button
                        key={day.dateKey}
                        type="button"
                        role="option"
                        aria-selected={active}
                        className={`day-pill${active ? ' active' : ''}`}
                        onClick={() => {
                          setSelectedDay(day.dateKey);
                          setSlotUtc('');
                          clearFieldError('slot');
                        }}
                      >
                        <span className="day-pill-name">{day.heading.prefix}</span>
                        <span className="day-pill-num">{day.heading.dayNum}</span>
                        <span className="day-pill-month">{day.heading.month}</span>
                        <span className="day-pill-count">{day.count} termina</span>
                      </button>
                    );
                  })}
                </div>
              )}
            </section>
          )}

          {serviceId && activeDay && daySlots.length > 0 && (
            <section className={`booking-step${fieldErrors.slot ? ' has-error' : ''}`}>
              <div className="booking-step-label">
                3. Sat · {formatDayHeading(activeDay).full}
              </div>
              <div className="time-grid">
                {daySlots.map((slot) => {
                  const active = slotUtc === slot.startUtc;
                  const time = parseLocal(slot.startLocal).timeLabel;
                  const end = parseLocal(slot.endLocal).timeLabel;
                  return (
                    <button
                      key={slot.startUtc}
                      type="button"
                      className={`time-slot${active ? ' active' : ''}`}
                      onClick={() => {
                        setSlotUtc(slot.startUtc);
                        clearFieldError('slot');
                      }}
                    >
                      <span className="time-slot-start">{time}</span>
                      <span className="time-slot-end">do {end}</span>
                    </button>
                  );
                })}
              </div>
              {fieldErrors.slot && <p className="field-error">{fieldErrors.slot}</p>}
            </section>
          )}

          {fieldErrors.slot && serviceId && daySlots.length === 0 && (
            <p className="field-error">{fieldErrors.slot}</p>
          )}

          <form className="stack booking-form" onSubmit={onSubmit} noValidate>
            <div className="booking-step-label">4. Vaši podaci</div>
            {selectedSlot && selectedService && (
              <div className="booking-summary">
                <strong>{selectedService.name}</strong>
                <span>{selectedSlot.startLocal.replace(' ', ' · ')} – {selectedSlot.endLocal.slice(11)}</span>
              </div>
            )}
            <div className="booking-fields">
              <label className={fieldErrors.customerName ? 'invalid' : undefined}>
                Ime i prezime
                <input
                  name="customerName"
                  autoComplete="name"
                  value={values.customerName}
                  onChange={(e) => setValue('customerName', e.target.value)}
                />
                {fieldErrors.customerName && <span className="field-error">{fieldErrors.customerName}</span>}
              </label>
              <label className={fieldErrors.customerPhone ? 'invalid' : undefined}>
                Telefon
                <input
                  name="customerPhone"
                  type="tel"
                  autoComplete="tel"
                  placeholder="067 xxx xxxx"
                  value={values.customerPhone}
                  onChange={(e) => setValue('customerPhone', e.target.value)}
                />
                {fieldErrors.customerPhone && <span className="field-error">{fieldErrors.customerPhone}</span>}
              </label>
              <label className={fieldErrors.customerEmail ? 'invalid' : undefined}>
                Email (opciono — potvrda termina)
                <input
                  name="customerEmail"
                  type="email"
                  autoComplete="email"
                  value={values.customerEmail}
                  onChange={(e) => setValue('customerEmail', e.target.value)}
                  placeholder="npr. ime@email.com"
                />
                {fieldErrors.customerEmail && <span className="field-error">{fieldErrors.customerEmail}</span>}
              </label>
              <label className={fieldErrors.address ? 'invalid' : undefined}>
                Adresa
                <input
                  name="address"
                  autoComplete="street-address"
                  placeholder="Ulica i broj, grad"
                  value={values.address}
                  onChange={(e) => setValue('address', e.target.value)}
                />
                {fieldErrors.address && <span className="field-error">{fieldErrors.address}</span>}
              </label>
              <label className="booking-notes">
                Napomena
                <textarea
                  name="notes"
                  rows={3}
                  value={values.notes}
                  onChange={(e) => setValue('notes', e.target.value)}
                />
              </label>
            </div>
            {error && <p className="error booking-form-error">{error}</p>}
            <button className="btn" type="submit" disabled={book.isPending}>
              {book.isPending ? 'Slanje…' : 'Pošalji zahtev'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
