using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.Functions.Timer;

public class UpdateDatabaseStatistics
{
    private readonly SteamDbContext _db;

    public UpdateDatabaseStatistics(SteamDbContext db)
    {
        _db = db;
    }

    [Function("Update-Database-Statistics")]
    public async Task Run([TimerTrigger("0 0 0 * * Sun")] /* every week on sunday at midnight */ TimerInfo timerInfo, FunctionContext context)
    {
        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            _db.Database.SetCommandTimeout(600); // 10mins

            await _db.Database.ExecuteSqlInterpolatedAsync(@$"
                DECLARE @update_stats_cmd NVARCHAR(1024)
                DECLARE update_stats_cursor CURSOR FOR (
	                SELECT 
		                ('UPDATE STATISTICS ' + OBJECT_SCHEMA_NAME(stat.object_id) + '.' + OBJECT_NAME(stat.object_id) + ' ' + name + ';') AS command
	                FROM 
		                sys.stats AS stat    
		                CROSS APPLY sys.dm_db_stats_properties(stat.object_id, stat.stats_id) AS sp   
	                WHERE 
		                sp.last_updated < (GetDate() - 7) -- hasn't been updated in over a week
		                OR (sp.modification_counter > (sp.rows * .10)) -- more than 10% of the table rows have been modified
                )

                OPEN update_stats_cursor  
                FETCH NEXT FROM update_stats_cursor INTO @update_stats_cmd
 
                WHILE @@FETCH_STATUS = 0  
                BEGIN  
                  EXEC sp_executesql @update_stats_cmd
                  FETCH NEXT FROM update_stats_cursor INTO @update_stats_cmd 
                END

                CLOSE update_stats_cursor  
                DEALLOCATE update_stats_cursor
            ");

            await transaction.CommitAsync();
        }
    }
}
