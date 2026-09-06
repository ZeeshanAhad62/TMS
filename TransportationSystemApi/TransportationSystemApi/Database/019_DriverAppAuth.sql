-- =====================================================================
-- Migration 019: Mobile driver app -- per-driver login.
--
-- No new tables. The driver app (wwwroot/driver-app/, a static PWA-style
-- client) authenticates against a scoped surface (api/driver-app/*) with a
-- JWT that carries a token_type = "driver-app" claim + the driver id. A
-- global filter blocks those tokens from every staff controller.
--
-- dbo.Drivers gains:
--   AppLoginEnabled  -- admin toggle; login is refused unless set
--   AppPassword      -- plain text, matching the project's deliberate
--                       plain-text password choice for staff Users
--   AppLastLoginAt   -- stamped on each successful driver-app login
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

IF COL_LENGTH('dbo.Drivers', 'AppLoginEnabled') IS NULL
    ALTER TABLE dbo.Drivers ADD AppLoginEnabled BIT NOT NULL CONSTRAINT DF_Drivers_AppLoginEnabled DEFAULT (0);
GO
IF COL_LENGTH('dbo.Drivers', 'AppPassword') IS NULL
    ALTER TABLE dbo.Drivers ADD AppPassword NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.Drivers', 'AppLastLoginAt') IS NULL
    ALTER TABLE dbo.Drivers ADD AppLastLoginAt DATETIME2 NULL;
GO
