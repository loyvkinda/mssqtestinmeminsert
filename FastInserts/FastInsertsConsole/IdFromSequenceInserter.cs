using Microsoft.Data.SqlClient;
using System.Threading;

namespace FastInsertsConsole;

internal class IdFromSequenceInserter : IInserter
{
    private const int _rangeSize = 2_000;
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;
    private SqlCommand? _commandNextIdValue;
    private (long NextValue, long RemaningingCount)? _idSequence;


    public IdFromSequenceInserter(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public async Task PrepareAsync()
    {
        await EnsureConnectionOpenedAsync();

        //await using var dropCommand = _sqlConnection.CreateCommand();
        //dropCommand.CommandText = """
        //    DROP TABLE IF EXISTS [dbo].[TestAutoIncrement]
        //    DROP SEQUENCE IF EXISTS [dbo].[TestSequence]
        //    """;
        //await dropCommand.ExecuteNonQueryAsync();

        await using var createCommand = _sqlConnection.CreateCommand();
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
            Id bigint not null,
            SomeData nvarchar(100) not null,
            AppKey int not null,
            ThreadId int not null,
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
            _insertCommand.CommandText = "set nocount on; insert into dbo.TestAutoIncrementSeq (Id, SomeData, AppKey, ThreadId) VALUES (@Id, @SomeData, @AppKey, @ThreadId)";
            _insertCommand.Parameters.Add(new SqlParameter("@SomeData", System.Data.SqlDbType.NVarChar));
            _insertCommand.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.BigInt));
            _insertCommand.Parameters.Add(new SqlParameter("@AppKey", System.Data.SqlDbType.Int));
            _insertCommand.Parameters.Add(new SqlParameter("@ThreadId", System.Data.SqlDbType.Int)); 
            _insertCommand.CommandTimeout = 300;        }

        _insertCommand.Parameters["@Id"].Value = await GetNextIdAsync();
        _insertCommand.Parameters["@SomeData"].Value = Helpers.GenerateRandomString(20, 70);
        _insertCommand.Parameters["@AppKey"].Value = Helpers.GetTimeMsSinceMidnight();
        _insertCommand.Parameters["@ThreadId"].Value = Environment.CurrentManagedThreadId; ; 
        await _insertCommand.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _sqlConnection.Dispose();
    }

    private async Task<long> GetNextIdAsync()
    {
        if (_idSequence == null || _idSequence.Value.RemaningingCount == 0)
        {
            if (_commandNextIdValue == null)
            {
                _commandNextIdValue = new SqlCommand();
                _commandNextIdValue.CommandText = @$"
DECLARE @range_first_value_output sql_variant  ;  
 
EXEC sys.sp_sequence_get_range  
@sequence_name = N'dbo.TestSequence'  
, @range_size = {_rangeSize}  
, @range_first_value = @range_first_value_output OUTPUT ;  
 
SELECT CONVERT(bigint, @range_first_value_output) AS FirstNumber; ";
                _commandNextIdValue.Connection = _sqlConnection;
            }
            _idSequence = ((long)(await _commandNextIdValue.ExecuteScalarAsync())!, _rangeSize);
        }
        var toReturn = _idSequence!.Value.NextValue;
        _idSequence = (toReturn + 1, _idSequence.Value.RemaningingCount - 1);
        return toReturn;
    }

    private async Task EnsureConnectionOpenedAsync()
    {
        if (_sqlConnection.State != System.Data.ConnectionState.Open)
            await _sqlConnection.OpenAsync();
    }

}
