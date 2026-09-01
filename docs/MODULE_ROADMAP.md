# Module Roadmap — Transport Management System

Living document. Tracks planned modules, build order, and the per-module steps.
Update the checkboxes and the "Status" line as work lands.

- **Stack:** Blazor Web (`TransportationSystemWeb`) · .NET API (`TransportationSystemApi`) · shared DTOs (`TransportationSystemShared`) · SQL Server (`FleetMasterDb`).
- **Migrations:** plain numbered `.sql` files in `TransportationSystemApi/TransportationSystemApi/Database/`, run manually with `sqlcmd`. No EF migrations, no auto-apply. Latest applied: `005_MaintenanceWorkshop.sql`.
- **Dev DB:** `FleetMasterDb` on `localhost`, `sa` / `123qwe`.

---

## Done (6 modules)

| Module | Key entities | Notes |
|---|---|---|
| Fleet Master | `Vehicle`, `VehicleDocument`, `AlertRule`, `Tyre`, `TyreReplacementHistory`, `MaintenanceRecord` | Original module; tabbed vehicle editor. |
| Driver Management | `Driver`, `DriverDocument`, `DriverVehicleAssignment` | Standalone records, not linked to `Users`. |
| Trip / Booking | `Trip` | Requires `VehicleId` + `DriverId`. Has `Revenue`, free-text `Origin`/`Destination`. |
| Maintenance & Workshop | `WorkOrder`, `WorkOrderItem` | Work order + parts/materials line items. `TotalCost = LabourCost + Σ items`. |
| Reports & Analytics | none (read-only) | `GET api/reports/summary` → one aggregated DTO. |
| Settings | `CompanyProfile`, `User`, `LoginHistory` | Company profile, users, login history. |

---

## Shared build recipe (follow for every new module)

Every module so far follows this exact sequence. Mirror it, do not reinvent.

1. **DB migration** — `Database/00N_<ModuleName>.sql`.
   - `USE FleetMasterDb; GO`
   - Every statement guarded (`IF OBJECT_ID('dbo.X','U') IS NULL BEGIN ... END`) so it is safe to re-run.
   - `Id INT IDENTITY(1,1)` PK, `CreatedAt DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME())`, nullable `UpdatedAt`.
   - Unique index on the human code column (`XxxCode`).
   - FKs to parents with `ON DELETE CASCADE` where the child has no meaning without the parent.
   - Apply locally: `sqlcmd -S localhost -U sa -P 123qwe -i Database\00N_<ModuleName>.sql`
2. **EF model** — `Models/<Entity>.cs`. Plain POCO, `DateOnly` for calendar dates, `decimal?` for money, enums for status. Child collections as `List<TChild> = new()`.
3. **Register in `FleetDbContext`** — add `DbSet<T>` and any `OnModelCreating` config (unique index, decimal precision, relationships).
4. **DTOs** — `TransportationSystemShared/Dtos/<Module>Dtos.cs`. Namespace stays `TransportationSystemApi.Dtos` even though the file is in the Shared project. One list DTO, one detail DTO, one create/update DTO per entity.
5. **Mapper** — `Mapping/<Entity>Mapper.cs`. Entity→DTO and DTO→entity. Compute derived fields here (totals, counts, days-until-expiry).
6. **Controller(s)** — `Controllers/<Module>Controller.cs`, route `api/<module>`. Mirror `VehiclesController`: `GET` list (with filter query params + `search`), `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}`. Child collections get a nested controller (`api/<module>/{id}/items`) like `WorkOrderItemsController`.
7. **Extend `FleetApiClient`** (`TransportationSystemWeb/Services/FleetApiClient.cs`) — one method per endpoint. Do **not** create a new client class.
8. **Blazor pages** — `Components/Pages/<Module>/`:
   - `<Entity>List.razor` — table, filters, search, `?ParentId=` deep-link support, row → editor.
   - `<Entity>Editor.razor` — single-card form if no child collections; tabbed (`Details` + child tabs) if it has them.
