if object_id('dbo.TestAutoIncrementGuidV7') is null
begin
create table dbo.TestAutoIncrementGuidV7 (
Id uniqueidentifier not null,
SomeData nvarchar(100) not null,
AppKey int not null,
ThreadId int not null,
ThreadCount int not null,
CreateAt datetime2 not null default(getutcdate()),
constraint PK_TestAutoIncrementGuidV7 primary key clustered(Id asc)with(optimize_for_sequential_key=on));
end