# Module Roadmap — Transport Management System

Living document. Tracks planned modules, build order, and the per-module steps.
Update the checkboxes and the "Status" line as work lands.

- **Stack:** Blazor Web (`TransportationSystemWeb`) · .NET API (`TransportationSystemApi`) · shared DTOs (`TransportationSystemShared`) · SQL Server (`FleetMasterDb`).
- **Migrations:** plain numbered `.sql` files in `TransportationSystemApi/TransportationSystemApi/Database/`, run manually with `sqlcmd`. No EF migrations, no auto-apply. Latest applied: `013_PartsInventory.sql`.
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

- [x] **5. Compliance & Document-Expiry Alert Engine** — done. Dashboard + API (`35d966c`) plus `AlertConfig` / `AlertLog` tables and a daily SMTP hosted service (`011`). Deferred: an optional `ExpiryDate` on uploaded `VehicleDocument` / `DriverDocument` (out of scope — the engine reads the structured expiry fields already on `Vehicle` / `Driver`, not uploaded files).
- [x] **6. Tyre Management (promote to full module)** — done (`012`).
- [x] **7. Spare Parts Inventory / Stores** — done (`013`).
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

### 5. Compliance & Document-Expiry Alert Engine — DONE (`011`)

**Goal:** one dashboard for every expiring document + scheduled email reminders.

