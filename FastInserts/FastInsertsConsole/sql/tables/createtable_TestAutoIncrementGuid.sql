if object_id('dbo.TestAutoIncrementGuid') is null
    create table dbo.TestAutoIncrementGuid (
    Id uniqueidentifier not null,
    SomeData nvarchar(100) not null,
    AppKey int not null,
    ThreadId int not null,
    ThreadCount int not null,
    CreateAt datetime2 default(getutcdate())
    constraint PK_TestAutoIncrementGuid primary key clustered(Id asc));
