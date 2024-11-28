using Microsoft.Data.SqlClient;
using System.Threading;

namespace FastInsertsConsole;

internal class GuidIdInserterV7OnClient : IInserter
{
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;

    public GuidIdInserterV7OnClient(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public async Task PrepareAsync()
    {
        await EnsureConnectionOpenedAsync();

        await using var createCommand = _sqlConnection.CreateCommand();
        createCommand.CommandText = """
            if object_id('dbo.TestAutoIncrementGuidV7') is null
            begin
            create table dbo.TestAutoIncrementGuidV7 (
            Id uniqueidentifier not null,
            SomeData nvarchar(100) not null,
            AppKey int not null,
            ThreadId int not null,
            CreateAt datetime2 not null default(getutcdate()),
            constraint PK_TestAutoIncrementGuidV7 primary key clustered(Id asc)with(optimize_for_sequential_key=on));
            end
            """;
        await createCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync()
    {
        await EnsureConnectionOpenedAsync();
        if (_insertCommand == null)
        {
            _insertCommand = _sqlConnection.CreateCommand();
            _insertCommand.CommandText = "set nocount on; insert into dbo.TestAutoIncrementGuidV7 (Id, SomeData, AppKey, ThreadId) VALUES (@Id, @SomeData, @AppKey, @ThreadId)";
            _insertCommand.Parameters.Add(new SqlParameter("@SomeData", System.Data.SqlDbType.NVarChar));
            _insertCommand.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier));
            _insertCommand.Parameters.Add(new SqlParameter("@AppKey", System.Data.SqlDbType.Int));
            _insertCommand.Parameters.Add(new SqlParameter("@ThreadId", System.Data.SqlDbType.Int));            
            _insertCommand.CommandTimeout = 300;
        }

        _insertCommand.Parameters["@SomeData"].Value = Helpers.GenerateRandomString(20, 70);
        _insertCommand.Parameters["@Id"].Value = Guid.CreateVersion7();
        _insertCommand.Parameters["@AppKey"].Value = Helpers.GetTimeMsSinceMidnight();
        _insertCommand.Parameters["@ThreadId"].Value = Environment.CurrentManagedThreadId;
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