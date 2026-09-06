-- ProviderHub seed data.
--
--   sqlcmd -S localhost,1433 -U sa -P 'ProviderHub!2026' -C -d ProviderHub -i db/seed.sql
--
-- Run schema.sql first. This script is idempotent: every insert is guarded by a NOT EXISTS on
-- the natural key of the row, so running it twice changes nothing the second time. Identity
-- values are never hard-coded; rows are related by looking each other up by name or tax id,
-- which is what makes the script safe to run against a database that already holds data.
--
-- Every NIT below carries its real check digit, computed with the official algorithm, so the
-- data survives the same validation the application applies to user input.

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-------------------------------------------------------------------------------
-- Services (12 rows)
-------------------------------------------------------------------------------
INSERT INTO dbo.Services (Name, HourlyRateAmount, HourlyRateCurrency)
SELECT source.Name, source.HourlyRate, 'USD'
FROM (VALUES
    (N'Space content download'             ,  120.00),
    (N'Forced byte disappearance'          ,   95.50),
    (N'Quantum cache warming'              ,  210.00),
    (N'Legacy protocol translation'        ,   88.75),
    (N'Orbital data relay'                 ,  340.00),
    (N'Cold storage archival'              ,   45.25),
    (N'Latency negotiation'                ,  175.00),
    (N'Packet loss recovery'               ,  132.40),
    (N'Firmware exorcism'                  ,  260.00),
    (N'Entropy auditing'                   ,  199.99),
    (N'Bandwidth reclamation'              ,   64.00),
    (N'Schema archaeology'                 ,  155.80)
) AS source (Name, HourlyRate)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Services existing WHERE existing.Name = source.Name);

-------------------------------------------------------------------------------
-- Providers (10 rows)
-------------------------------------------------------------------------------
INSERT INTO dbo.Providers (NitBaseNumber, NitCheckDigit, Name, Website, Email)
SELECT source.NitBaseNumber, source.NitCheckDigit, source.Name, source.Website, source.Email
FROM (VALUES
    (N'890903938', 8, N'Importaciones Tekus S.A.'       , N'https://tekus.co'               , N'contacto@tekus.co'),
    (N'900373115', 3, N'Andina Cloud Services S.A.S.'   , N'https://andinacloud.co'         , N'hola@andinacloud.co'),
    (N'811021363', 0, N'Bits del Caribe Ltda.'          , N'https://bitsdelcaribe.com'      , N'info@bitsdelcaribe.com'),
    (N'830045781', 9, N'Cordillera Software Group'      , N'https://cordillerasoft.com'     , N'ventas@cordillerasoft.com'),
    (N'860512336', 7, N'Pacifico Data Works'            , N'https://pacificodata.co'        , N'contacto@pacificodata.co'),
    (N'901234567', 7, N'Magdalena Systems S.A.S.'       , N'https://magdalenasystems.co'    , N'soporte@magdalenasystems.co'),
    (N'805019876', 9, N'Valle Digital Partners'         , N'https://valledigital.com'       , N'hello@valledigital.com'),
    (N'890200354', 1, N'Orinoco Infrastructure S.A.'    , N'https://orinocoinfra.com'       , N'contact@orinocoinfra.com'),
    (N'900876543', 1, N'Sierra Nevada Analytics'        , N'https://sierranevada.io'        , N'team@sierranevada.io'),
    (N'811400229', 8, N'Cafetero Networks Ltda.'        , N'https://cafeteronetworks.co'    , N'admin@cafeteronetworks.co')
) AS source (NitBaseNumber, NitCheckDigit, Name, Website, Email)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Providers existing WHERE existing.NitBaseNumber = source.NitBaseNumber);

-------------------------------------------------------------------------------
-- Service offerings (26 rows): who offers what
-------------------------------------------------------------------------------
INSERT INTO dbo.ServiceOfferings (ProviderId, ServiceId)
SELECT p.Id, s.Id
FROM (VALUES
    (N'890903938', N'Space content download'),
    (N'890903938', N'Forced byte disappearance'),
    (N'890903938', N'Orbital data relay'),
    (N'900373115', N'Quantum cache warming'),
    (N'900373115', N'Latency negotiation'),
    (N'900373115', N'Bandwidth reclamation'),
    (N'811021363', N'Legacy protocol translation'),
    (N'811021363', N'Cold storage archival'),
    (N'830045781', N'Packet loss recovery'),
    (N'830045781', N'Schema archaeology'),
    (N'830045781', N'Space content download'),
    (N'860512336', N'Firmware exorcism'),
    (N'860512336', N'Entropy auditing'),
    (N'901234567', N'Forced byte disappearance'),
    (N'901234567', N'Quantum cache warming'),
    (N'901234567', N'Cold storage archival'),
    (N'805019876', N'Latency negotiation'),
    (N'805019876', N'Legacy protocol translation'),
    (N'890200354', N'Orbital data relay'),
    (N'890200354', N'Bandwidth reclamation'),
    (N'890200354', N'Packet loss recovery'),
    (N'900876543', N'Entropy auditing'),
    (N'900876543', N'Schema archaeology'),
    (N'811400229', N'Firmware exorcism'),
    (N'811400229', N'Space content download'),
    (N'811400229', N'Cold storage archival')
) AS source (NitBaseNumber, ServiceName)
JOIN dbo.Providers p ON p.NitBaseNumber = source.NitBaseNumber
JOIN dbo.Services  s ON s.Name = source.ServiceName
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ServiceOfferings existing
    WHERE existing.ProviderId = p.Id AND existing.ServiceId = s.Id);