9. **Sidebar nav** — `Components/Layout/MainLayout.razor`. Add a `nav-module` block with `nav-module-header` + `nav-sublink`(s). Match the existing active-module highlight pattern.
10. **Dashboard section** — `Components/Pages/Home.razor`. Add a stats block (counts + a short "upcoming / attention" list) and a quick-link button. Each section shows/hides independently.
11. **Verify end-to-end in browser** — create → list → edit → derived fields → delete (+ FK cascade). Confirm no console errors. Leave the dev DB clean (delete test rows).
12. **Commit** — one commit per module: `Add <Module> module (<one-line scope>)`. Update this file's checkboxes + the memory note.

---

## Build order

Rationale: turn the app from record-keeping into an operations-and-money system. Each tier unblocks the next.

### Tier 1 — core (do first)

- [x] **1. Customer / Client Master** — done. Master CRUD (`006`, commit `e757dc8`) + `Trip.CustomerId` link (`007`, commit `e91ae4e`).
- [x] **2. Fuel Management** — done. `dbo.FuelEntries` (`008`), mileage / cost-per-km derived at read time, dashboard section. Optional `FuelStation` master deferred (free-text `StationName` for now).
- [x] **3. Trip Expenses + per-trip P&L** — done. `dbo.TripExpenses` child of Trips (`009`), nested `api/trips/{id}/expenses` CRUD, P&L folded into trip detail DTO (revenue − fuel − expenses − driver-pay; driver-pay stays 0 until module 8), tabbed Trip editor (Details / Expenses / P&L). Not yet: profitability columns on the Trip **list** and Reports section (see cross-cutting).
- [x] **4. Billing & Invoicing (A/R)** — done. `dbo.Invoices` + `dbo.InvoiceLines` + `dbo.Payments` (`010`). Money & effective status computed at read time; `InvoiceLines.TripId` is a soft link (no FK). `POST api/invoices/from-trips`, `GET api/invoices/aging`, `GET api/invoices/billable-trips`. Tabbed editor (Details / Lines / Payments) + `/invoices/from-trips` picker + dashboard A/R section. **Tier 1 complete.**

### Tier 2 — strongly expected

- [ ] **5. Compliance & Document-Expiry Alert Engine** — mostly wiring up data already stored.
- [ ] **6. Tyre Management (promote to full module)**
- [ ] **7. Spare Parts Inventory / Stores**
- [ ] **8. Driver Payroll / Settlements & Advances**
- [ ] **9. GPS / Live Tracking integration**

### Tier 3 — differentiators / larger scope

- [ ] **10. Route & Rate-Contract Management**
- [ ] **11. Vendor / Procurement / Purchase Orders**
- [ ] **12. Consignment note / LR / Proof-of-Delivery on trips**
- [ ] **13. Mobile driver app**
- [ ] **14. Accident / Incident & Insurance-Claims tracking**
- [ ] **15. Audit trail (data-change log) + RBAC hardening**

---

## Module specs

### 1. Customer / Client Master

**Goal:** master list of billing customers; every trip can optionally belong to one.

- **Migration:** `006_CustomerMaster.sql` → `dbo.Customers`.
- **Entities:**
  - `Customer` — `CustomerCode` (unique), `Name`, `ContactPerson`, `Phone`, `Email`, `BillingAddress`, `TaxNumber` (GST/NTN), `CreditLimit` (decimal?), `PaymentTermsDays` (int?), `Status` (`Active`/`Inactive`), `Notes`.
  - `CustomerContact` (optional child) — extra contacts per customer.
- **Endpoints:** `api/customers` CRUD + `?search=` + `?status=`.
- **Trip change:** add nullable `CustomerId` FK to `Trip` (migration alters `dbo.Trips`, `ON DELETE SET NULL`). Trip editor gets a customer picker.
- **UI:** `Pages/Customers/CustomerList.razor` + `CustomerEditor.razor` (single card, or tabbed if `CustomerContact` is included).
- **Dashboard:** active customers count; top customers by trip count (last 30 days).
- **Depends on:** nothing.

