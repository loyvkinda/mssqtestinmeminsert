create or alter procedure dbo.nativeTestAutoIncrementInMemIdentityInsertOne
(@somedata nvarchar(100), @AppKey int, @ThreadId int)
	with native_compilation,
	     schemabinding,
	     execute as owner
as
begin atomic
	with (transaction isolation level = snapshot,
	      language = N'us_english')
	insert dbo.TestAutoIncrementInMemIdentity(SomeData, AppKey, ThreadId) 
    values (@somedata, @AppKey, @ThreadId);
end;
