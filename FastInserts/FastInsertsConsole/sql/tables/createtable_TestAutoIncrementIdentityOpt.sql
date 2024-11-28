if object_id('dbo.TestAutoIncrementIdentityOpt') is null
    create table dbo.TestAutoIncrementIdentityOpt (
    Id bigint identity(1, 1) not null,
    SomeData nvarchar(100) not null,
    AppKey int not null,
    ThreadId int not null,
    CreateAt datetime2 not null default(getutcdate()),
    constraint PK_TestAutoIncrementIdentityOpt primary key clustered(Id asc)with(optimize_for_sequential_key=on));