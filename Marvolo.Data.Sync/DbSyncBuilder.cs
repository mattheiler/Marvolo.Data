using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Marvolo.Data.Sync
{
    /// <summary>
    /// </summary>
    public sealed class DbSyncBuilder
    {
        private readonly HashSet<ObjectStateEntry> _added = new HashSet<ObjectStateEntry>();

        private readonly HashSet<ObjectStateEntry> _deleted = new HashSet<ObjectStateEntry>();

        private readonly HashSet<ObjectStateEntry> _modified = new HashSet<ObjectStateEntry>();

        private readonly ObjectContext _source;

        private readonly ObjectContext _target;

        internal DbSyncBuilder(ObjectContext source, ObjectContext target)
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
            var entries = GetEntries().OrderBy(entry => entry.IsRelationship);

            // take care of relationships first

            foreach (var entry in entries)
            {
                if (entry.IsRelationship)
                {
                    LoadRelationshipEntry(entry, cache);
                }
                else
                {
                    LoadEntityEntry(entry, cache);
                }
            }

            return new DbSync(_target, cache.Values);
        }

        private IEnumerable<ObjectStateEntry> GetEntries(EntityState state = EntityState.Added | EntityState.Deleted | EntityState.Modified)
        {
            var entries = Enumerable.Empty<ObjectStateEntry>();

            if (state.HasFlag(EntityState.Added))
            {
                entries = entries.Concat(_added);
            }

            if (state.HasFlag(EntityState.Deleted))
            {
                entries = entries.Concat(_deleted);
            }

            if (state.HasFlag(EntityState.Modified))
            {
                entries = entries.Concat(_modified);
            }

            return entries;
        }

        private DbSyncEntry CreateAddedDbSyncEntry(ObjectStateEntry sourceEntry)
        {
            Contract.Assert(sourceEntry.IsRelationship == false);
            Contract.Assert(sourceEntry.State == EntityState.Added);

            var sourceEntity = sourceEntry.Entity;
            var sourceEntityKey = sourceEntry.EntityKey;

            if (_target.ObjectStateManager.TryGetObjectStateEntry(sourceEntityKey, out var targetEntry))
                return new DbSyncEntry(targetEntry.Entity, targetEntry.EntityKey)
                {
                    SourceState = EntityState.Added,
                    TargetState = targetEntry.State
                };

            // clone, or activate new instance
            // TODO test cases

            var targetEntityKey = _target.CreateEntityKey(sourceEntry.EntitySet.Name, sourceEntity);
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

            var property0 = GetNavigationProperty(roles[0]);
            if (property0 != null)
            {
                if (current0.CollectionsInternal.TryGetValue(property0.Name, out var collection))
                {
                    collection.CurrentValuesInternal.Add(current1.TargetEntity);
                }
                else
                {
                    current0.CollectionsInternal.Add(property0.Name, new DbSyncCollectionEntry(property0.Name) { CurrentValuesInternal = { current1.TargetEntity } });
                }
            }

            var property1 = GetNavigationProperty(roles[1]);
            if (property1 != null)
            {
                if (current1.CollectionsInternal.TryGetValue(property1.Name, out var collection))
                {
                    collection.CurrentValuesInternal.Add(current0.TargetEntity);
                }
                else
                {
                    current1.CollectionsInternal.Add(property1.Name, new DbSyncCollectionEntry(property1.Name) { CurrentValuesInternal = { current0.TargetEntity } });
                }
            }
        }

        private void LoadDeletedRelationshipEntry(ObjectStateEntry context, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            Contract.Assert(context.IsRelationship);
            Contract.Assert(context.State == EntityState.Deleted);

            var association = (AssociationType) context.EntitySet.ElementType;
            var roles = association.AssociationEndMembers;

            var original0 = GetDbSyncEntry((EntityKey) context.OriginalValues[0], cache);
            var original1 = GetDbSyncEntry((EntityKey) context.OriginalValues[1], cache);

            var property0 = GetNavigationProperty(roles[0]);
            if (property0 != null)
            {
                if (original0.CollectionsInternal.TryGetValue(property0.Name, out var collection))
                {
                    collection.OriginalValuesInternal.Add(original1.TargetEntity);
                }
                else
                {
                    original0.CollectionsInternal.Add(property0.Name, new DbSyncCollectionEntry(property0.Name) { OriginalValuesInternal = { original1.TargetEntity } });
                }
            }

            var property1 = GetNavigationProperty(roles[1]);
            if (property1 != null)
            {
                if (original1.CollectionsInternal.TryGetValue(property1.Name, out var collection))
                {
                    collection.OriginalValuesInternal.Add(original0.TargetEntity);
                }
                else
                {
                    original1.CollectionsInternal.Add(property1.Name, new DbSyncCollectionEntry(property1.Name) { OriginalValuesInternal = { original0.TargetEntity } });
                }
            }
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

            foreach (var reference in GetReferences(source).Where(IsSourceReference))
            {
                var inverse = GetNavigationPropertyForTarget(reference);
                if (inverse == null)
                {
                    continue;
                }

                var key = reference.EntityKey;
                if (key == null || !TryGetDbSyncEntry(key, cache, out var current) || current.SourceState == EntityState.Added)
                {
                    continue;
                }

                switch (inverse.ToEndMember.RelationshipMultiplicity)
                {
                    case RelationshipMultiplicity.Many:

                        if (current.CollectionsInternal.TryGetValue(inverse.Name, out var collection))
                        {
                            collection.CurrentValuesInternal.Add(entity);
                        }
                        else
                        {
                            current.CollectionsInternal.Add(inverse.Name, new DbSyncCollectionEntry(inverse.Name) { CurrentValuesInternal = { entity } });
                        }

                        break;

                    case RelationshipMultiplicity.One:
                    case RelationshipMultiplicity.ZeroOrOne:

                        if (current.ReferencesInternal.TryGetValue(inverse.Name, out var target))
                        {
                            target.CurrentValue = entity;
                        }
                        else
                        {
                            current.ReferencesInternal.Add(inverse.Name, new DbSyncReferenceEntry(inverse.Name) { CurrentValue = entity });
                        }

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

            foreach (var reference in GetReferences(source).Where(IsSourceReference))
            {
                var inverse = GetNavigationPropertyForTarget(reference);
                if (inverse == null)
                {
                    continue;
                }

                var key = GetEntityTypeKeyOriginalValue(source, reference);
                if (key == null || !TryGetDbSyncEntry(key, cache, out var original) || original.SourceState == EntityState.Deleted)
                {
                    continue;
                }

                switch (inverse.ToEndMember.RelationshipMultiplicity)
                {
                    case RelationshipMultiplicity.Many:

                        if (original.CollectionsInternal.TryGetValue(inverse.Name, out var collection))
                        {
                            collection.OriginalValuesInternal.Add(entity);
                        }
                        else
                        {
                            original.CollectionsInternal.Add(inverse.Name, new DbSyncCollectionEntry(inverse.Name) { OriginalValuesInternal = { entity } });
                        }

                        break;

                    case RelationshipMultiplicity.One:
                    case RelationshipMultiplicity.ZeroOrOne:

                        if (original.ReferencesInternal.TryGetValue(inverse.Name, out var target))
                        {
                            target.OriginalValue = entity;
                        }
                        else
                        {
                            original.ReferencesInternal.Add(inverse.Name, new DbSyncReferenceEntry(inverse.Name) { OriginalValue = entity });
                        }

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
            {
                entry.PropertiesInternal.Add(property, new DbSyncPropertyEntry(property)
                {
                    CurrentValue = source.CurrentValues[property],
                    OriginalValue = source.OriginalValues[property]
                });
            }

            foreach (var reference in GetReferences(source).Where(reference => IsSourceReference(reference) && IsSourceReferenceChanged(source, reference)))
            {
                var property = GetNavigationProperty(GetEntityType(source), reference);
                if (property == null)
                {
                    continue;
                }

                var currentKey = reference.EntityKey;
                var originalKey = GetEntityTypeKeyOriginalValue(source, reference);

                DbSyncEntry original = null;
                DbSyncEntry current = null;

                entry.ReferencesInternal.Add(property.Name, new DbSyncReferenceEntry(property.Name)
                {
                    CurrentValue = currentKey != null && TryGetDbSyncEntry(currentKey, cache, out current) ? current.TargetEntity : null,
                    OriginalValue = originalKey != null && TryGetDbSyncEntry(originalKey, cache, out original) ? original.TargetEntity : null
                });

                var inverse = GetNavigationPropertyForTarget(reference);
                if (inverse == null)
                {
                    continue;
                }

                if (original != null && (EntityState.Modified | EntityState.Unchanged).HasFlag(original.SourceState))
                {
                    switch (inverse.ToEndMember.RelationshipMultiplicity)
                    {
                        case RelationshipMultiplicity.Many:

                            if (original.CollectionsInternal.TryGetValue(inverse.Name, out var collection))
                            {
                                collection.OriginalValuesInternal.Add(entity);
                            }
                            else
                            {
                                original.CollectionsInternal.Add(inverse.Name, new DbSyncCollectionEntry(inverse.Name) { OriginalValuesInternal = { entity } });
                            }

                            break;

                        case RelationshipMultiplicity.One:
                        case RelationshipMultiplicity.ZeroOrOne:

                            if (original.ReferencesInternal.TryGetValue(inverse.Name, out var target))
                            {
                                target.OriginalValue = entity;
                            }
                            else
                            {
                                original.ReferencesInternal.Add(inverse.Name, new DbSyncReferenceEntry(inverse.Name) { OriginalValue = entity });
                            }

                            break;
                    }
                }

                if (current != null && (EntityState.Modified | EntityState.Unchanged).HasFlag(current.SourceState))
                {
                    switch (inverse.ToEndMember.RelationshipMultiplicity)
                    {
                        case RelationshipMultiplicity.Many:
                            if (current.CollectionsInternal.TryGetValue(inverse.Name, out var collection))
                            {
                                collection.CurrentValuesInternal.Add(entity);
                            }
                            else
                            {
                                current.CollectionsInternal.Add(inverse.Name, new DbSyncCollectionEntry(inverse.Name) { CurrentValuesInternal = { entity } });
                            }
                            break;
                        case RelationshipMultiplicity.One:
                        case RelationshipMultiplicity.ZeroOrOne:
                            if (current.ReferencesInternal.TryGetValue(inverse.Name, out var target))
                            {
                                target.OriginalValue = entity;
                            }
                            else
                            {
                                current.ReferencesInternal.Add(inverse.Name, new DbSyncReferenceEntry(inverse.Name) { OriginalValue = entity });
                            }
                            break;
                    }
                }
            }
        }

        private DbSyncEntry GetDbSyncEntry(EntityKey key, IDictionary<EntityKey, DbSyncEntry> cache)
        {
            if (!TryGetDbSyncEntry(key, cache, out var entry))
            {
                throw new KeyNotFoundException();
            }

            return entry;
        }

        private bool TryGetDbSyncEntry(EntityKey key, IDictionary<EntityKey, DbSyncEntry> cache, out DbSyncEntry value)
        {
            if (cache.TryGetValue(key, out value))
            {
                return true;
            }

            if (!_source.ObjectStateManager.TryGetObjectStateEntry(key, out var source))
            {
                return false;
            }

            // try to load the entity first to populate the object state manager, if the object exists

            if (!_target.TryGetObjectByKey(key, out _) || !_target.ObjectStateManager.TryGetObjectStateEntry(key, out var target))
            {
                return false;
            }

            cache.Add(key, value = new DbSyncEntry(target.Entity, key)
            {
                SourceState = source.State,
                TargetState = target.State
            });

            return true;
        }

        #region AssociationEndMember

        private static NavigationProperty GetNavigationProperty(AssociationEndMember member)
        {
            return member.GetEntityType().NavigationProperties.SingleOrDefault(property => property.RelationshipType == member.DeclaringType && property.FromEndMember == member);
        }

        #endregion

        #region  EntityType

      private static NavigationProperty GetNavigationProperty(EntityType type, EntityReference reference)
        {
            return type.NavigationProperties.SingleOrDefault(property => property.ToEndMember.Name == reference.TargetRoleName);
        }

        #endregion

        #region  EntityReference

        private static NavigationProperty GetNavigationPropertyForTarget(EntityReference reference)
        {
            var relationship = (AssociationSet) reference.RelationshipSet;
            var members = relationship.ElementType.AssociationEndMembers;
            var target = members[reference.TargetRoleName];
            var source = members[reference.SourceRoleName];
            return target.GetEntityType().NavigationProperties.SingleOrDefault(property => property.ToEndMember == source);
        }

        private static bool IsSourceReference(EntityReference reference)
        {
            var relationship = (AssociationSet) reference.RelationshipSet;
            var constraint = relationship.ElementType.Constraint;
            return constraint.ToRole.Name == reference.SourceRoleName;
        }

        private static bool IsSourceReferenceChanged(ObjectStateEntry entry, EntityReference reference)
        {
            var relationship = (AssociationSet) reference.RelationshipSet;

            var constraint = relationship.ElementType.Constraint;
            if (constraint.ToRole.Name == reference.TargetRoleName)
                throw new InvalidOperationException("Dependant end expected.");

            return constraint.ToProperties.Any(property => entry.IsPropertyChanged(property.Name));
        }

        #endregion

        #region  ObjectStateEntry

        private static EntityType GetEntityType(ObjectStateEntry entry)
        {
            var type = ObjectContext.GetObjectType(entry.Entity.GetType());
            return entry.ObjectStateManager.MetadataWorkspace.GetItem<EntityType>(type.FullName, DataSpace.OSpace);
        }

        private static EntityKey GetEntityTypeKeyOriginalValue(ObjectStateEntry entry, EntityReference reference)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Detached:
                    throw new InvalidOperationException("Cannot get the entity key for added or detached items.");
            }

            var relationship = (AssociationSet) reference.RelationshipSet;

            var constraint = relationship.ElementType.Constraint;
            if (constraint.ToRole.Name != reference.SourceRoleName)
                throw new InvalidOperationException("Source role expected.");

            var set = relationship.AssociationSetEnds[reference.TargetRoleName].EntitySet;
            var keys = new Dictionary<string, object>();

            // cache
            var originals = entry.OriginalValues;
            var to = constraint.ToProperties;
            var from = constraint.FromProperties;

            // ordered, 1-1 mapping
            for (var index = 0; index < from.Count; index++)
                keys[from[index].Name] = originals[to[index].Name];

            // build the entity key for the original value
            var value = new EntityKey($"{relationship.EntityContainer}.{set.Name}", keys);

            return entry.ObjectStateManager.TryGetObjectStateEntry(value, out var target) ? target.EntityKey : null;
        }

        private static IEnumerable<EntityReference> GetReferences(ObjectStateEntry entry)
        {
            return entry.RelationshipManager.GetAllRelatedEnds().OfType<EntityReference>();
        }

        #endregion
    }
}