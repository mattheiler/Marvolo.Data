using System.Data.Entity;
using Marvolo.Data.Extensions;

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
            var builder = new DbSyncBuilder(source, target);

            source.ChangeTracker.DetectChanges(); // force change detection before evaluating

            var entries = source.GetObjectContext().ObjectStateManager.GetObjectStateEntries(state);

            foreach (var entry in entries)
                builder.Add(entry);

            return builder.CreateDbSync();
        }
    }
}