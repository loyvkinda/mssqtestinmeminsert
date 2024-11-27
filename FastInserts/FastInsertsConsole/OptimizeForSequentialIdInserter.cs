using Microsoft.Data.SqlClient;

namespace FastInsertsConsole;

internal class OptimizeForSequentialIdInserter : IInserter
{
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;

    public OptimizeForSequentialIdInserter(string connectionString)
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
            if object_id('dbo.TestAutoIncrement') is null
            create table [dbo].[TestAutoIncrement](
            	[Id] [bigint] identity(1,1) not null,
            	[SomeData] [nvarchar](100) not null,
            	constraint [PK_TestAutoIncrement] primary key clustered ([Id] ASC)
                    with(optimize_for_sequential_key = on)
            )
            """;
        await createCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync()
    {
        await EnsureConnectionOpenedAsync();
        if (_insertCommand == null)
        {
            _insertCommand = _sqlConnection.CreateCommand();
            _insertCommand.CommandText = "set nocount on; insert into [dbo].[TestAutoIncrement] ([SomeData]) VALUES (@SomeData)";
            _insertCommand.Parameters.Add(new SqlParameter("@SomeData", System.Data.SqlDbType.NVarChar));
            _insertCommand.CommandTimeout = 300;
        }

        _insertCommand.Parameters["@SomeData"].Value = Helpers.GenerateRandomString(20, 100);
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
