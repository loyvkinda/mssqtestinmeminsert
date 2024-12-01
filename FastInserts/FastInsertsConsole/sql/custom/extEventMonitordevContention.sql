--select database_id, name from sys.databases where name='fortests'
--go
create event session [MonitordevTestAutoIncrement Contention] on server 
add event sqlserver.latch_suspend_end(
    action(sqlserver.database_id)
    where ([sqlserver].[database_id]=(69)))
add target package0.ring_buffer
with (max_memory=4096 kb,event_retention_mode=allow_single_event_loss,max_dispatch_latency=30 seconds,max_event_size=0 kb,memory_partition_mode=none,track_causality=off,startup_state=off)
go
