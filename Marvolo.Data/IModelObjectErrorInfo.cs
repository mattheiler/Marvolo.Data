using System;
using System.Collections.Generic;

namespace Marvolo.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface IModelObjectErrorInfo
    {
        /// <summary>
        /// 
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// 
        /// </summary>
        event EventHandler<ModelObjectErrorInfoChangedEventArgs> Changed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="error"></param>
        void Add(ModelObjectError error);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errors"></param>
        void AddRange(IEnumerable<ModelObjectError> errors);

        /// <summary>
        /// 
        /// </summary>
        void Clear();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        void Remove(string propertyName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        IEnumerable<ModelObjectError> GetErrors(string propertyName);
    }
}