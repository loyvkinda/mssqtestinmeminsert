using Microsoft.Data.SqlClient;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualBasic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace FastInsertsConsole;

public static class DbHelper
{
    /// <summary>
    /// Информация о сервере
    /// </summary>
    /// <param name="connectionString">Строка соединения</param>
    /// <returns></returns>
    public static List<KeyValuePair<string, string>> GetServerinfo(string connectionString)
    {
        List<KeyValuePair<string, string>> col = new List<KeyValuePair<string, string>>();

        using var _sqlConnection = new SqlConnection(connectionString);
        if(_sqlConnection.State != System.Data.ConnectionState.Open)
            _sqlConnection.Open();
        using SqlCommand cmd = _sqlConnection.CreateCommand();

       
        cmd.CommandText = SqlResource("FastInsertsConsole.sql.custom.getserverinfo.sql").Result;
        /*
        cmd.CommandText = """
            select 'SERVERNAME' as name, @@SERVERNAME as value
            union all
            select name, cast(value_in_use as nvarchar(15)) as value
            from sys.configurations
            where name='max server memory (MB)'
            union all
            select name, cast(value_in_use as nvarchar(15)) as value
            from sys.configurations
            where description LIKE '%max%%parallelism%'
            union all
            select name, cast(value as nvarchar(15)) 
            from sys.database_scoped_configurations
            where name in ('MAXDOP', 'IDENTITY_CACHE')
            union all
            select 'snapshot_isolation_state_desc' as optionname, snapshot_isolation_state_desc as val
            from sys.databases where database_id=db_id()
            union all
            select 'recovery_model_desc', recovery_model_desc 
            from sys.databases where database_id=db_id()
            union all
            select 'is_memory_optimized_elevate_to_snapshot_on',
                cast(is_memory_optimized_elevate_to_snapshot_on as nvarchar(2)) 
            from sys.databases where database_id=db_id()
            union all
            select 'is_read_committed_snapshot_on', 
                cast(is_read_committed_snapshot_on as nvarchar(2))
            from sys.databases where database_id=db_id()
            """;
        */
        using var reader = cmd.ExecuteReader();
        while(reader.Read())
        {
            col.Add(new KeyValuePair<string, string>(reader.GetString(0), reader.GetString(1)));
        }
        reader.Close();
        return col;
    }
    /// <summary>
    /// Проверка соединения с сервером
    /// </summary>
    /// <param name="connectionString">Строка соединения</param>
    /// <returns></returns>
    public static bool CanConnect(string connectionString)
    {
        using SqlConnection cnn = new SqlConnection(connectionString);
        try
        {
            cnn.Open();
            cnn.Close();
            return true;
        }
        catch (Exception ex) { 
            return false;
        }            
    }
    /// <summary>
    /// Проверка принадлежности группе sysadmin
    /// </summary>
    /// <param name="connectionString">Строка соединения</param>
    /// <returns></returns>
    public static bool IsSysadmin(string connectionString)
    {
        using var _sqlConnection = new SqlConnection(connectionString);
        if (_sqlConnection.State != System.Data.ConnectionState.Open)
            _sqlConnection.Open();
        using SqlCommand cmd = _sqlConnection.CreateCommand();
        cmd.CommandText = """
            declare @rc bit=0;
            if( serverproperty('EngineEdition') < 8 )   
                select @rc = isnull(is_srvrolemember('sysadmin'), 0);
            select @rc;
            """;
        var val = cmd.ExecuteScalar();
        return (bool)val;
    }
    /// <summary>
    /// Выполнение sql скрипта 
    /// </summary>
    /// <param name="sqlConnection"></param>
    /// <param name="sqlBatch"></param>
    public static void ExecuteSqlScript(this SqlConnection sqlConnection, string sqlBatch)
    {
        string BatchTerminator = "GO";
        // Handle backslash utility statement (see http://technet.microsoft.com/en-us/library/dd207007.aspx)
        sqlBatch = Regex.Replace(sqlBatch, @"\\(\r\n|\r|\n)", string.Empty);

        // Handle batch splitting utility statement (see http://technet.microsoft.com/en-us/library/ms188037.aspx)
        var batches = Regex.Split(
            sqlBatch,
            string.Format(CultureInfo.InvariantCulture, @"^\s*({0}[ \t]+[0-9]+|{0})(?:\s+|$)", BatchTerminator),
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        for (int i = 0; i < batches.Length; ++i)
        {
            // Skip batches that merely contain the batch terminator
            if (batches[i].StartsWith(BatchTerminator, StringComparison.OrdinalIgnoreCase) ||
                (i == batches.Length - 1 && string.IsNullOrWhiteSpace(batches[i])))
            {
                continue;
            }

            // Include batch terminator if the next element is a batch terminator
            if (batches.Length > i + 1 &&
                batches[i + 1].StartsWith(BatchTerminator, StringComparison.OrdinalIgnoreCase))
            {
                int repeatCount = 1;

                // Handle count parameter on the batch splitting utility statement
                if (!string.Equals(batches[i + 1], BatchTerminator, StringComparison.OrdinalIgnoreCase))
                {
                    repeatCount = int.Parse(Regex.Match(batches[i + 1], @"([0-9]+)").Value, CultureInfo.InvariantCulture);
                }

                for (int j = 0; j < repeatCount; ++j)
                {
                    var command = sqlConnection.CreateCommand();
                    command.CommandText = batches[i];
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                var command = sqlConnection.CreateCommand();
                command.CommandText = batches[i];
                command.ExecuteNonQuery();
            }
        }
    }

    
    public static async Task<string> SqlResource(string sqlResource, Assembly resourceAssembly = null, bool suppressTransaction = false, object anonymousArguments = null)
    {
        if(string.IsNullOrEmpty(sqlResource))
            throw new ArgumentNullException(nameof(sqlResource));

        resourceAssembly = resourceAssembly ?? Assembly.GetExecutingAssembly();//Assembly.GetCallingAssembly();

        if (!resourceAssembly.GetManifestResourceNames().Contains(sqlResource))
        {
            throw new ArgumentException("Not found resource by name");
        }

        using (var reader = new StreamReader(resourceAssembly.GetManifestResourceStream(sqlResource), Encoding.UTF8))
        {
            var query = await reader.ReadToEndAsync();
            return query;
        }
    }
}
