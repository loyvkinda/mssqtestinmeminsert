create   procedure dbo.nativeTestAutoIncrementInMemInsertOne
(@id bigint, @SomeData nvarchar(100), @AppKey int, @ThreadId int)
	with native_compilation,
	     schemabinding,
	     execute as owner
as
begin atomic
	with (transaction isolation level = snapshot,
	      language = N'us_english')
	insert dbo.TestAutoIncrementInMem(Id, SomeData, AppKey, ThreadId) 
    values (@id, @SomeData, @AppKey, @ThreadId);
end;