### 2. Fuel Management

**Goal:** log every fuel fill, auto-compute mileage and cost-per-km, flag variance.

- **Migration:** `007_FuelManagement.sql` → `dbo.FuelEntries`, `dbo.FuelStations` (optional master).
- **Entities:**
  - `FuelEntry` — `VehicleId` FK, `DriverId` FK (nullable), `TripId` FK (nullable), `Date`, `OdometerReading` (decimal), `Litres` (decimal), `RatePerLitre` (decimal), `TotalCost` (decimal, computed = Litres × Rate, but store it), `FuelType` (reuse `FuelType` enum), `StationId` FK (nullable) or `StationName` (string), `PaymentMode` (`Cash`/`FuelCard`/`Credit`), `SlipNumber`, `IsTankFull` (bool), `Notes`.
  - `FuelStation` (optional) — `Name`, `Location`, `Vendor`.
- **Derived (in mapper / a service):**
  - `DistanceSinceLast` = this odometer − previous full-tank entry odometer for the same vehicle.
  - `Mileage` (km/L) = `DistanceSinceLast / Litres` (only meaningful full-tank to full-tank).
  - `CostPerKm` = `TotalCost / DistanceSinceLast`.
  - `VarianceFlag` — mileage deviates > X% from the vehicle's rolling average → highlight (possible theft / issue).
- **Endpoints:** `api/fuel-entries` CRUD, filters `vehicleId` / `driverId` / `tripId` / date range / `search`. `api/fuel-stations` CRUD.
- **UI:** `Pages/Fuel/FuelEntryList.razor` (filters + running mileage column + variance badge) + `FuelEntryEditor.razor` (single card). Optional `FuelStationList` / editor.
- **Fleet editor:** add a read-only "Fuel History" tab on the vehicle editor (last N entries, avg mileage, cost/km) — mirror how the "Operational Status" tab shows trip history.
- **Dashboard:** this-month fuel spend, fleet avg mileage, vehicles with a variance flag.
- **Depends on:** Vehicles (done), Drivers (done), Trips (done, for optional link).

### 3. Trip Expenses + per-trip P&L

**Goal:** capture all costs against a trip and show trip profitability.

- **Migration:** `008_TripExpenses.sql` → `dbo.TripExpenses`.
- **Entities:**
  - `TripExpense` — `TripId` FK (`ON DELETE CASCADE`), `Category` enum (`Toll`, `Parking`, `LoadingUnloading`, `DriverAllowance`, `EnRouteRepair`, `Fine`, `Weighbridge`, `Misc`), `Amount` (decimal), `Date`, `PaidBy` (`Driver`/`Company`), `ReceiptNumber`, `Notes`.