- **Sources already in DB:** `Vehicle` — RC / fitness / permit / insurance / pollution / tax expiry dates. `Driver` — `LicenseExpiryDate`. (`VehicleDocument` / `DriverDocument` uploaded-file `ExpiryDate` stayed deferred — the engine only reads the structured fields above.)
- **Migration:** `011_ComplianceAlerts.sql` → `dbo.AlertConfigs` (nullable `EntityType` / `DocumentType` = wildcard, `ThresholdDays`, `RecipientEmails`, `IsActive`), `dbo.AlertLog` (what was sent, when; unique index on `EntityType, EntityId, DocumentType, ExpiryDate, Severity` is the dedupe key — re-alerts only on severity escalation or a renewed expiry date).
- **Service:** `ComplianceScanner` (existing, `35d966c`) produces the unified `{ EntityType, EntityId, EntityName, DocumentType, ExpiryDate, DaysRemaining, Severity }` list; `ComplianceAlertService` (new, scoped) matches it against active `AlertConfig` rows and emails via `IEmailSender`.
- **Notifications:** `ComplianceAlertHostedService` (`BackgroundService`) runs on a timer, default every 24h (`Compliance:AlertScanIntervalHours`), first run right after startup. `SmtpEmailSender` wraps `System.Net.Mail`; `Smtp:Host` empty (the dev default) makes `TrySendAsync` log a warning and return `false` instead of throwing, so the job runs safely with no mail server configured.
- **Endpoints:** `GET api/compliance/expiries` / `summary` (existing) + `GET api/compliance/document-types`, `api/compliance/config` CRUD (`AlertConfigsController`), `GET api/compliance/alert-log`, `POST api/compliance/run-alerts` (manual trigger, used by the UI's "Run Now").
- **UI:** `Pages/Compliance/ComplianceDashboard.razor` (existing). `Pages/Compliance/AlertSettings.razor` (new) — add/deactivate/delete alert rules, "Run Now" button, recent alert-activity table. Sidebar gained an "Alert Settings" sub-link under Compliance.
- **Dashboard:** "X documents expiring in 30 days" headline (existing, `35d966c`) — unchanged.
- **Depends on:** Fleet + Drivers (done). Existing `AlertRule` model was left as-is (still vehicle-scoped, used by `VehicleAlertsController`) — not folded in, to keep this module's scope to the compliance-wide engine.
- **Verified via API + browser:** CRUD on `AlertConfig` (create/update/deactivate/delete), `run-alerts` with 7 pre-existing expired vehicle/driver documents (`itemsScanned: 7`), 0 sent while `Smtp:Host` is unconfigured (no crash, warning logged), inactive configs excluded from the scan, dedupe index in place. Dev DB clean (no leftover `AlertConfigs` / `AlertLog` rows).

### 6. Tyre Management (promote to full module) — DONE (`012`)

**Goal:** track each tyre as an asset across its life, cost-per-km, inventory.

- **Existing (untouched):** the nested `api/vehicles/{id}/tyres` (`VehicleTyresController`) and its `TyreDto`/`TyreUpsertDto`/`TyreReplacementHistoryDto` stay exactly as they were — still the vehicle editor's quick "add a tyre" tab. `VehicleTyresController.Create` now also seeds a `TyreEvents` Fit row and defaults `Status = Fitted` so tyres added there aren't invisible to the new module.
- **Migration:** `012_TyreManagement.sql` — `dbo.Tyres.VehicleId` made nullable (`ON DELETE SET NULL`, was `NOT NULL`/`CASCADE`) so a tyre's asset/event history survives past the vehicle it was last fitted to; new columns `SerialNumber`, `Pattern`, `PurchaseDate`, `PurchaseCost`, `Status`, `TotalDistanceRunCarried`, `CreatedAt`, `UpdatedAt`. New `dbo.TyreEvents` (fit / remove / rotate / retread / inspect / scrap, `ON DELETE CASCADE` from `Tyres`). `VehiclesController.Delete` unassigns any still-fitted tyres back to stock (carrying distance forward, logging a Remove event) before the vehicle row goes, since the FK alone only nulls the column.
- **Deliberate deviations from the original spec, disclosed here:**
  - **`dbo.TyreStock` was not built as a separate table.** "Stock" is modeled as `Tyre` rows with `VehicleId IS NULL` — one asset table for a tyre's whole life instead of a copy-in/copy-out dance between two tables when it's pulled or refitted. `GET api/tyres/stock` is a thin filter over the same table.
  - **`TyreStatus` has 3 values, not 4** (`InStock` / `Fitted` / `Scrapped` — no standalone `Retreaded`). A retread doesn't change where a tyre physically is, so a 4th status would overlap with the other 3; retread history instead lives in `TyreEvents` and surfaces as `RetreadCount` / `LastRetreadDate` (derived).
  - **No visual axle/position map** on the vehicle editor — deferred; the position data it would need is already exposed via the nested tyre list and `Tyre.Position`.
  - **Existing `AlertRule` model left alone** (unrelated, no change needed here).
- **Derived (in `TyreMapper`, not stored):** `DistanceRun` = `TotalDistanceRunCarried` + (if `Fitted`: current vehicle odometer − `InstallationOdometer`, floored at 0). `CostPerKm` = (`PurchaseCost` + Σ retread event costs) / `DistanceRun`, null until there's billable distance.
- **Endpoints:** `api/tyres` CRUD + `status`/`vehicleId`/`search` filters, `api/tyres/stock`, `api/tyres/{id}/events` (`GET`/`POST`/`DELETE`). `POST .../events` is the only way fitment changes — Fit/Remove/Rotate/Retread/Inspect/Scrap each validate the tyre's current state (e.g. can't fit an already-fitted tyre, can't rotate one that's in stock, can't retread one still on a vehicle, nothing works on a scrapped one) and keep `Status`/`VehicleId`/`Position`/`InstallationOdometer` in lock-step with the log.
- **UI:** `Pages/Tyres/TyreList.razor` (status/vehicle/brand search + KPI tiles) + `TyreEditor.razor` (tabbed `Details` / `Events`, event form adapts to the selected event type). Sidebar "Tyre Management" module; Home dashboard section (in-stock / fitted counts, worst cost/km, "Tyres to Watch" list); quick-link button.
- **Depends on:** Vehicles (done). Existing tyre data migrated forward automatically (existing rows default to `Status = Fitted`, matching the fact that migration 001 required every tyre to have a `VehicleId`).
- **Verified via API + browser:** full lifecycle (Fit → Rotate → Remove → Retread → re-Fit → Scrap) with correct `DistanceRun`/`CostPerKm` math at each step; invalid-transition rejections (double-fit, rotate/retread from the wrong state, any event on a scrapped tyre) all return 400; nested vehicle-tab create still works and seeds an event; deleting a vehicle unassigns its fitted tyres back to stock instead of destroying their history; top-level create/edit/delete and the Events tab exercised end-to-end in the browser with no console errors. Dev DB left clean.

### 7. Spare Parts Inventory / Stores — DONE (`013`)

**Goal:** real stock for parts, linked to work-order consumption.

- **Migration:** `013_PartsInventory.sql` → `dbo.Parts` (part master: number, name, unit, reorder level, standard cost), `dbo.StockMovements` (Receipt / Issue / Adjust, qty, unit cost, generic `ReferenceType`/`ReferenceId` soft link -- today only `WorkOrder`, kept generic so a future `PurchaseOrder` reference needs no schema change). `dbo.WorkOrderItems` gains nullable `PartId` (FK, `ON DELETE SET NULL`) and `StockMovementId` (soft link, no FK) tying a line to the Issue movement it created.
- **Deliberate deviation, disclosed here:** no `dbo.Suppliers` table -- that's Vendor-master territory owned by module 11 when it lands. `StockMovements.SupplierName` is free text for receipts in the meantime.
- **WorkOrderItem change:** issuing a line against a stocked part (`PartId` set) auto-creates an Issue `StockMovement` and decrements on-hand; editing the line's quantity/part keeps that movement in sync, and deleting the line (or the whole work order) deletes the movement, restoring on-hand. Direct edits to a work-order-linked movement are blocked from the Parts side (`400`) -- edit the line instead.
- **Derived (in `PartMapper`, not stored):** on-hand qty = Σ movements (Receipt +, Issue −, Adjust as entered); stock value = on-hand × `StandardCost`; below-reorder = on-hand < `ReorderLevel`.
- **Endpoints:** `api/parts` CRUD (rejects duplicate `PartNumber`), `api/parts/{id}/movements` (`GET`/`POST` manual only/`DELETE` with the work-order-linked guard above), `GET api/parts/low-stock`.
- **UI:** `Pages/Inventory/PartList.razor` (search + KPI tiles) + `PartEditor.razor` (tabbed `Details` / `Stock Movements`) + `GoodsReceipt.razor` (multi-line batch receipt, one Receipt movement per line). `WorkOrderEditor.razor`'s Parts & Materials tab gained an optional stocked-part picker. Sidebar "Spare Parts Inventory" module; Home dashboard section (total / below-reorder / stock value + reorder list); quick-link.
- **Depends on:** Maintenance module (done) for the WO link; Vendors (#11) optional, not built.
- **Bug found + fixed during verification:** `WorkOrdersController.Delete` deleted a work order via DB cascade (which removes its `WorkOrderItems`) without also deleting the `StockMovement` rows those items had created -- on-hand stayed decremented and the movement was orphaned (`ReferenceId` pointing at a now-gone work order). Fixed by deleting those movements explicitly before the work order, mirroring the existing `VehiclesController.Delete` pattern for `FuelEntries`/`Tyres`. Also missing: `.ThenInclude(i => i.Part)` on `WorkOrdersController`'s `GetAll`/`GetById` queries, which left `WorkOrderItemDto.PartNumber` null even when a line was linked -- added.
- **Verified via API + browser:** part CRUD + duplicate-number rejection; Receipt/Issue/Adjust math and the below-reorder flag; linking a work-order line to a part auto-issues and decrements on-hand; changing that line's quantity keeps the movement in sync; deleting the line restores on-hand; deleting a movement created from a work-order line is blocked (`400`); deleting the work order itself now correctly restores on-hand (post-fix); Goods Receipt screen recording a batch line. Dev DB left clean.

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

- [x] Notification delivery channel (SMTP now; SMS later) — done for SMTP via `IEmailSender` / `SmtpEmailSender` (module 5, `011`). SMS still open, first needed if/when a channel beyond email is required.
- [ ] Map/JS library baseline (Leaflet) — first needed by #9.
- [x] Background/hosted service host — done via `ComplianceAlertHostedService` (module 5, `011`); reusable pattern for future scheduled jobs (e.g. #9's tracking poller).
- [ ] Consolidated `EnumDisplay` entries for every new enum.
- [ ] Extend `ReportsController` summary + `ReportsDashboard.razor` as each money module lands. **Owed:** (a) a Fuel section (month spend, fleet avg mileage, cost/km by vehicle, top spenders) — not added after module 2; (b) a Trip P&L section (most/least profitable trips, profit by customer, profit by vehicle) + `NetProfit` column on the Trip list — not added after module 3.
- [ ] Retire dead `ComingSoon.razor` + `/modules/coming-soon/{slug}` route once no longer referenced.

---

## Progress log

| Date | Module | Commit | Notes |
|---|---|---|---|
| 2026-09-05 | Spare Parts Inventory (module 7) | _pending_ | Migration 013: `dbo.Parts` + `dbo.StockMovements`; `WorkOrderItems` gained `PartId` (FK, SET NULL) + `StockMovementId` (soft link). `Part`/`StockMovement` models, `PartMapper` (on-hand/stock-value/below-reorder derived), `PartsController` (`api/parts` CRUD + `movements` + `low-stock`). `WorkOrderItemsController` now syncs an Issue `StockMovement` with a part-linked line on create/update/delete. `Pages/Inventory/PartList.razor` + `PartEditor.razor` (tabbed) + `GoodsReceipt.razor`; `WorkOrderEditor.razor` gained a stocked-part picker; sidebar + Home dashboard section. **Two bugs found and fixed during verification:** (1) `WorkOrdersController.Delete` didn't clean up a deleted work order's issued `StockMovement`s, orphaning them and leaving on-hand permanently decremented -- fixed by deleting them explicitly first. (2) `WorkOrdersController` `GetAll`/`GetById` were missing `.ThenInclude(i => i.Part)`, so `WorkOrderItemDto.PartNumber` rendered as "—" even when a line was linked -- fixed. Verified via API (full CRUD, Receipt/Issue/Adjust math, part-link auto-issue/sync/restore, delete guards, both bugs confirmed present then fixed) and browser (part create → receipt → work-order part-linked line → goods receipt, no console errors). Dev DB clean. **Module 7 complete.**
| 2026-09-05 | Tyre Management (module 6) | `e086c9b` | Migration 012: `dbo.Tyres.VehicleId` nullable (`ON DELETE SET NULL`) + new columns (`SerialNumber`/`Pattern`/`PurchaseDate`/`PurchaseCost`/`Status`/`TotalDistanceRunCarried`/`CreatedAt`/`UpdatedAt`), new `dbo.TyreEvents`. `TyreEvent` model, `TyreStatus`/`TyreEventType` enums, `TyreMapper`, `TyresController` (`api/tyres` CRUD + `stock` + `{id}/events`). `VehiclesController.Delete` now unassigns fitted tyres to stock (carrying distance, logging a Remove event) before the vehicle cascade. `VehicleTyresController.Create` seeds a matching Fit event; nested tab otherwise untouched. `FleetApiClient` methods (`*TyreRecord*`/`*TyreEvent*`, distinct from the existing nested `*Tyre*` methods). `Pages/Tyres/TyreList.razor` + `TyreEditor.razor` (tabbed Details/Events); sidebar + Home dashboard section + quick-link. Verified via API (full Fit→Rotate→Remove→Retread→re-Fit→Scrap lifecycle, distance/cost-per-km math, all invalid-transition 400s, vehicle-delete cascade) and browser (create → fit event → list → delete, no console errors). Dev DB clean — note: cascade testing deleted then recreated the pre-existing placeholder vehicle `VEH-00004` (now `VEH-00005`, same reg/make/model). **Module 6 complete.**
| 2026-09-05 | Compliance alert config + delivery (module 5 completion) | `b13e871` | Migration 011: `dbo.AlertConfigs` + `dbo.AlertLog` (unique dedupe index on entity/document/expiry/severity). `AlertConfig` / `AlertLog` models, `AlertConfigMapper`, `AlertConfigsController` (`api/compliance/config` CRUD), `ComplianceController` gained `document-types` / `alert-log` / `run-alerts`. `IEmailSender` + `SmtpEmailSender` (System.Net.Mail, no-op-with-warning when `Smtp:Host` unset) + `ComplianceAlertService` (shared scan/match/send logic) + `ComplianceAlertHostedService` (24h timer, configurable via `Compliance:AlertScanIntervalHours`). `FleetApiClient` methods; `Pages/Compliance/AlertSettings.razor` (add/deactivate/delete rules, Run Now, recent activity); sidebar sub-link. Verified via API (CRUD, 7 expired items scanned, 0 sent without SMTP configured, inactive-config skip) and browser (add rule → Run Now → deactivate → delete, no console errors, existing Compliance dashboard unaffected). Dev DB clean. **Module 5 complete.**
| 2026-09-01 | Roadmap created | — | This document. |
| 2026-09-01 | Customer Master (CRUD) | `e757dc8` | `dbo.Customers` (migration 006, applied to dev DB). `Customer` model + `CustomerStatus` enum, `CustomerDtos`, `CustomerMapper`, `CustomersController` (`api/customers`), `FleetApiClient` methods, `Customers/CustomerList.razor` + `CustomerEditor.razor` (single card), sidebar nav, Home dashboard section. Full CRUD + search + validation verified via API. |
| 2026-09-01 | Trip ↔ Customer link | `e91ae4e` | Migration 007: nullable `dbo.Trips.CustomerId` → `Customers`, `ON DELETE SET NULL`. `TripUpsertDto.CustomerId`, `CustomerName` on DTOs, `customerId` filter, TripEditor picker + TripList column/filter. Verified: customerName flows through; deleting a customer nulls its trips' CustomerId. **Module 1 complete.** |
| 2026-09-01 | Compliance dashboard + API | `35d966c` | No migration — scans the structured expiry fields already on `Vehicle` (RC / fitness / permit / insurance / pollution / road tax) and `Driver.LicenseExpiryDate`. `ComplianceScanner` (static, in `Mapping/`, reusable by a future job) grades each into `ComplianceSeverity` Expired / Critical (≤7d) / Warning (≤30d) / Upcoming (≤ window, default 60d). `ComplianceController`: `GET api/compliance/expiries?entityType=&severity=&withinDays=&search=` + `GET api/compliance/summary`. `Compliance/ComplianceDashboard.razor` (severity tiles, filters, click-through to vehicle/driver editor); sidebar nav; Home banner (`_compliance` summary, links to /compliance). `FleetApiClient` methods. Verified via API: severity grading at 5/24/53-day and expired dates, `withinDays` window include/exclude, entityType + severity + search filters. Dev DB clean (test drivers deleted). **Remaining for module 5:** alert-config tables + daily SMTP hosted service. |
| 2026-09-01 | Billing & Invoicing (A/R) | `d568d65` | Migration 010: `dbo.Invoices` / `dbo.InvoiceLines` / `dbo.Payments` (both children `ON DELETE CASCADE`; `Invoices.CustomerId` CASCADE). `Invoice` / `InvoiceLine` / `Payment` models + `InvoiceStatus` / `PaymentMode` enums. `InvoiceLines.TripId` is a **soft link** — indexed, no FK (frozen record; avoids multi-cascade-path with Trips). `InvoiceMapper` computes SubTotal / TaxAmount / Total / AmountPaid / Balance and `EffectiveStatus` (Draft/Sent/Cancelled are user intent; Paid/PartiallyPaid derived from payments). `InvoicesController` (`api/invoices` CRUD + `from-trips` + `aging` + `billable-trips`), `InvoiceLinesController`, `InvoicePaymentsController`. `Billing/InvoiceList.razor` (aging tiles, filters) + `InvoiceEditor.razor` (tabbed Details/Lines/Payments) + `InvoiceFromTrips.razor` (customer → unbilled-trip checkboxes → invoice). Nav + Home A/R section (outstanding / overdue / collected-this-month / aging list). Verified via API: from-trips (75000 + 15% tax = 86250), re-bill blocked (400), manual line recalc, partial→full payment status transitions, overdue flag, aging, cascade delete. Dev DB clean. Fix: replaced a non-ASCII arrow in a C# string literal with `-` (csc on Windows misreads BOM-less UTF-8). |
| 2026-09-01 | Trip Expenses + P&L | `5e16811` | Migration 009: `dbo.TripExpenses` (child of Trips, `ON DELETE CASCADE`). `TripExpense` model + `TripExpenseCategory` / `ExpensePaidBy` enums. `TripExpensesController` (`api/trips/{tripId}/expenses`, mirrors `WorkOrderItemsController`). `TripMapper.ToDetailDto` now takes `fuelCost` and returns `RevenueAmount` / `FuelCost` / `ExpensesTotal` / `DriverPay` (0) / `NetProfit` + `Expenses[]`; `TripsController.GetById` sums `FuelEntries` where `TripId` matches. `TripEditor.razor` rebuilt as tabbed (Trip Details / Expenses / P&L); `FuelEntryList.razor` gained a `?tripId=` filter. Verified via API: P&L math (100000 − 25400 fuel − 6000 exp = 68600), expense CRUD + recalc, validation 400, missing-trip 404, trip delete cascades expenses and detaches fuel. Dev DB clean. |
| 2026-09-01 | Fuel Management | `7127afe` | `dbo.FuelEntries` (migration 008, applied to dev DB). `FuelEntry` model + `FuelPaymentMode` enum; `TotalCost` = Litres × Rate recomputed server-side; `DistanceSinceLast` / `Mileage` (km/L) / `CostPerKm` derived at read time from the previous fill, mileage only full-tank→full-tank. `FuelEntriesController` (`api/fuel-entries`, filters vehicle/driver/trip/fuelType/from/to/search). FK note: `TripId` is `ON DELETE NO ACTION` (avoids multi-cascade-path); `TripsController` + `VehiclesController` null `FuelEntries.TripId` before deleting. `Fuel/FuelEntryList.razor` (mileage + variance badge + KPI tiles) + `FuelEntryEditor.razor` (single card, live total). Nav + Home section (month spend, fleet avg mileage, low-mileage count, recent fills). Verified via API: CRUD, mileage math (400 km / 40 L = 10 km/L, cost/km 28.5), partial-fill excluded, validation 400/404, trip-detach on delete. Dev DB clean. |
