 CREATE TABLE dbo.TestAutoIncrementInMem
 (
 Id bigint NOT NULL, 
 SomeData nvarchar(100) not null,
 AppKey int not null,
 ThreadId int not null,
 ThreadCount int not null,
 CreateAt datetime2 not null default(getutcdate()),
    --CONSTRAINT PK_TestAutoIncrementInMem PRIMARY KEY NONCLUSTERED (Id),
     CONSTRAINT PK_TestAutoIncrementInMem PRIMARY KEY nonclustered hash (id) with(bucket_count=1024)
    
 ) WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA)
 