using Microsoft.Data.SqlClient;

namespace FastInsertsConsole;

internal class IdFromSequenceNextInserter : IInserter
{
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;
    private int _workerCount = 0;


    public IdFromSequenceNextInserter(string connectionString, int workerCount)
    {
        _sqlConnection = new SqlConnection(connectionString);
        _workerCount = workerCount;
    }

    public async Task PrepareAsync()
    {
        await EnsureConnectionOpenedAsync();

        using var createCommand = _sqlConnection.CreateCommand();
        createCommand.CommandText = """
            if not exists (select * from sys.sequences where name = N'TestSequence' and schema_id = schema_id(N'dbo'))
            begin
            CREATE SEQUENCE [dbo].[TestSequence] 
                AS [bigint]
                START WITH -9223372036854775808
                INCREMENT BY 1
                MINVALUE -9223372036854775808
                MAXVALUE 9223372036854775807
                CACHE;
            end

            if object_id('dbo.TestAutoIncrementSeq') is null
            create table dbo.TestAutoIncrementSeq (
            Id bigint not null default (next value for dbo.TestSequence),
            SomeData nvarchar(100) not null,
            AppKey int not null,
            ThreadId int not null,
            ThreadCount int not null,
            CreateAt datetime2 not null default(getutcdate()),
            constraint PK_TestAutoIncrementSeq primary key clustered(Id asc));
            """;
        await createCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync()
    {
        await EnsureConnectionOpenedAsync();
        if (_insertCommand == null)
        {
            _insertCommand = _sqlConnection.CreateCommand();
            _insertCommand.CommandText = "set nocount on; insert into dbo.TestAutoIncrementSeq (SomeData, AppKey, ThreadId, ThreadCount) VALUES (@SomeData, @AppKey, @ThreadId, @ThreadCount)";
            _insertCommand.Parameters.Add(new SqlParameter("@SomeData", System.Data.SqlDbType.NVarChar));
            _insertCommand.Parameters.Add(new SqlParameter("@AppKey", System.Data.SqlDbType.Int));
            _insertCommand.Parameters.Add(new SqlParameter("@ThreadId", System.Data.SqlDbType.Int));
            _insertCommand.Parameters.Add(new SqlParameter("@ThreadCount", System.Data.SqlDbType.Int));
            _insertCommand.CommandTimeout = 300;
        }

        _insertCommand.Parameters["@SomeData"].Value = Helpers.GenerateRandomString(20, 70, "siqenceserver");
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
