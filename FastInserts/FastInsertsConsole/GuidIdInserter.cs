using Microsoft.Data.SqlClient;

namespace FastInsertsConsole;

internal class GuidIdInserter : IInserter
{
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;

    public GuidIdInserter(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public async Task PrepareAsync()
    {
        await EnsureConnectionOpenedAsync();

        await using var createCommand = _sqlConnection.CreateCommand();
        createCommand.CommandText = """
            if object_id('dbo.TestAutoIncrementGuid') is null
            create table dbo.TestAutoIncrementGuid (
            Id uniqueidentifier not null,
            SomeData nvarchar(100) not null,
            AppKey int not null,
            CreateAt datetime2 not null default(getutcdate())
            constraint PK_TestAutoIncrementGuid primary key clustered(Id asc));
            """;
        await createCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync()
    {
        await EnsureConnectionOpenedAsync();
        if (_insertCommand == null)
        {
            _insertCommand = _sqlConnection.CreateCommand();
            _insertCommand.CommandText = "set nocount on; insert into [dbo].[TestAutoIncrementGuid] (Id, [SomeData], AppKey) VALUES (@Id, @SomeData, @AppKey)";
            _insertCommand.Parameters.Add(new SqlParameter("@SomeData", System.Data.SqlDbType.NVarChar));
            _insertCommand.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier));
            _insertCommand.Parameters.Add(new SqlParameter("@AppKey", System.Data.SqlDbType.Int));
            
            _insertCommand.CommandTimeout = 300;
        }

        _insertCommand.Parameters["@SomeData"].Value = Helpers.GenerateRandomString(20, 70);
        _insertCommand.Parameters["@Id"].Value = Guid.NewGuid();
        _insertCommand.Parameters["@AppKey"].Value = Helpers.GetTimeMsSinceMidnight();
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
