IF DB_ID('PlatformDb')         IS NULL CREATE DATABASE PlatformDb;
IF DB_ID('TenantDb')           IS NULL CREATE DATABASE TenantDb;
IF DB_ID('AnalyticsDb')        IS NULL CREATE DATABASE AnalyticsDb;
IF DB_ID('LogDb')              IS NULL CREATE DATABASE LogDb;
IF DB_ID('NotificationsDb')    IS NULL CREATE DATABASE NotificationsDb;
IF DB_ID('MarketplaceEventsDb') IS NULL CREATE DATABASE MarketplaceEventsDb;
IF DB_ID('SyncDb')             IS NULL CREATE DATABASE SyncDb;
GO

PRINT 'All 7 databases created (or already existed).';
