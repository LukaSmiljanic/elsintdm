import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { CartProvider } from './cart';
import { StoreLayout } from './components/StoreLayout';
import { HomePage } from './pages/HomePage';
import { CatalogPage } from './pages/CatalogPage';
import { ProductPage } from './pages/ProductPage';
import { CartPage } from './pages/CartPage';
import { CheckoutPage } from './pages/CheckoutPage';
import { PrivacyPage, ThankYouPage } from './pages/MiscPages';
import { BookingPage } from './pages/BookingPage';
import { ServisKlimePage, UgradnjaKlimePage } from './pages/ServicePages';
import { AdminApp } from './pages/AdminApp';
import './styles.css';

const queryClient = new QueryClient();

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <CartProvider>
        <BrowserRouter>
          <Routes>
            <Route element={<StoreLayout />}>
              <Route index element={<HomePage />} />
              <Route path="katalog" element={<CatalogPage />} />
              <Route path="kategorija/:slug" element={<CatalogPage />} />
              <Route path="proizvod/:slug" element={<ProductPage />} />
              <Route path="korpa" element={<CartPage />} />
              <Route path="checkout" element={<CheckoutPage />} />
              <Route path="zakazivanje" element={<BookingPage />} />
              <Route path="ugradnja-klime-novi-sad" element={<UgradnjaKlimePage />} />
              <Route path="servis-klime-novi-sad" element={<ServisKlimePage />} />
              <Route path="hvala" element={<ThankYouPage />} />
              <Route path="privatnost" element={<PrivacyPage />} />
              <Route path="admin/*" element={<AdminApp />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </CartProvider>
    </QueryClientProvider>
  );
}
