if object_id('dbo.TestAutoIncrementInMemIdentity') is null
    create table dbo.TestAutoIncrementInMemIdentity (
    Id bigint not null identity(1, 1),
    SomeData nvarchar(100) not null,
    AppKey int not null,
    ThreadId int not null,
    CreateAt datetime2(7) not null default(getutcdate()),
    constraint PK_TestAutoIncrementInMemIdentity primary key nonclustered
    (Id asc))
    with (memory_optimized=on, durability=schema_and_data);