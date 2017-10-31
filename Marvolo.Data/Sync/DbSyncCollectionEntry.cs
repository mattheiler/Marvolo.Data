using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Marvolo.Data.Sync
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DbSyncCollectionEntry
    {
        internal DbSyncCollectionEntry(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<object> OriginalValues => new ReadOnlyCollection<object>(OriginalValuesInternal);

        internal IList<object> OriginalValuesInternal { get; } = new List<object>();

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<object> CurrentValues => new ReadOnlyCollection<object>(CurrentValuesInternal);

        internal IList<object> CurrentValuesInternal { get; } = new List<object>();
    }
}