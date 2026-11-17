SET XACT_ABORT ON;
BEGIN TRANSACTION;
CREATE TABLE dbo.SchemaVersion (
 ScriptId nvarchar(100) NOT NULL PRIMARY KEY,
 Checksum char(64) NOT NULL,
 AppliedUtc datetime2 NOT NULL,
 Result nvarchar(20) NOT NULL CHECK(Result='Applied'),
 DurationMs bigint NOT NULL CHECK(DurationMs>=0)
);
CREATE TABLE dbo.SchemaExecutionError (
 ExecutionId uniqueidentifier NOT NULL PRIMARY KEY,
 ScriptId nvarchar(100) NOT NULL,
 FailedUtc datetime2 NOT NULL,
 ErrorNumber int NOT NULL
);
COMMIT;
