using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace Marvolo.Data.Sync
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DbSyncFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public DbSync Create(DbContext source, DbContext target, EntityState state = EntityState.Added | EntityState.Deleted | EntityState.Modified)
        {
            var sourceContext = (source as IObjectContextAdapter).ObjectContext;
            var targetContext = (target as IObjectContextAdapter).ObjectContext;

            var builder = new DbSyncBuilder(sourceContext, targetContext);

            source.ChangeTracker.DetectChanges(); // force change detection before evaluating

            var entries = sourceContext.ObjectStateManager.GetObjectStateEntries(state);

            foreach (var entry in entries)
                builder.Add(entry);

            return builder.CreateDbSync();
        }
    }
}