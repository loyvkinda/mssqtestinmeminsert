using Microsoft.Data.SqlClient;

namespace FastInsertsConsole;

/// <summary>
/// Вставка данных в in-memory с использованием identity 
/// на стороне сервера и использования native хранимой процедуры
/// </summary>
internal class InMemoryTableInserterIdentityNativeSp : IInserter
{
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;

    public InMemoryTableInserterIdentityNativeSp(string connectionString)
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


        await using var createCommand = _sqlConnection.CreateCommand();
        createCommand.CommandText = """
            if object_id('dbo.TestAutoIncrementInMemIdentity') is null
            create table dbo.TestAutoIncrementInMemIdentity (
            Id bigint not null identity(1, 1),
            SomeData nvarchar(100) collate Cyrillic_General_CI_AS null,
            AppKey int not null,
            CreateAt datetime2(7) not null default(getutcdate()),
            constraint PK_TestAutoIncrementInMemIdentity primary key nonclustered(Id asc))
            with (memory_optimized=on, durability=schema_and_data);
            
            """;
        await createCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync()
    {
        await EnsureConnectionOpenedAsync();
        if (_insertCommand == null)
        {
            _insertCommand = _sqlConnection.CreateCommand();
            _insertCommand.CommandText = "dbo.nativeTestAutoIncrementInMemIdentityInsertOne";
            _insertCommand.CommandType = System.Data.CommandType.StoredProcedure;
            _insertCommand.Parameters.Add(new SqlParameter("@SomeData", System.Data.SqlDbType.NVarChar));
            _insertCommand.Parameters.Add(new SqlParameter("@AppKey", System.Data.SqlDbType.Int));
            _insertCommand.CommandTimeout = 300;
        }

        _insertCommand.Parameters["@SomeData"].Value = Helpers.GenerateRandomString(20, 70, "spidentity ");
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

