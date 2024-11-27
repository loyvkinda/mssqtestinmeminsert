using Microsoft.Data.SqlClient;

namespace FastInsertsConsole;


internal class InMemoryViewInserter : IInserter
{
    private SqlConnection _sqlConnection;
    private SqlCommand? _insertCommand;


    public InMemoryViewInserter(string connectionString)
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
        //    drop table if exists dbo.TestAutoIncrementInMem;
        //    drop sequence if exists dbo.TestSequenceInMem;
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
                CACHE 2000; 
            end

            if object_id('dbo.TestAutoIncrementInMem') is null
            create table dbo.TestAutoIncrementInMem
            (
            	Id bigint not null, 
            	SomeData nvarchar(100)
               --CONSTRAINT PK_TestAutoIncrementInMem primary key NONCLUSTERED (Id),
                CONSTRAINT PK_TestAutoIncrementInMem primary key nonclustered hash (id) with(bucket_count=1024)
            ) with (memory_optimized = on, durability = schema_and_data);
            
            """;
        await createCommand.ExecuteNonQueryAsync();

        await using var createViewCommand = _sqlConnection.CreateCommand();
        createViewCommand.CommandText = """
            if not exists (select * from sys.views where object_id = object_id(N'[dbo].[TestAutoIncrementMemView]'))
            exec dbo.sp_executesql @statement = N'create view [dbo].[TestAutoIncrementMemView]
            with schemabinding
            as
            select id, SomeData from dbo.TestAutoIncrementInMem with(snapshot);
            '
            """;
        await createViewCommand.ExecuteNonQueryAsync();

        await using var createViewTrigCommand = _sqlConnection.CreateCommand();
        createViewTrigCommand.CommandText = """
            if not exists (select * from sys.triggers where object_id = object_id(N'[dbo].[TR_TestAutoIncrementMemV2View_Insert]'))
            exec dbo.sp_executesql @statement = N'create trigger [dbo].[TR_TestAutoIncrementMemV2View_Insert]
            on [dbo].[TestAutoIncrementMemView]
            instead of insert
            as
            set nocount on;
            insert into dbo.TestAutoIncrementInMem with(snapshot) (id, SomeData)
            select next value for dbo.TestSequenceInMem, SomeData from INSERTED;
            ' 
            """;
        await createViewTrigCommand.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync()
    {
        await EnsureConnectionOpenedAsync();
        if (_insertCommand == null)
        {
            _insertCommand = _sqlConnection.CreateCommand();
            _insertCommand.CommandText = "set nocount on; insert into [dbo].[TestAutoIncrementMemView] ([SomeData]) VALUES (@SomeData)";
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
        {
            //Console.WriteLine("open cnn");
            await _sqlConnection.OpenAsync();
        }
    }
}
