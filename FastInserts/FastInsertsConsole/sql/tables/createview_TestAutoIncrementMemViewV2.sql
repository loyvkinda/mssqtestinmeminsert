create view dbo.TestAutoIncrementMemViewV2
with schemabinding
as
select id, SomeData, AppKey, ThreadId, ThreadCount, CreateAt from dbo.TestAutoIncrementInMem with(snapshot);
go


