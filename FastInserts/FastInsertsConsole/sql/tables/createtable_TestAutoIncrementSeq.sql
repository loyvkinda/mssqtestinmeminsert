if object_id('dbo.TestAutoIncrementSeq') is null
    create table dbo.TestAutoIncrementSeq (
    Id bigint not null,
    SomeData nvarchar(100) not null,
    AppKey int not null,
    ThreadId int not null,
    CreateAt datetime2 not null default(getutcdate()),
    constraint PK_TestAutoIncrementSeq primary key clustered(Id asc));
