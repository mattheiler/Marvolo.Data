using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Marvolo.Data.Extensions
{
    internal static class ObjectContextExtensions
    {
        internal static ObjectContext GetObjectContext(this IObjectContextAdapter context)
        {
            return context.ObjectContext;
        }

        public static NavigationProperty GetNavigationProperty(this AssociationEndMember member)
        {
            return member.GetEntityType().NavigationProperties.SingleOrDefault(property => property.RelationshipType == member.DeclaringType && property.FromEndMember == member);
        }

        public static NavigationProperty GetNavigationProperty(this EntityType type, EntityReference reference)
        {
            return type.NavigationProperties.SingleOrDefault(property => property.ToEndMember.Name == reference.TargetRoleName);
        }

        public static NavigationProperty GetTargetNavigationProperty(this EntityReference reference)
        {
            var relationship = (AssociationSet) reference.RelationshipSet;
            var members = relationship.ElementType.AssociationEndMembers;
            var target = members[reference.TargetRoleName];
            var source = members[reference.SourceRoleName];
            return target.GetEntityType().NavigationProperties.SingleOrDefault(property => property.ToEndMember == source);
        }

        public static bool IsSource(this EntityReference reference)
        {
            var relationship = (AssociationSet) reference.RelationshipSet;
            var constraint = relationship.ElementType.Constraint;
            return constraint.ToRole.Name == reference.SourceRoleName;
        }

        public static void RejectPropertyChanges(this ObjectStateEntry entry)
        {
            var properties = entry.GetModifiedProperties().ToList();

            foreach (var property in properties)
                entry.RejectPropertyChanges(property);
        }

        public static EntityType GetEntityType(this ObjectStateEntry entry)
        {
            var type = ObjectContext.GetObjectType(entry.Entity.GetType());
            return entry.ObjectStateManager.MetadataWorkspace.GetItem<EntityType>(type.FullName, DataSpace.OSpace);
        }

        public static EntityKey GetOriginalEntityKey(this ObjectStateEntry entry, EntityReference reference)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Detached:
                    throw new InvalidOperationException("Cannot get the original key for added or detached items.");
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

        public static IEnumerable<EntityReference> GetReferences(this ObjectStateEntry entry)
        {
            return entry.RelationshipManager.GetAllRelatedEnds().OfType<EntityReference>();
        }

        public static bool IsReferenceChanged(this ObjectStateEntry entry, EntityReference reference)
        {
            var relationship = (AssociationSet) reference.RelationshipSet;

            var constraint = relationship.ElementType.Constraint;
            if (constraint.ToRole.Name == reference.TargetRoleName)
                throw new InvalidOperationException("Dependant end expected.");

            return constraint.ToProperties.Any(property => entry.IsPropertyChanged(property.Name));
        }

        public static void RejectChanges(this ObjectContext context, EntityState state = EntityState.Added | EntityState.Deleted | EntityState.Modified)
        {
            var entries = context.ObjectStateManager.GetObjectStateEntries(state).ToList();

            foreach (var entry in entries)
                if (entry.IsRelationship)
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.ChangeState(EntityState.Detached);
                            break;
                        case EntityState.Deleted:
                            entry.ChangeState(EntityState.Unchanged);
                            break;
                    }
                else
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.ChangeState(EntityState.Detached);
                            break;
                        case EntityState.Deleted:
                            entry.ChangeState(EntityState.Unchanged);
                            break;
                        case EntityState.Modified:
                            entry.RejectPropertyChanges();
                            break;
                    }

            context.DetectChanges();
        }
    }
}