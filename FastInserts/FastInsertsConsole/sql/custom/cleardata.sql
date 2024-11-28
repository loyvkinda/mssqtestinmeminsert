-- Сброс данных.
set nocount on;
if object_id('dbo.TestAutoIncrement') is not null
    truncate table dbo.TestAutoIncrement;
if object_id('dbo.TestAutoIncrementGuid') is not null
    truncate table dbo.TestAutoIncrementGuid;
if object_id('dbo.TestAutoIncrementGuidNewId') is not null
    truncate table dbo.TestAutoIncrementGuidNewId;
if object_id('dbo.TestAutoIncrementGuidNewSequentialid') is not null
    truncate table dbo.TestAutoIncrementGuidNewSequentialid;
if object_id('dbo.TestAutoIncrementIdentity') is not null
    truncate table dbo.TestAutoIncrementIdentity;
if object_id('dbo.TestAutoIncrementSeq') is not null
    truncate table dbo.TestAutoIncrementSeq;
if object_id('dbo.TestAutoIncrementInMem') is not null
    delete from dbo.TestAutoIncrementInMem;
go
if object_id('dbo.TestAutoIncrementInMemIdentity') is not null
    delete from dbo.TestAutoIncrementInMemIdentity;
go
