using Microsoft.Data.SqlClient;
using System.Threading;

namespace FastInsertsConsole;

internal class GuidIdInserterNewSequentialidOnServer : IInserter
{
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;
    private int _workerCount = 0;

    public GuidIdInserterNewSequentialidOnServer(string connectionString, int workerCount)
    {
        _sqlConnection = new SqlConnection(connectionString);
        _workerCount = workerCount;
    }

    public async Task PrepareAsync()
    {
        await EnsureConnectionOpenedAsync();

        using var createCommand = _sqlConnection.CreateCommand();
        createCommand.CommandText = """
            if object_id('dbo.TestAutoIncrementGuidNewSequentialid') is null begin
                create table dbo.TestAutoIncrementGuidNewSequentialid (
                Id uniqueidentifier not null default(newsequentialid()),
                SomeData nvarchar(100) not null,
                AppKey int not null,
                ThreadId int not null,
                ThreadCount int not null,
                CreateAt datetime2 not null default(getutcdate()) 
                constraint PK_TestAutoIncrementGuidNewSequentialid primary key clustered (Id asc) with(optimize_for_sequential_key=on));
            end;
            """;
        await createCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync()
    {
        await EnsureConnectionOpenedAsync();
        if (_insertCommand == null)
        {
            _insertCommand = _sqlConnection.CreateCommand();
            _insertCommand.CommandText = "set nocount on; insert into dbo.TestAutoIncrementGuidNewSequentialid (SomeData, AppKey, ThreadId, ThreadCount) VALUES (@SomeData, @AppKey, @ThreadId, @ThreadCount)";
            _insertCommand.Parameters.Add(new SqlParameter("@SomeData", System.Data.SqlDbType.NVarChar));
            _insertCommand.Parameters.Add(new SqlParameter("@AppKey", System.Data.SqlDbType.Int));
            _insertCommand.Parameters.Add(new SqlParameter("@ThreadId", System.Data.SqlDbType.Int));
            _insertCommand.Parameters.Add(new SqlParameter("@ThreadCount", System.Data.SqlDbType.Int));
            _insertCommand.CommandTimeout = 300;
        }

        _insertCommand.Parameters["@SomeData"].Value = Helpers.GenerateRandomString(20, 70);
        _insertCommand.Parameters["@AppKey"].Value = Helpers.GetTimeMsSinceMidnight();
        _insertCommand.Parameters["@ThreadId"].Value = Environment.CurrentManagedThreadId;
        _insertCommand.Parameters["@ThreadCount"].Value = _workerCount;
        await _insertCommand.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _sqlConnection.Dispose();
    }

    private async Task EnsureConnectionOpenedAsync()
    {
        if (_sqlConnection.State != System.Data.ConnectionState.Open)
            await _sqlConnection.OpenAsync();
    }

}
