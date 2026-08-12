# ElsInt

Webshop za prodaju klima uređaja (B2C, Srbija) — ASP.NET Core Clean Architecture + React/TypeScript.

## Struktura

```
ElsInt/
├── src/
│   ├── ElsInt.Api
│   ├── ElsInt.Application
│   ├── ElsInt.Domain
│   └── ElsInt.Infrastructure
├── tests/
└── web/          # React storefront + admin
```

## Pokretanje backend-a

1. SQL Server LocalDB (ili promeni connection string u `appsettings.json`)
2. ```bash
   dotnet restore
   dotnet run --project src/ElsInt.Api
   ```
3. API: http://localhost:5080 (Swagger u Development)
4. Seed admin: `admin@elsint.rs` / `Admin123!`

## Pokretanje storefront-a

```bash
cd web
npm install
npm run dev
```

UI: http://localhost:5173 (proxy ka API-ju)

## Testovi

```bash
dotnet test
```

## MVP funkcionalnosti

- Katalog sa filterima (BTU/kW, energetska klasa, inverter, WiFi, buka, površina, brend, cena)
- Korpa + guest checkout (dostava / dostava+montaža)
- Plaćanje: CorvusPay, pouzeće, virman
- Admin: proizvodi, zalihe po magacinu, porudžbine/statusi
- SEO: slugovi, meta, sitemap.xml, robots.txt
- GDPR: cookie consent, privacy page, ConsentRecord
