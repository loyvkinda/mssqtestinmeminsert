create view [dbo].[TestAutoIncrementMemView]
with schemabinding
as
select id, SomeData, AppKey, ThreadId, CreateAt from dbo.TestAutoIncrementInMem with(snapshot);
