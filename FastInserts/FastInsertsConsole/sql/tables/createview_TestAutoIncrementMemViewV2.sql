create view dbo.TestAutoIncrementMemViewV2
with schemabinding
as
select id, SomeData, AppKey, ThreadId, CreateAt from dbo.TestAutoIncrementInMem with(snapshot);
go


