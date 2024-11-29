if object_id('dbo.TestAutoIncrementIdentity') is null
    create table dbo.TestAutoIncrementIdentity (
    Id bigint identity(1, 1) not null,
    SomeData nvarchar(100) not null,
    AppKey int not null,
    ThreadId int not null,
    ThreadCount int not null,
    CreateAt datetime2 default(getutcdate())
    constraint PK_TestAutoIncrementIdentity primary key clustered(Id asc));
