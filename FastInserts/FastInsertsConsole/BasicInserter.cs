using Microsoft.Data.SqlClient;

namespace FastInsertsConsole;

internal class BasicInserter : IInserter
{
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;

    public BasicInserter(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public async Task PrepareAsync()
    {
        await EnsureConnectionOpenedAsync();

        //await using var dropCommand = _sqlConnection.CreateCommand();
        //dropCommand.CommandText = "DROP TABLE IF EXISTS [dbo].[TestAutoIncrement]";
        //await dropCommand.ExecuteNonQueryAsync();

        await using var createCommand = _sqlConnection.CreateCommand();
        createCommand.CommandText = """
            if object_id('dbo.TestAutoIncrementIdentity') is null
            create table dbo.TestAutoIncrementIdentity (
            Id bigint identity(1, 1) not null,
            SomeData nvarchar(100) not null,
            AppKey int not null,
            CreateAt datetime2 not null default(getutcdate()),
            constraint PK_TestAutoIncrementIdentity primary key clustered(Id asc));
            """;
        await createCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync()
    {
        await EnsureConnectionOpenedAsync();
        if (_insertCommand == null)
        {
            _insertCommand = _sqlConnection.CreateCommand();
            _insertCommand.CommandText = "set nocount on; insert into [dbo].[TestAutoIncrementIdentity] ([SomeData], AppKey) VALUES (@SomeData, @AppKey)";
            _insertCommand.Parameters.Add(new SqlParameter("@SomeData", System.Data.SqlDbType.NVarChar));
            _insertCommand.Parameters.Add(new SqlParameter("@AppKey", System.Data.SqlDbType.Int));
            
            _insertCommand.CommandTimeout = 300;
        }

        _insertCommand.Parameters["@SomeData"].Value = Helpers.GenerateRandomString(20, 70);
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
