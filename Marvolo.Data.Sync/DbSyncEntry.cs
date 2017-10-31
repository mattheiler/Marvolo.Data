using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Core;

namespace Marvolo.Data.Sync
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DbSyncEntry
    {
        internal DbSyncEntry(object targetEntity, EntityKey targetEntityKey)
        {
            TargetEntity = targetEntity;
            TargetEntityKey = targetEntityKey;
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityState SourceState { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public EntityState TargetState { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyDictionary<string, DbSyncCollectionEntry> Collections => new ReadOnlyDictionary<string, DbSyncCollectionEntry>(CollectionsInternal);

        internal IDictionary<string, DbSyncCollectionEntry> CollectionsInternal { get; } = new Dictionary<string, DbSyncCollectionEntry>();

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyDictionary<string, DbSyncPropertyEntry> Properties => new ReadOnlyDictionary<string, DbSyncPropertyEntry>(PropertiesInternal);

        internal IDictionary<string, DbSyncPropertyEntry> PropertiesInternal { get; } = new Dictionary<string, DbSyncPropertyEntry>();

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyDictionary<string, DbSyncReferenceEntry> References => new ReadOnlyDictionary<string, DbSyncReferenceEntry>(ReferencesInternal);

        internal IDictionary<string, DbSyncReferenceEntry> ReferencesInternal { get; } = new Dictionary<string, DbSyncReferenceEntry>();

        /// <summary>
        /// 
        /// </summary>
        public object TargetEntity { get; }

        /// <summary>
        /// 
        /// </summary>
        public EntityKey TargetEntityKey { get; }
    }
}