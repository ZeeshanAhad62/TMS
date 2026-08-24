-- =====================================================================
-- Migration 002: switch Users to plain-text passwords, add LoginHistory
--
-- NOTE ON PASSWORDS: at the user's explicit request, passwords are now
-- stored in plain text (Users.Password) instead of hashed. This is a
-- deliberate, documented deviation from best practice -- see the
-- conversation this was decided in. Do not "fix" this back to hashing
-- without checking with the project owner first, since it would break
-- every existing stored password (hashes are one-way).
--
-- Safe to re-run: every statement is guarded with an existence check.
-- =====================================================================

USE FleetMasterDb;
GO

-- ---------------------------------------------------------------------
-- Users: PasswordHash -> Password (plain text)
-- ---------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'PasswordHash')
BEGIN
    EXEC sp_rename 'dbo.Users.PasswordHash', 'Password', 'COLUMN';
    ALTER TABLE dbo.Users ALTER COLUMN Password NVARCHAR(200) NOT NULL;
END
GO

-- ---------------------------------------------------------------------
-- LoginHistory
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.LoginHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoginHistory
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        UserId              INT             NULL,
        AttemptedUsername   NVARCHAR(100)   NOT NULL,
        IsSuccessful        BIT             NOT NULL,
        LoginAt             DATETIME2       NOT NULL CONSTRAINT DF_LoginHistory_LoginAt DEFAULT (SYSUTCDATETIME()),
        IpAddress            NVARCHAR(64)   NULL,
        DeviceId             NVARCHAR(100)  NULL,
        UserAgent             NVARCHAR(500) NULL,
        Browser               NVARCHAR(100) NULL,
        OperatingSystem        NVARCHAR(100) NULL,
        City                    NVARCHAR(100) NULL,
        Region                  NVARCHAR(100) NULL,
        Country                 NVARCHAR(100) NULL,
        CONSTRAINT PK_LoginHistory PRIMARY KEY (Id),
        CONSTRAINT FK_LoginHistory_Users_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users(Id) ON DELETE SET NULL
    );
    CREATE INDEX IX_LoginHistory_UserId ON dbo.LoginHistory(UserId);
    CREATE INDEX IX_LoginHistory_LoginAt ON dbo.LoginHistory(LoginAt DESC);
END
GO
