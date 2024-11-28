if object_id('dbo.TestAutoIncrementGuidNewSequentialid') is null begin
    create table dbo.TestAutoIncrementGuidNewSequentialid (
    Id uniqueidentifier not null default(newsequentialid()),
    SomeData nvarchar(100) not null,
    AppKey int not null,
    ThreadId int not null,
    CreateAt datetime2 not null default(getutcdate()) 
    constraint PK_TestAutoIncrementGuidNewSequentialid primary key clustered (Id asc) with(optimize_for_sequential_key=on));
end;
