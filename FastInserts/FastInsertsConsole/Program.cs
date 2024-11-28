using FastInsertsConsole;

int targetInsertCount = 1_000_00;//2_000_000;
string connectionString = "Server=localhost;Database=ForTests;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True; Max Pool Size=200;";
// фикскируем время старта, используется для группировки в таблицах
Helpers.GetTimeMsSinceMidnight();

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
    Elapsed time in seconds: {(int)(completedAt - startedAt).TotalSeconds}
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

(int WorkerCount, Func<IInserter> Inserter)? AskForSettings(string connectionString)
{
    Console.WriteLine("""
    Select inserter and thread count([1,256]), ex. "B 256":
        - B - autoicnremented id
        - O - autoicnremented id + optimize for sequential identity
        - G - guid as id on client
        - G7 - guid as id on client using Guid.CreateVersion7
        - G2 - guid as id on server newid()
        - G3 - guid as id on server newsequentialid()
        - S - id from sequence
        - M - inmemory table id from sequence
        - M1 - inmemory table id from sequence use stored procedure
        - MV - inmemory table id from sequence use view
        - MI - inmemory table (with identity) direct insert
        - MI2 - inmemory table (with identity) insert use stored procedure
    """);

    var inputParts = (Console.ReadLine() ?? string.Empty).Trim().Split(' ');
    if (inputParts.Length != 2 || !int.TryParse(inputParts[1], out var workerCount) || workerCount <= 0 || workerCount > 256)
        return null;

    Func<IInserter>? creator = inputParts[0].ToUpperInvariant() switch
    {
        "B" => () => new BasicInserter(connectionString),
        "O" => () => new OptimizeForSequentialIdInserter(connectionString),
        "G" => () => new GuidIdInserter(connectionString),
        "G7" => () => new GuidIdInserterV7OnClient(connectionString),
        "G2" => () => new GuidIdInserterNewIdOnServer(connectionString),
        "G3" => () => new GuidIdInserterNewSequentialidOnServer(connectionString),
        "S" => () => new IdFromSequenceInserter(connectionString),
        "M" => () => new InMemoryTableInserter(connectionString),
        "M1" => () => new InMemoryTableInserterNativesp(connectionString),
        "MV" => () => new InMemoryViewInserter(connectionString),
        "MI" => () => new InMemoryTableInserterIdentity(connectionString),
        "MI2" => () => new InMemoryTableInserterIdentityNativeSp(connectionString),
        _ => null
    };
    if (creator == null)
        return null;
    return (workerCount, creator);
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