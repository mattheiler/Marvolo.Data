using System;
using System.Collections.Generic;
using System.Linq;

namespace Marvolo.Data
{
    public class ModelObjectErrorInfo : IModelObjectErrorInfo
    {
        private readonly Dictionary<string, List<ModelObjectError>> _errors = new Dictionary<string, List<ModelObjectError>>();

        public bool HasErrors => _errors.Any();

        public event EventHandler<ModelObjectErrorInfoChangedEventArgs> Changed;

        public void Add(ModelObjectError error)
        {
            if (_errors.TryGetValue(error.PropertyName, out var entry)) entry.Add(error);
            else _errors.Add(error.PropertyName, new List<ModelObjectError> { error });

            OnErrorsChanged(error.PropertyName);
        }

        public void AddRange(IEnumerable<ModelObjectError> errors)
        {
            foreach (var grouping in errors.GroupBy(error => error.PropertyName))
            {
                if (_errors.TryGetValue(grouping.Key, out var entry)) entry.AddRange(grouping);
                else _errors.Add(grouping.Key, grouping.ToList());

                OnErrorsChanged(grouping.Key);
            }
        }

        public void Clear()
        {
            var properties = _errors.Keys.ToList();

            _errors.Clear();

            foreach (var property in properties) OnErrorsChanged(property);
        }

        public void Remove(string propertyName)
        {
            _errors.Remove(propertyName);

            OnErrorsChanged(propertyName);
        }

        public IEnumerable<ModelObjectError> GetErrors(string propertyName)
        {
            return _errors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<ModelObjectError>();
        }

        protected void OnErrorsChanged(string propertyName)
        {
            Changed?.Invoke(this, new ModelObjectErrorInfoChangedEventArgs(propertyName));
        }
    }
}