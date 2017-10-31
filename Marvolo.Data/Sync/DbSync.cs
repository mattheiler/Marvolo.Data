using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Threading.Tasks;
using Marvolo.Data.Extensions;

namespace Marvolo.Data.Sync
{
    /// <summary>
    /// </summary>
    public sealed class DbSync
    {
        private readonly HashSet<DbSyncEntry> _added = new HashSet<DbSyncEntry>();

        private readonly DbContext _context;

        private readonly HashSet<DbSyncEntry> _deleted = new HashSet<DbSyncEntry>();

        private readonly HashSet<DbSyncEntry> _modified = new HashSet<DbSyncEntry>();

        private readonly HashSet<DbSyncEntry> _unchanged = new HashSet<DbSyncEntry>();

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entries"></param>
        internal DbSync(DbContext context, IEnumerable<DbSyncEntry> entries)
        {
            _context = context;

            foreach (var entry in entries)
                switch (entry.SourceState)
                {
                    case EntityState.Added:
                        _added.Add(entry);
                        break;
                    case EntityState.Deleted:
                        _deleted.Add(entry);
                        break;
                    case EntityState.Modified:
                        _modified.Add(entry);
                        break;
                    case EntityState.Unchanged:
                        _unchanged.Add(entry);
                        break;
                }
        }

        /// <summary>
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public IEnumerable<DbSyncEntry> GetEntries(EntityState state = EntityState.Added | EntityState.Deleted | EntityState.Modified | EntityState.Unchanged)
        {
            var entries = Enumerable.Empty<DbSyncEntry>();

            if (state.HasFlag(EntityState.Added))
                entries = entries.Concat(_added);

            if (state.HasFlag(EntityState.Deleted))
                entries = entries.Concat(_deleted);

            if (state.HasFlag(EntityState.Modified))
                entries = entries.Concat(_modified);

            if (state.HasFlag(EntityState.Unchanged))
                entries = entries.Concat(_unchanged);

            return entries;
        }

        /// <summary>
        /// </summary>
        public void Sync()
        {
            var entries = GetEntries(EntityState.Added | EntityState.Modified | EntityState.Unchanged).Where(entry => entry.TargetState == EntityState.Detached);

            // attach those target entity that are marked as detached

            foreach (var entry in entries)
                _context.GetObjectContext().AttachTo(entry.TargetEntityKey.EntitySetName, entry.TargetEntity);
        }

        /// <summary>
        /// </summary>
        /// <param name="state"></param>
        public void Refresh(EntityState state)
        {
            _context.GetObjectContext().Refresh(RefreshMode.StoreWins, GetEntries(state).Where(CanRefresh).Select(entry => entry.TargetEntity));
        }

        /// <summary>
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public Task RefreshAsync(EntityState state)
        {
            return _context.GetObjectContext().RefreshAsync(RefreshMode.StoreWins, GetEntries(state).Where(CanRefresh).Select(entry => entry.TargetEntity));
        }

        /// <summary>
        /// </summary>
        public void Flush()
        {
            foreach (var entry in GetEntries())
                entry.TargetState = _context.GetObjectContext().ObjectStateManager.TryGetObjectStateEntry(entry.TargetEntityKey, out var current) ? current.State : EntityState.Detached;

            _context.GetObjectContext().DetectChanges(); // should this be done before? ...in-between?
        }

        private static bool CanRefresh(DbSyncEntry entry)
        {
            switch (entry.TargetState)
            {
                case EntityState.Detached:
                    return entry.SourceState == EntityState.Added;
                default:
                    return true;
            }
        }
    }
}