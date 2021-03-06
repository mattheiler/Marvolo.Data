﻿using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Marvolo.Data
{
    public class ModelObjectRepository<T> : IModelObjectRepository<T> where T : class
    {
        public ModelObjectRepository(IDbSet<T> set)
        {
            Set = set;
        }

        public IDbSet<T> Set { get; }

        public T Add(T item)
        {
            return Set.Add(item);
        }

        public T Delete(T item)
        {
            return Set.Remove(item);
        }

        IList<T> IModelObjectRepository<T>.ToList()
        {
            return Set.ToList();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => Set.Local.CollectionChanged += value;
            remove => Set.Local.CollectionChanged -= value;
        }

        public int Count()
        {
            return Set.Count();
        }

        public Task<int> CountAsync()
        {
            return Set.CountAsync();
        }
    }
}