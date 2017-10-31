using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Diagnostics.Contracts;
using System.Linq;
using Marvolo.Data.Extensions;

namespace Marvolo.Data.Sync
{
    /// <summary>
    /// </summary>
    public sealed class DbSyncBuilder
    {
        private readonly HashSet<ObjectStateEntry> _added = new HashSet<ObjectStateEntry>();

        private readonly HashSet<ObjectStateEntry> _deleted = new HashSet<ObjectStateEntry>();

        private readonly HashSet<ObjectStateEntry> _modified = new HashSet<ObjectStateEntry>();

        private readonly DbContext _source;

        private readonly DbContext _target;

        /// <summary>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public DbSyncBuilder(DbContext source, DbContext target)
        {
            _source = source;
            _target = target;
        }

        /// <summary>
        /// </summary>
        /// <param name="entry"></param>
        public void Add(ObjectStateEntry entry)
        {
            switch (entry.State)
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
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public DbSync CreateDbSync()
        {
            var cache = new Dictionary<EntityKey, DbSyncEntry>();
            var entries = GetEntries().OrderBy(entry => entry.IsRelationship); // take care of relationships first

            foreach (var entry in entries)
                if (entry.IsRelationship)
                    LoadRelationshipEntry(entry, cache);
                else
                    LoadEntityEntry(entry, cache);

            return new DbSync(_target, cache.Values);
        }

        private IEnumerable<ObjectStateEntry> GetEntries(EntityState state = EntityState.Added | EntityState.Deleted | EntityState.Modified)
        {
            var entries = Enumerable.Empty<ObjectStateEntry>();

            if (state.HasFlag(EntityState.Added))
                entries = entries.Concat(_added);

            if (state.HasFlag(EntityState.Deleted))
                entries = entries.Concat(_deleted);

            if (state.HasFlag(EntityState.Modified))
                entries = entries.Concat(_modified);

            return entries;
        }

        private DbSyncEntry CreateAddedDbSyncEntry(ObjectStateEntry sourceEntry)
        {
            Contract.Assert(sourceEntry.IsRelationship == false);
            Contract.Assert(sourceEntry.State == EntityState.Added);

            var sourceEntity = sourceEntry.Entity;
            var sourceEntityKey = sourceEntry.EntityKey;

            var targetObjectContext = _target.GetObjectContext();
            if (targetObjectContext.ObjectStateManager.TryGetObjectStateEntry(sourceEntityKey, out var targetEntry))
                return new DbSyncEntry(targetEntry.Entity, targetEntry.EntityKey)
                {
                    SourceState = EntityState.Added,
                    TargetState = targetEntry.State
                };

            // clone, or activate new instance. test this...

            var targetEntityKey = targetObjectContext.CreateEntityKey(sourceEntry.EntitySet.Name, sourceEntity);
            var targetEntity = sourceEntity is ICloneable cloneable ? cloneable.Clone() : Activator.CreateInstance(ObjectContext.GetObjectType(sourceEntity.GetType()));

            return new DbSyncEntry(targetEntity, targetEntityKey)
            {
                SourceState = EntityState.Added,
                TargetState = EntityState.Detached
            };
        }

        private void LoadRelationshipEntry(ObjectStateEntry context, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            switch (context.State)
            {
                case EntityState.Added:
                    LoadAddedRelationshipEntry(context, cache);
                    break;
                case EntityState.Deleted:
                    LoadDeletedRelationshipEntry(context, cache);
                    break;
            }
        }

        private void LoadAddedRelationshipEntry(ObjectStateEntry context, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            Contract.Assert(context.IsRelationship);
            Contract.Assert(context.State == EntityState.Added);

            var association = (AssociationType) context.EntitySet.ElementType;
            var roles = association.AssociationEndMembers;

            var current0 = GetDbSyncEntry((EntityKey) context.CurrentValues[0], cache);
            var current1 = GetDbSyncEntry((EntityKey) context.CurrentValues[1], cache);

            var property0 = roles[0].GetNavigationProperty();
            if (property0 != null)
                if (current0.CollectionsInternal.TryGetValue(property0.Name, out var collection))
                    collection.CurrentValuesInternal.Add(current1.TargetEntity);
                else
                    current0.CollectionsInternal.Add(property0.Name, new DbSyncCollectionEntry(property0.Name) { CurrentValuesInternal = { current1.TargetEntity } });

            var property1 = roles[1].GetNavigationProperty();
            if (property1 != null)
                if (current1.CollectionsInternal.TryGetValue(property1.Name, out var collection))
                    collection.CurrentValuesInternal.Add(current0.TargetEntity);
                else
                    current1.CollectionsInternal.Add(property1.Name, new DbSyncCollectionEntry(property1.Name) { CurrentValuesInternal = { current0.TargetEntity } });
        }

        private void LoadDeletedRelationshipEntry(ObjectStateEntry context, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            Contract.Assert(context.IsRelationship);
            Contract.Assert(context.State == EntityState.Deleted);

            var association = (AssociationType) context.EntitySet.ElementType;
            var roles = association.AssociationEndMembers;

            var original0 = GetDbSyncEntry((EntityKey) context.OriginalValues[0], cache);
            var original1 = GetDbSyncEntry((EntityKey) context.OriginalValues[1], cache);

            var property0 = roles[0].GetNavigationProperty();
            if (property0 != null)
                if (original0.CollectionsInternal.TryGetValue(property0.Name, out var collection))
                    collection.OriginalValuesInternal.Add(original1.TargetEntity);
                else
                    original0.CollectionsInternal.Add(property0.Name, new DbSyncCollectionEntry(property0.Name) { OriginalValuesInternal = { original1.TargetEntity } });

            var property1 = roles[1].GetNavigationProperty();
            if (property1 != null)
                if (original1.CollectionsInternal.TryGetValue(property1.Name, out var collection))
                    collection.OriginalValuesInternal.Add(original0.TargetEntity);
                else
                    original1.CollectionsInternal.Add(property1.Name, new DbSyncCollectionEntry(property1.Name) { OriginalValuesInternal = { original0.TargetEntity } });
        }

        private void LoadEntityEntry(ObjectStateEntry context, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            switch (context.State)
            {
                case EntityState.Added:
                    LoadAddedEntityEntry(context, cache);
                    break;
                case EntityState.Deleted:
                    LoadDeletedEntityEntry(context, cache);
                    break;
                case EntityState.Modified:
                    LoadModifiedEntityEntry(context, cache);
                    break;
            }
        }

        private void LoadAddedEntityEntry(ObjectStateEntry source, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            Contract.Assert(source.IsRelationship == false);
            Contract.Assert(source.State == EntityState.Added);

            var entry = CreateAddedDbSyncEntry(source);
            var entity = entry.TargetEntity;

            cache.Add(source.EntityKey, entry);

            foreach (var reference in source.GetReferences().Where(reference => reference.IsSource()))
            {
                var inverse = reference.GetTargetNavigationProperty();
                if (inverse == null)
                    continue;

                var key = reference.EntityKey;
                if (key == null || !TryGetDbSyncEntry(key, cache, out var current) || current.SourceState == EntityState.Added)
                    continue;

                switch (inverse.ToEndMember.RelationshipMultiplicity)
                {
                    case RelationshipMultiplicity.Many:
                        if (current.CollectionsInternal.TryGetValue(inverse.Name, out var collection)) collection.CurrentValuesInternal.Add(entity);
                        else current.CollectionsInternal.Add(inverse.Name, new DbSyncCollectionEntry(inverse.Name) { CurrentValuesInternal = { entity } });
                        break;
                    case RelationshipMultiplicity.One:
                    case RelationshipMultiplicity.ZeroOrOne:
                        if (current.ReferencesInternal.TryGetValue(inverse.Name, out var target)) target.CurrentValue = entity;
                        else current.ReferencesInternal.Add(inverse.Name, new DbSyncReferenceEntry(inverse.Name) { CurrentValue = entity });
                        break;
                }
            }
        }

        private void LoadDeletedEntityEntry(ObjectStateEntry source, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            Contract.Assert(source.IsRelationship == false);
            Contract.Assert(source.State == EntityState.Deleted);

            var entry = GetDbSyncEntry(source.EntityKey, cache);
            var entity = entry.TargetEntity;

            foreach (var reference in source.GetReferences().Where(reference => reference.IsSource()))
            {
                var inverse = reference.GetTargetNavigationProperty();
                if (inverse == null)
                    continue;

                var key = source.GetOriginalEntityKey(reference);
                if (key == null || !TryGetDbSyncEntry(key, cache, out var original) || original.SourceState == EntityState.Deleted)
                    continue;

                switch (inverse.ToEndMember.RelationshipMultiplicity)
                {
                    case RelationshipMultiplicity.Many:
                        if (original.CollectionsInternal.TryGetValue(inverse.Name, out var collection)) collection.OriginalValuesInternal.Add(entity);
                        else original.CollectionsInternal.Add(inverse.Name, new DbSyncCollectionEntry(inverse.Name) { OriginalValuesInternal = { entity } });
                        break;
                    case RelationshipMultiplicity.One:
                    case RelationshipMultiplicity.ZeroOrOne:
                        if (original.ReferencesInternal.TryGetValue(inverse.Name, out var target)) target.OriginalValue = entity;
                        else original.ReferencesInternal.Add(inverse.Name, new DbSyncReferenceEntry(inverse.Name) { OriginalValue = entity });
                        break;
                }
            }
        }

        private void LoadModifiedEntityEntry(ObjectStateEntry source, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            Contract.Assert(source.IsRelationship == false);
            Contract.Assert(source.State == EntityState.Modified);

            var entry = GetDbSyncEntry(source.EntityKey, cache);
            var entity = entry.TargetEntity;

            foreach (var property in source.GetModifiedProperties())
                entry.PropertiesInternal.Add(property, new DbSyncPropertyEntry(property)
                {
                    CurrentValue = source.CurrentValues[property],
                    OriginalValue = source.OriginalValues[property]
                });

            foreach (var reference in source.GetReferences().Where(reference => reference.IsSource() && source.IsReferenceChanged(reference)))
            {
                var property = source.GetEntityType().GetNavigationProperty(reference);
                if (property == null)
                    continue;

                var currentKey = reference.EntityKey;
                var originalKey = source.GetOriginalEntityKey(reference);

                DbSyncEntry original = null;
                DbSyncEntry current = null;
                entry.ReferencesInternal.Add(property.Name, new DbSyncReferenceEntry(property.Name)
                {
                    CurrentValue = currentKey != null && TryGetDbSyncEntry(currentKey, cache, out current) ? current.TargetEntity : null,
                    OriginalValue = originalKey != null && TryGetDbSyncEntry(originalKey, cache, out original) ? original.TargetEntity : null
                });

                var inverse = reference.GetTargetNavigationProperty();
                if (inverse == null)
                    continue;

                if (original != null && (EntityState.Modified | EntityState.Unchanged).HasFlag(original.SourceState))
                    switch (inverse.ToEndMember.RelationshipMultiplicity)
                    {
                        case RelationshipMultiplicity.Many:
                            if (original.CollectionsInternal.TryGetValue(inverse.Name, out var collection)) collection.OriginalValuesInternal.Add(entity);
                            else original.CollectionsInternal.Add(inverse.Name, new DbSyncCollectionEntry(inverse.Name) { OriginalValuesInternal = { entity } });
                            break;
                        case RelationshipMultiplicity.One:
                        case RelationshipMultiplicity.ZeroOrOne:
                            if (original.ReferencesInternal.TryGetValue(inverse.Name, out var target)) target.OriginalValue = entity;
                            else original.ReferencesInternal.Add(inverse.Name, new DbSyncReferenceEntry(inverse.Name) { OriginalValue = entity });
                            break;
                    }

                if (current != null && (EntityState.Modified | EntityState.Unchanged).HasFlag(current.SourceState))
                    switch (inverse.ToEndMember.RelationshipMultiplicity)
                    {
                        case RelationshipMultiplicity.Many:
                            if (current.CollectionsInternal.TryGetValue(inverse.Name, out var collection))
                                collection.CurrentValuesInternal.Add(entity);
                            else
                                current.CollectionsInternal.Add(inverse.Name, new DbSyncCollectionEntry(inverse.Name) { CurrentValuesInternal = { entity } });
                            break;
                        case RelationshipMultiplicity.One:
                        case RelationshipMultiplicity.ZeroOrOne:
                            if (current.ReferencesInternal.TryGetValue(inverse.Name, out var target)) target.OriginalValue = entity;
                            else current.ReferencesInternal.Add(inverse.Name, new DbSyncReferenceEntry(inverse.Name) { OriginalValue = entity });
                            break;
                    }
            }
        }

        private DbSyncEntry GetDbSyncEntry(EntityKey key, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            if (!TryGetDbSyncEntry(key, cache, out var entry))
                throw new KeyNotFoundException();

            return entry;
        }

        private bool TryGetDbSyncEntry(EntityKey key, IDictionary<EntityKey, DbSyncEntry> cache, out DbSyncEntry value)
        {
            if (cache.TryGetValue(key, out value))
                return true;

            if (!_source.GetObjectContext().ObjectStateManager.TryGetObjectStateEntry(key, out var source))
                return false;

            // we need to try to load the entity first to populate the object state manager, if the object exists

            if (!_target.GetObjectContext().TryGetObjectByKey(key, out _) || !_target.GetObjectContext().ObjectStateManager.TryGetObjectStateEntry(key, out var target))
                return false;

            cache.Add(key, value = new DbSyncEntry(target.Entity, key)
            {
                SourceState = source.State,
                TargetState = target.State
            });

            return true;
        }
    }
}