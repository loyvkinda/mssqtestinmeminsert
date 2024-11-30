using FastInsertsConsole;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
                  .AddJsonFile($"appsettings.json", true, true);

var config = builder.Build();

int targetInsertCount = 500_000;
if (config["targetInsertCount"] != null)
    int.TryParse(config["targetInsertCount"], out targetInsertCount);
// "Server=localhost;Database=ForTests;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True; Max Pool Size=200;";
string connectionString = config["ConnectionString"]; 


// фикскируем время старта, используется для группировки в таблицах
Helpers.GetTimeMsSinceMidnight();

if(! DbHelper.CanConnect(connectionString))
{
    Console.WriteLine("Нет соединения с сервером!!! Исправте строку подключения в файле appsettings.json");
    return;
}
if(!DbHelper.IsSysadmin(connectionString))
{
    Console.WriteLine("Вы не сисадмин на сервере!!! Исправте строку подключения в файле appsettings.json");
    return;
}

// общая информация о сервере
var srvInfo = DbHelper.GetServerinfo(connectionString);
Console.WriteLine("Информация о сервере и базе:");
foreach (var item in srvInfo)
{
    Console.WriteLine($"{item.Key.ToUpper()}: {item.Value}");
    
}
Console.WriteLine(string.Concat(Enumerable.Repeat("-", 50)));

var settings = AskForSettings(connectionString);
if (!settings.HasValue)
{
    Console.WriteLine("Invalid input");
    Console.ReadLine();
    return;
}

// Prepare DB
using (var prepareInserter = settings.Value.Inserter())
    await prepareInserter.PrepareAsync();

// Do inserts
List<Task> workers = new();
var startedAt = DateTime.UtcNow;
var remainingCounter = new RemainingCounter(targetInsertCount);
for (int i = 0; i < settings.Value.WorkerCount; i++)
    workers.Add(InsertAsync(settings.Value.Inserter, remainingCounter));

while (remainingCounter.Remaining > 0 )
{
    // исключаем первые 15000 обработанных данных
    if(remainingCounter.Remaining < targetInsertCount - 15000)
        Console.WriteLine($"{DateTime.UtcNow:dd.MM.yyyy HH:mm:ss} Inserted: {targetInsertCount - remainingCounter.Remaining}, per thread: {(int)(targetInsertCount / (DateTime.UtcNow - startedAt).TotalSeconds / settings.Value.WorkerCount)}");
    await Task.Delay(1000);
}

await Task.WhenAll(workers);
var completedAt = DateTime.UtcNow;

// Print summary
Console.WriteLine($"""    
    {settings.Value.SelectedVariant} {settings.Value.WorkerCount} Elapsed time in seconds: {(int)(completedAt - startedAt).TotalSeconds}
    Inserts per second: {targetInsertCount / (completedAt - startedAt).TotalSeconds}
    Inserts per second per thread: {targetInsertCount / (completedAt - startedAt).TotalSeconds / settings.Value.WorkerCount}
    """);
Console.ReadLine();


async Task InsertAsync(Func<IInserter> inserterCreator, RemainingCounter counter)
{
    using var inserter = inserterCreator();
    while (counter.TryDecrement())
    {
        try
        {
            await inserter.InsertAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}

(int WorkerCount, string SelectedVariant,  Func<IInserter> Inserter)? AskForSettings(string connectionString)
{
    Console.WriteLine("""
    Выбираем inserter и количество потоков (thread) ([1,256]), например: "I 8":
        - I  - ид на основе Identity. Таблица dbo.TestAutoIncrementIdentity
        - I2 - ид на основе Identity + опция optimize_for_sequential_key=on. Таблица dbo.TestAutoIncrementIdentityOpt
        - G  - guid as id on client. Таблица dbo.TestAutoIncrementGuid
        - G2 - guid сгенерированный на сервере newid(). Таблица dbo.TestAutoIncrementGuidNewId
        - G3 - guid сгенерированный на сервере newsequentialid(). Таблица dbo.TestAutoIncrementGuidNewSequentialid
        - G7 - guid сгенерированный на клиенте используя Guid.CreateVersion7. Таблица dbo.TestAutoIncrementGuidV7
        - S  - использование sequence на основе диапазона (sp_sequence_get_range). Таблица dbo.TestAutoIncrementSeq
        - S1 - использование sequence (next value for). Таблица dbo.TestAutoIncrementSeq
        - M  - in-memory таблица id из sequence на основе диапазона (sp_sequence_get_range). Таблица dbo.TestAutoIncrementInMem
        - M1 - in-memory таблица id из sequence на основе диапазона (sp_sequence_get_range) использую native хранимую процедуру. Таблица dbo.TestAutoIncrementInMem
        - MV - in-memory таблица id из sequence (next value for) вcтавка в представление (view) и instead of тригер. Таблица dbo.TestAutoIncrementInMem
        - MI - in-memory таблица (identity) direct insert. Таблица dbo.TestAutoIncrementInMemIdentity
        - MI2 - in-memory таблица (identity) insert use stored procedure. Таблица dbo.TestAutoIncrementInMemIdentity
    """);

    var inputParts = args.Length > 0 ? args : (Console.ReadLine() ?? string.Empty).Trim().Split(' ');
    if (inputParts.Length != 2 || !int.TryParse(inputParts[1], out var workerCount) || workerCount <= 0 || workerCount > 256)
        return null;
    var selectedVariant = inputParts[0];
    Func<IInserter>? creator = inputParts[0].ToUpperInvariant() switch
    {
        "I" => () => new BasicInserter(connectionString, workerCount),
        "I2" => () => new OptimizeForSequentialIdInserter(connectionString, workerCount),
        "G" => () => new GuidIdInserter(connectionString, workerCount),
        "G7" => () => new GuidIdInserterV7OnClient(connectionString, workerCount),
        "G2" => () => new GuidIdInserterNewIdOnServer(connectionString, workerCount),
        "G3" => () => new GuidIdInserterNewSequentialidOnServer(connectionString, workerCount),
        "S" => () => new IdFromSequenceInserter(connectionString, workerCount),
        "S1" => () => new IdFromSequenceNextInserter(connectionString, workerCount),
        "M" => () => new InMemoryTableInserter(connectionString, workerCount),
        "M1" => () => new InMemoryTableInserterNativesp(connectionString, workerCount),
        "MV" => () => new InMemoryViewInserter(connectionString, workerCount),
        "MI" => () => new InMemoryTableInserterIdentity(connectionString, workerCount),
        "MI2" => () => new InMemoryTableInserterIdentityNativeSp(connectionString, workerCount),
        _ => null
    };
    if (creator == null)
        return null;
    return (workerCount, selectedVariant, creator);
}

class RemainingCounter
{
    private object _lock = new();
    private int _counter;

    public int Remaining => _counter;

    public RemainingCounter(int counter)
    {
        _counter = counter;
    }

    public bool TryDecrement()
    {
        lock (_lock)
        {
            if (_counter <= 0)
                return false;
            _counter--;
            return true;
        }
    }
}