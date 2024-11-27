using Microsoft.Data.SqlClient;

namespace FastInsertsConsole;

/// <summary>
/// Вставка данных в in-memory с использованием Sequence 
/// на стороне приложения и использования native хранимой процедуры
/// </summary>
internal class InMemoryTableInserterNativesp : IInserter
{
    private const int _rangeSize = 2_000;
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;
    private SqlCommand? _commandNextIdValue;
    private (long NextValue, long RemaningingCount)? _idSequence;


    public InMemoryTableInserterNativesp(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public async Task PrepareAsync()
    {
        await EnsureConnectionOpenedAsync();
        // проверка наличия ImMemory группы в базе
        await using var preСheckCommand = _sqlConnection.CreateCommand();
        preСheckCommand.CommandText = """
            declare @rc bit=0;
            select @rc = (select 1 from sys.filegroups where type='FX');
            select isnull(@rc, 0) as inmemGroupExists;
            """;
        var resultCheck = (bool)await preСheckCommand.ExecuteScalarAsync();
        if (!resultCheck)
        {
            // текущая папка с базами данных
            string resDbName = "";
            string resFileName = "";
            string resFilePath = "";
            using var cmdGetFiles = _sqlConnection.CreateCommand();
            cmdGetFiles.CommandText = "select db_name() as dbname, name, physical_name from sys.database_files where data_space_id = 1;";
            var reader = cmdGetFiles.ExecuteReader(System.Data.CommandBehavior.SingleRow);
            while (reader.Read())
            {
                resDbName = reader.GetString(0);
                resFileName = reader.GetString(1);
                resFilePath = reader.GetString(2);
            }
            reader.Close();
            string basedir = System.IO.Path.GetDirectoryName(resFilePath);
            string inmemDir = System.IO.Path.Combine(basedir, $"{resFileName}Inmem");
            //if (!System.IO.Path.Exists(inmemDir))
            //    System.IO.Directory.CreateDirectory(inmemDir); 
            //
            using var cmdCreateFileGroup = _sqlConnection.CreateCommand();
            cmdCreateFileGroup.CommandText = $"alter database [{resDbName}] add filegroup [inmemdata] contains memory_optimized_data;";
            cmdCreateFileGroup.ExecuteNonQuery();
            cmdCreateFileGroup.CommandText = $"ALTER DATABASE [{resDbName}] ADD FILE ( NAME = N'{resFileName}_inmem', FILENAME = N'{inmemDir}' ) TO FILEGROUP [inmemdata];";
            cmdCreateFileGroup.ExecuteNonQuery();
        }

        //await using var dropCommand = _sqlConnection.CreateCommand();
        //dropCommand.CommandText = """
        //    drop view if exists dbo.TestAutoIncrementMemView;
        //    drop table if exists [dbo].[TestAutoIncrementInMem];
        //    drop sequence if exists [dbo].[TestSequenceInMem];
        //    """;
        //await dropCommand.ExecuteScalarAsync();

        await using var createCommand = _sqlConnection.CreateCommand();
        createCommand.CommandText = """
            if not exists (select * from sys.sequences where name = N'TestSequenceInMem' and schema_id = schema_id(N'dbo'))
            begin
            CREATE SEQUENCE [dbo].[TestSequenceInMem] 
                AS [bigint]
                START WITH -9223372036854775808
                INCREMENT BY 1
                MINVALUE -9223372036854775808
                MAXVALUE 9223372036854775807
                CACHE 2000
            end

            if object_id('dbo.TestAutoIncrementInMem') is null
            CREATE TABLE dbo.TestAutoIncrementInMem
            (
            	Id bigint NOT NULL, 
            	SomeData nvarchar(100)
               --CONSTRAINT PK_TestAutoIncrementInMem PRIMARY KEY NONCLUSTERED (Id),
                CONSTRAINT PK_TestAutoIncrementInMem PRIMARY KEY nonclustered hash (id) with(bucket_count=1024)
               
            ) WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA)
            
            """;
        await createCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync()
    {
        await EnsureConnectionOpenedAsync();
        if (_insertCommand == null)
        {
            _insertCommand = _sqlConnection.CreateCommand();
            _insertCommand.CommandText = "dbo.nativeTestAutoIncrementInMemInsertOne";
            _insertCommand.CommandType = System.Data.CommandType.StoredProcedure;
            _insertCommand.Parameters.Add(new SqlParameter("@SomeData", System.Data.SqlDbType.NVarChar));
            _insertCommand.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.BigInt));
            _insertCommand.CommandTimeout = 300;
        }

        _insertCommand.Parameters["@Id"].Value = await GetNextIdAsync();
        _insertCommand.Parameters["@SomeData"].Value = Helpers.GenerateRandomString(20, 100);
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
@sequence_name = N'dbo.TestSequenceInMem'  
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
        {
            await _sqlConnection.OpenAsync();
        }
    }
}

