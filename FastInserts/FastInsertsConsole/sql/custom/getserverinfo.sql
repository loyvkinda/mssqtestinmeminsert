﻿select 'SERVERNAME' as name, @@SERVERNAME as value
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