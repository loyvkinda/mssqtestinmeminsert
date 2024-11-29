create view [dbo].[TestAutoIncrementMemView]
with schemabinding
as
select id, SomeData, AppKey, ThreadId, ThreadCount, CreateAt from dbo.TestAutoIncrementInMem with(snapshot);
