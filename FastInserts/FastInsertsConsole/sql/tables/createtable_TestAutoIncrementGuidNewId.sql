if object_id('dbo.TestAutoIncrementGuidNewId') is null 
begin
    create table dbo.TestAutoIncrementGuidNewId (
    Id uniqueidentifier not null default(newid()),
    SomeData nvarchar(100) not null,
    AppKey int not null,
    ThreadId int not null,
    ThreadCount int not null,
    CreateAt datetime2 default(getutcdate()) 
    constraint PK_TestAutoIncrementGuidNewId primary key clustered (Id asc));
end;
