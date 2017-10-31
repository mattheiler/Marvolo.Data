namespace Marvolo.Data.Sync
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DbSyncReferenceEntry
    {
        internal DbSyncReferenceEntry(string name)
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
        public object OriginalValue { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public object CurrentValue { get; internal set; }
    }
}