- **Trip P&L (mapper / service):** per trip →
  - Revenue = `Trip.Revenue`
  - Fuel = Σ `FuelEntry.TotalCost` where `TripId` matches
  - Expenses = Σ `TripExpense.Amount`
  - Driver pay = from Payroll module later (0 until #8 exists)
  - `NetProfit` = Revenue − Fuel − Expenses − DriverPay
- **Endpoints:** `api/trips/{id}/expenses` nested CRUD (mirror `WorkOrderItemsController`). Add `GET api/trips/{id}/pnl` or fold P&L fields into the trip detail DTO.
- **UI:** Trip editor becomes **tabbed** — `Details` + `Expenses` + `P&L` (read-only summary).
- **Dashboard / Reports:** most/least profitable trips; profit by customer; profit by vehicle.
- **Depends on:** Trips (done); Fuel (#2) for the fuel line; Payroll (#8) for the driver-pay line (wire later).

### 4. Billing & Invoicing (Accounts Receivable) — DONE (`010`)

_Built. `InvoiceLine.IsBilled`-style "billed?" column on the Trip **list** was deferred (belongs with the Reports/list-columns debt). `billable-trips` endpoint already excludes billed trips._


**Goal:** raise invoices against trips/customers, record payments, track outstanding.

- **Migration:** `009_Billing.sql` → `dbo.Invoices`, `dbo.InvoiceLines`, `dbo.Payments`.
- **Entities:**
  - `Invoice` — `InvoiceNumber` (unique), `CustomerId` FK, `InvoiceDate`, `DueDate`, `Status` (`Draft`/`Sent`/`PartiallyPaid`/`Paid`/`Cancelled`), `SubTotal`, `TaxPercent`, `TaxAmount`, `Total`, `AmountPaid`, `Balance`, `Notes`.
  - `InvoiceLine` — `InvoiceId` FK (`ON DELETE CASCADE`), `TripId` FK (nullable), `Description`, `Quantity`, `UnitPrice`, `LineTotal`.
  - `Payment` — `InvoiceId` FK, `Date`, `Amount`, `Mode` (`Cash`/`Bank`/`Cheque`/`Online`), `Reference`, `Notes`.
- **Rules (service):** `Invoice.Total = SubTotal + TaxAmount`; `AmountPaid = Σ Payments`; `Balance = Total − AmountPaid`; status auto-transitions on payment. "Create invoice from trips" helper: pick a customer + unbilled trips → prefill lines.
- **Endpoints:** `api/invoices` CRUD, `api/invoices/{id}/lines` nested, `api/invoices/{id}/payments` nested. `GET api/invoices/aging` → buckets (0-30 / 31-60 / 61-90 / 90+).
- **UI:** `Pages/Billing/InvoiceList.razor` (status + customer + overdue filters) + `InvoiceEditor.razor` (tabbed: `Details` / `Lines` / `Payments`). "Unbilled trips" picker.
- **Trip change:** add `IsBilled` computed/flag or derive from existence of an `InvoiceLine`.
- **Dashboard:** total outstanding, overdue amount, collected this month.
- **Depends on:** Customer Master (#1); Trips (done).

### 5. Compliance & Document-Expiry Alert Engine

**Goal:** one dashboard for every expiring document + scheduled email reminders.

- **Sources already in DB:** `Vehicle` — RC / fitness / permit / insurance / pollution / tax expiry dates. `Driver` — `LicenseExpiryDate`. `VehicleDocument` / `DriverDocument` — uploaded docs (add optional `ExpiryDate` if missing).
- **Migration:** `010_ComplianceAlerts.sql` → `dbo.AlertConfigs` (which document types to watch, lead-time days, recipient emails), `dbo.AlertLog` (what was sent, when — dedupe).
- **Service:** `ComplianceService` — scans all sources, produces a unified list `{ EntityType, EntityId, EntityName, DocumentType, ExpiryDate, DaysRemaining, Severity }`. Severity: expired / ≤7 / ≤30 / ≤60.
- **Notifications:** a `BackgroundService` (hosted service) runs daily, uses `AlertConfig` lead times, sends email via SMTP (add `SmtpOptions` to config), writes `AlertLog` to avoid re-sending.
- **Endpoints:** `GET api/compliance/expiries` (with severity + entityType filters), `api/compliance/config` CRUD.
- **UI:** `Pages/Compliance/ComplianceDashboard.razor` — grouped table, severity colour, click-through to the vehicle/driver editor. `Pages/Compliance/AlertSettings.razor`.
- **Dashboard:** "X documents expiring in 30 days" headline with drill-in.
- **Depends on:** Fleet + Drivers (done). Existing `AlertRule` model — decide whether to fold it in or leave it vehicle-scoped.

### 6. Tyre Management (promote to full module)

**Goal:** track each tyre as an asset across its life, cost-per-km, inventory.

- **Existing:** `Tyre`, `TyreReplacementHistory`, `VehicleTyresController` (currently nested under Fleet).
- **Migration:** `011_TyreManagement.sql` — extend `dbo.Tyres` (serial/brand/pattern, purchase date/cost, current position code e.g. `FL/FR/RL1`, status `InStock`/`Fitted`/`Retreaded`/`Scrapped`, current odometer-at-fit, total distance run), new `dbo.TyreEvents` (fit / remove / rotate / retread / inspect / scrap with odometer + cost + notes), `dbo.TyreStock` for unfitted tyres.
- **Derived:** `DistanceRun` = current vehicle odometer − odometer-at-fit + carried distance; `CostPerKm` = (purchase + retread costs) / DistanceRun.
- **Endpoints:** `api/tyres` CRUD, `api/tyres/{id}/events` nested, `api/tyres/stock`. Vehicle tyre-position map endpoint.
- **UI:** `Pages/Tyres/TyreList.razor` (status + vehicle + brand filters) + `TyreEditor.razor` (tabbed: `Details` / `Events`). A visual axle/position map on the vehicle editor.
- **Dashboard:** tyres due for rotation, worst cost-per-km, stock count.
- **Depends on:** Vehicles (done). Migrate existing tyre data forward.

### 7. Spare Parts Inventory / Stores

**Goal:** real stock for parts, linked to work-order consumption.

- **Migration:** `012_PartsInventory.sql` → `dbo.Parts` (part master: number, name, unit, reorder level, standard cost), `dbo.StockMovements` (receipt / issue / adjust with qty, unit cost, ref to WO or PO), `dbo.Suppliers` (if not added by #11).
- **WorkOrderItem change:** optional `PartId` FK; issuing a `WorkOrderItem` against a stocked part creates an `issue` `StockMovement` and decrements on-hand.
- **Derived:** on-hand qty = Σ movements; below-reorder flag; stock value.
- **Endpoints:** `api/parts` CRUD, `api/parts/{id}/movements`, `GET api/parts/low-stock`.
- **UI:** `Pages/Inventory/PartList.razor` + `PartEditor.razor` (tabbed: `Details` / `Stock Movements`). Goods-receipt screen.
- **Dashboard:** low-stock count, total stock value.
- **Depends on:** Maintenance module (done) for the WO link; Vendors (#11) optional.

### 8. Driver Payroll / Settlements & Advances

**Goal:** compute driver pay per trip / period, track advances (khata).

- **Migration:** `013_DriverPayroll.sql` → `dbo.DriverAdvances` (date, amount, reason, recovered flag), `dbo.PayRuns` (period, driver, gross, advance recovery, net, status), `dbo.PayRunLines` (per-trip pay basis).
- **Pay model:** per `Driver` add pay config — `PayType` (`PerTrip`/`PerKm`/`Monthly`/`Percentage`), `Rate`. Per-trip pay = rate × (trips or km or % of revenue) in the period.
- **Settlement:** net = gross pay + allowances − advances outstanding.
- **Endpoints:** `api/drivers/{id}/advances` nested, `api/payruns` CRUD + `POST api/payruns/generate?period=YYYY-MM`.
- **UI:** `Pages/Payroll/PayRunList.razor` + `PayRunEditor.razor` (lines + advance recovery). Advances tab on the driver editor.
- **Feeds:** the "Driver pay" line of Trip P&L (#3).
- **Depends on:** Drivers + Trips (done); Fuel (#2) if fuel is driver-paid and recovered.

### 9. GPS / Live Tracking integration

**Goal:** real-time vehicle location, geofencing, trip tracking.

- **Approach:** integrate an external telematics provider (device API / webhook) rather than build hardware. Define a provider-agnostic interface.
- **Migration:** `014_Tracking.sql` → `dbo.VehiclePositions` (vehicleId, lat, lng, speed, heading, deviceTime, ignition), `dbo.Geofences` (name, polygon/circle), `dbo.GeofenceEvents` (enter/exit).
- **Ingest:** `POST api/tracking/ingest` webhook (provider pushes positions) + a poller `BackgroundService` fallback. Keep only latest N positions per vehicle hot; archive the rest.
- **Endpoints:** `GET api/tracking/live` (all vehicles latest), `GET api/tracking/vehicle/{id}/history?from&to`, `api/geofences` CRUD.
- **UI:** `Pages/Tracking/LiveMap.razor` (map with vehicle pins, status colour), `TripReplay.razor` (polyline for a trip's date range). Needs a JS map library (Leaflet + OSM tiles — no API key) added to `wwwroot/js`.
- **Vehicle change:** replace free-text `CurrentLocation` display with the live position when available.
- **Dashboard:** moving / idle / offline counts.
- **Depends on:** Vehicles (done). Provider choice is a prerequisite decision.

### 10–15. Tier 3 (brief)

- **10. Route & Rate-Contract Management** — `Route` master (origin, destination, distance, standard rate), `RateContract` (customer × route × vehicle-type → rate, valid dates). Feeds Trip revenue default and Invoice line pricing.
- **11. Vendor / Procurement / Purchase Orders** — `Vendor` master, `PurchaseOrder` + `PurchaseOrderLine`, goods receipt → `StockMovement` (#7). Workshops and fuel stations become vendor types.
- **12. Consignment note / LR / POD** — `Consignment` (LR number, consignor, consignee, goods, weight, freight) linked to `Trip`; POD image upload + delivered date/time/receiver. One trip can carry many consignments.
- **13. Mobile driver app** — separate client (MAUI or PWA) hitting a scoped `api/driver-app/*`: my trips, update status, upload POD, log expense, log fuel. Token auth per driver.
- **14. Accident / Incident & Insurance Claims** — `Incident` (date, vehicle, driver, location, description, severity, photos), `InsuranceClaim` (policy, claim number, amount claimed/approved, status). Links to `Vehicle` and `Driver`.
- **15. Audit trail + RBAC** — `AuditLog` (user, entity, entityId, action, before/after JSON, timestamp) written via an EF `SaveChanges` interceptor. Roles + permission checks on controllers; role management screen under Settings.

---

## Cross-cutting TODO (not modules)

- [ ] Notification delivery channel (SMTP now; SMS later) — first needed by #5.
- [ ] Map/JS library baseline (Leaflet) — first needed by #9.
- [ ] Background/hosted service host — first needed by #5.
- [ ] Consolidated `EnumDisplay` entries for every new enum.
- [ ] Extend `ReportsController` summary + `ReportsDashboard.razor` as each money module lands. **Owed:** (a) a Fuel section (month spend, fleet avg mileage, cost/km by vehicle, top spenders) — not added after module 2; (b) a Trip P&L section (most/least profitable trips, profit by customer, profit by vehicle) + `NetProfit` column on the Trip list — not added after module 3.
- [ ] Retire dead `ComingSoon.razor` + `/modules/coming-soon/{slug}` route once no longer referenced.

---

## Progress log

| Date | Module | Commit | Notes |
|---|---|---|---|
| 2026-09-01 | Roadmap created | — | This document. |
| 2026-09-01 | Customer Master (CRUD) | `e757dc8` | `dbo.Customers` (migration 006, applied to dev DB). `Customer` model + `CustomerStatus` enum, `CustomerDtos`, `CustomerMapper`, `CustomersController` (`api/customers`), `FleetApiClient` methods, `Customers/CustomerList.razor` + `CustomerEditor.razor` (single card), sidebar nav, Home dashboard section. Full CRUD + search + validation verified via API. |
| 2026-09-01 | Trip ↔ Customer link | `e91ae4e` | Migration 007: nullable `dbo.Trips.CustomerId` → `Customers`, `ON DELETE SET NULL`. `TripUpsertDto.CustomerId`, `CustomerName` on DTOs, `customerId` filter, TripEditor picker + TripList column/filter. Verified: customerName flows through; deleting a customer nulls its trips' CustomerId. **Module 1 complete.** |
| 2026-09-01 | Billing & Invoicing (A/R) | `<pending>` | Migration 010: `dbo.Invoices` / `dbo.InvoiceLines` / `dbo.Payments` (both children `ON DELETE CASCADE`; `Invoices.CustomerId` CASCADE). `Invoice` / `InvoiceLine` / `Payment` models + `InvoiceStatus` / `PaymentMode` enums. `InvoiceLines.TripId` is a **soft link** — indexed, no FK (frozen record; avoids multi-cascade-path with Trips). `InvoiceMapper` computes SubTotal / TaxAmount / Total / AmountPaid / Balance and `EffectiveStatus` (Draft/Sent/Cancelled are user intent; Paid/PartiallyPaid derived from payments). `InvoicesController` (`api/invoices` CRUD + `from-trips` + `aging` + `billable-trips`), `InvoiceLinesController`, `InvoicePaymentsController`. `Billing/InvoiceList.razor` (aging tiles, filters) + `InvoiceEditor.razor` (tabbed Details/Lines/Payments) + `InvoiceFromTrips.razor` (customer → unbilled-trip checkboxes → invoice). Nav + Home A/R section (outstanding / overdue / collected-this-month / aging list). Verified via API: from-trips (75000 + 15% tax = 86250), re-bill blocked (400), manual line recalc, partial→full payment status transitions, overdue flag, aging, cascade delete. Dev DB clean. Fix: replaced a non-ASCII arrow in a C# string literal with `-` (csc on Windows misreads BOM-less UTF-8). |
| 2026-09-01 | Trip Expenses + P&L | `5e16811` | Migration 009: `dbo.TripExpenses` (child of Trips, `ON DELETE CASCADE`). `TripExpense` model + `TripExpenseCategory` / `ExpensePaidBy` enums. `TripExpensesController` (`api/trips/{tripId}/expenses`, mirrors `WorkOrderItemsController`). `TripMapper.ToDetailDto` now takes `fuelCost` and returns `RevenueAmount` / `FuelCost` / `ExpensesTotal` / `DriverPay` (0) / `NetProfit` + `Expenses[]`; `TripsController.GetById` sums `FuelEntries` where `TripId` matches. `TripEditor.razor` rebuilt as tabbed (Trip Details / Expenses / P&L); `FuelEntryList.razor` gained a `?tripId=` filter. Verified via API: P&L math (100000 − 25400 fuel − 6000 exp = 68600), expense CRUD + recalc, validation 400, missing-trip 404, trip delete cascades expenses and detaches fuel. Dev DB clean. |
| 2026-09-01 | Fuel Management | `7127afe` | `dbo.FuelEntries` (migration 008, applied to dev DB). `FuelEntry` model + `FuelPaymentMode` enum; `TotalCost` = Litres × Rate recomputed server-side; `DistanceSinceLast` / `Mileage` (km/L) / `CostPerKm` derived at read time from the previous fill, mileage only full-tank→full-tank. `FuelEntriesController` (`api/fuel-entries`, filters vehicle/driver/trip/fuelType/from/to/search). FK note: `TripId` is `ON DELETE NO ACTION` (avoids multi-cascade-path); `TripsController` + `VehiclesController` null `FuelEntries.TripId` before deleting. `Fuel/FuelEntryList.razor` (mileage + variance badge + KPI tiles) + `FuelEntryEditor.razor` (single card, live total). Nav + Home section (month spend, fleet avg mileage, low-mileage count, recent fills). Verified via API: CRUD, mileage math (400 km / 40 L = 10 km/L, cost/km 28.5), partial-fill excluded, validation 400/404, trip-detach on delete. Dev DB clean. |
