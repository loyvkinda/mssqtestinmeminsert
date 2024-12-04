# Описание

.Net 9, sqlclient 5.2.2

Тест производительности вставки данных при использовании 
ключей наиболее популяпных в mssql: на основе identity, sequence, newid() и newsequentialid()  

## Запуск и варианты

База данных должна существовать на сервере! Лучше создайте просто пустую базу и не используюйте существующую (**тем более основную рабочую базу**)!

Настройка в файле appsettings.json

```
{
  "ConnectionString": "Server=localhost;Database=ForTests;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True; Max Pool Size=200;",
  "targetInsertCount": 1000000
}
```

`ConnectionString` - строка соединения с mssql

`targetInsertCount` - количество строк для вставки. Не имеет смысла устанавливать малое значение. Оптимальное от 10 000 000 и выше.

Выбор количества 

После запуска вы увидите информацию о сервере и варианты запуска.
Требуются права sysamin на уровне mssql - ограничение просто от 
глупого запуска на предмет "нагрузить сервер".

```
Информация о сервере и базе:
MAXDOP SERVER: 0
MAXDOP: 0
IDENTITY_CACHE: 1
SNAPSHOT_ISOLATION_STATE_DESC: OFF
RECOVERY_MODEL_DESC: FULL
IS_MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT_ON: 1
IS_READ_COMMITTED_SNAPSHOT_ON: 0
--------------------------------------------------
Выбираем inserter и количество потоков (thread) ([1,256]), например: "I 8":
    - I  - ид на основе Identity. Таблица dbo.TestAutoIncrementIdentity
    - I2 - ид на основе Identity + опция optimize_for_sequential_key=on. Таблица dbo.TestAutoIncrementIdentityOpt
    - G  - guid as id on client. Таблица dbo.TestAutoIncrementGuid
    - G2 - guid сгенерированный на сервере newid(). Таблица dbo.TestAutoIncrementGuidNewId
    - G3 - guid сгенерированный на сервере newsequentialid(). Таблица dbo.TestAutoIncrementGuidNewSequentialid
    - G7 - guid сгенерированный на клиенте используя Guid.CreateVersion7. Таблица dbo.TestAutoIncrementGuidV7
    - S  - использование sequence. Таблица dbo.TestAutoIncrementSeq
    - M  - in-memory таблица id из sequence. Таблица dbo.TestAutoIncrementInMem
    - M1 - in-memory таблица id из sequence использую native хранимую процедуру. Таблица dbo.TestAutoIncrementInMem
    - MV - in-memory таблица id из sequence втавка в представление (view) и instead of тригер. Таблица dbo.TestAutoIncrementInMem
    - MI - in-memory таблица (identity) direct insert. Таблица dbo.TestAutoIncrementInMemIdentity
    - MI2 - in-memory таблица (identity) insert use stored procedure. Таблица dbo.TestAutoIncrementInMemIdentity
```

Можно также запустить с указанием параметров, например: `FastInsertsConsole.exe i2 96`

## Использование

Для проверки скорости вставки данных. Тестировать на домашних ноутбуках и прочем несерверном железе - пустая трата времени. 

Результаты теста представляют интерес в разрезе физически разных серверов, т.к. в большинстве случаев результаты на одном и том же железе 
для разных вариантов будут практически идентичны в большистве случаев.

При тестировании данных для in-memory таблиц - очень желательно в дальнейшем удалить таблицу или вообще базу целиком! 

После теста данные из таблиц не удаляются, таблицы при повторном тестировании не очищаются.
Вы можете самостоятельно поиграть с настройками индексов после создания таблиц (т.е. после первого теста) для 
поиска оптимальных параметров индекса.

Тест c использоваем диапазона sequence (вариант S) - может показать прирост производительности, но 
стоит обратить внимание на размер кеширования самого sequence и на использование именно диапазона!

```sql
CREATE SEQUENCE [dbo].[TestSequence] 
AS [bigint]
START WITH -9223372036854775808
INCREMENT BY 1
MINVALUE -9223372036854775808
MAXVALUE 9223372036854775807
CACHE;
```

Для тестирования в различных вариантах кеширования

```sql
alter sequence [dbo].[TestSequence]
 no cache 
```

или

```sql
alter sequence [dbo].[TestSequence]
 cache  2000 
```

## Данные

Таблицы для тестов имееют простую структуру

``` sql
create table dbo.TestAutoIncrementIdentity (
    Id bigint identity(1, 1) not null,
    SomeData nvarchar(100) not null,
    AppKey int not null,
    ThreadId int not null,
    ThreadCount int not null,
    CreateAt datetime2 default(getutcdate())
    constraint PK_TestAutoIncrementIdentity primary key clustered(Id asc));
```

И могут отличатся типом данных для для ключегового поля `Id` и настройками самого индекса.

`AppKey` - метка времени запуска программы

`ThreadId` - идентификатор потока программы

`ThreadCount` - количество потоков выбранное при тектировании

`CreateAt` - дата создания записи

## Диагностика и устранение конфликтов кратковременных блокировок

Документация от MS (
[ангийский]([https://learn.microsoft.com/ru-ru/sql/relational-databases/diagnose-resolve-latch-contention?view=sql-server-ver16)
[русский](https://learn.microsoft.com/en-us/sql/relational-databases/diagnose-resolve-latch-contention?view=sql-server-ver16)
)

Настрраиваем ExtEvent из файла `extEventMonitordevContention.sql` указав там правильный ид базы.

И проводим анализ mode=SH и mode=EX
