create trigger [dbo].[TR_TestAutoIncrementMemV2View_Insert]
on [dbo].[TestAutoIncrementMemView]
instead of insert
as
set nocount on;
insert into dbo.TestAutoIncrementInMem with(snapshot) (id, SomeData, AppKey, ThreadId, ThreadCount)
select next value for dbo.TestSequenceInMem, SomeData, AppKey, ThreadId, ThreadCount from INSERTED;
