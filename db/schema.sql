IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    CREATE TABLE [Providers] (
        [Id] int NOT NULL IDENTITY,
        [NitBaseNumber] nvarchar(15) NOT NULL,
        [NitCheckDigit] tinyint NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Website] nvarchar(2048) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        CONSTRAINT [PK_Providers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    CREATE TABLE [Services] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [HourlyRateAmount] decimal(18,2) NOT NULL,
        [HourlyRateCurrency] char(3) NOT NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    CREATE TABLE [ServiceOfferings] (
        [Id] int NOT NULL IDENTITY,
        [ProviderId] int NOT NULL,
        [ServiceId] int NOT NULL,
        CONSTRAINT [PK_ServiceOfferings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceOfferings_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Providers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ServiceOfferings_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    CREATE TABLE [ServiceOfferingCountries] (
        [CountryCode] char(2) NOT NULL,
        [ServiceOfferingId] int NOT NULL,
        CONSTRAINT [PK_ServiceOfferingCountries] PRIMARY KEY ([ServiceOfferingId], [CountryCode]),
        CONSTRAINT [FK_ServiceOfferingCountries_ServiceOfferings_ServiceOfferingId] FOREIGN KEY ([ServiceOfferingId]) REFERENCES [ServiceOfferings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Providers_Email] ON [Providers] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Providers_Nit] ON [Providers] ([NitBaseNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ServiceOfferings_ServiceId] ON [ServiceOfferings] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ServiceOfferings_Provider_Service] ON [ServiceOfferings] ([ProviderId], [ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Services_Name] ON [Services] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906231851_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906231851_InitialSchema', N'10.0.11');
END;

COMMIT;
GO