-------------------------------------------------------------------------------
-- Countries where each service is offered (48 rows)
-------------------------------------------------------------------------------
INSERT INTO dbo.ServiceOfferingCountries (ServiceOfferingId, CountryCode)
SELECT o.Id, source.CountryCode
FROM (VALUES
    (N'890903938', N'Space content download', 'CO'),
    (N'890903938', N'Space content download', 'MX'),
    (N'890903938', N'Forced byte disappearance', 'CO'),
    (N'890903938', N'Orbital data relay', 'CO'),
    (N'890903938', N'Orbital data relay', 'MX'),
    (N'890903938', N'Orbital data relay', 'PE'),
    (N'900373115', N'Quantum cache warming', 'CO'),
    (N'900373115', N'Quantum cache warming', 'CL'),
    (N'900373115', N'Latency negotiation', 'CO'),
    (N'900373115', N'Bandwidth reclamation', 'CO'),
    (N'900373115', N'Bandwidth reclamation', 'EC'),
    (N'900373115', N'Bandwidth reclamation', 'PE'),
    (N'811021363', N'Legacy protocol translation', 'CO'),
    (N'811021363', N'Legacy protocol translation', 'PA'),
    (N'811021363', N'Cold storage archival', 'PA'),
    (N'830045781', N'Packet loss recovery', 'CO'),
    (N'830045781', N'Packet loss recovery', 'AR'),
    (N'830045781', N'Schema archaeology', 'CO'),
    (N'830045781', N'Schema archaeology', 'AR'),
    (N'830045781', N'Schema archaeology', 'UY'),
    (N'830045781', N'Space content download', 'AR'),
    (N'860512336', N'Firmware exorcism', 'CO'),
    (N'860512336', N'Firmware exorcism', 'BR'),
    (N'860512336', N'Entropy auditing', 'BR'),
    (N'901234567', N'Forced byte disappearance', 'MX'),
    (N'901234567', N'Forced byte disappearance', 'US'),
    (N'901234567', N'Quantum cache warming', 'US'),
    (N'901234567', N'Cold storage archival', 'MX'),
    (N'901234567', N'Cold storage archival', 'US'),
    (N'901234567', N'Cold storage archival', 'CA'),
    (N'805019876', N'Latency negotiation', 'CO'),
    (N'805019876', N'Latency negotiation', 'ES'),
    (N'805019876', N'Legacy protocol translation', 'ES'),
    (N'890200354', N'Orbital data relay', 'CO'),
    (N'890200354', N'Orbital data relay', 'VE'),
    (N'890200354', N'Bandwidth reclamation', 'CO'),
    (N'890200354', N'Packet loss recovery', 'VE'),
    (N'890200354', N'Packet loss recovery', 'CO'),
    (N'890200354', N'Packet loss recovery', 'PE'),
    (N'900876543', N'Entropy auditing', 'CO'),
    (N'900876543', N'Entropy auditing', 'CL'),
    (N'900876543', N'Entropy auditing', 'PE'),
    (N'900876543', N'Schema archaeology', 'CL'),
    (N'811400229', N'Firmware exorcism', 'CO'),
    (N'811400229', N'Space content download', 'CO'),
    (N'811400229', N'Space content download', 'EC'),
    (N'811400229', N'Cold storage archival', 'EC'),
    (N'811400229', N'Cold storage archival', 'PE')
) AS source (NitBaseNumber, ServiceName, CountryCode)
JOIN dbo.Providers        p ON p.NitBaseNumber = source.NitBaseNumber
JOIN dbo.Services         s ON s.Name = source.ServiceName
JOIN dbo.ServiceOfferings o ON o.ProviderId = p.Id AND o.ServiceId = s.Id
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ServiceOfferingCountries existing
    WHERE existing.ServiceOfferingId = o.Id AND existing.CountryCode = source.CountryCode);

COMMIT TRANSACTION;
GO
