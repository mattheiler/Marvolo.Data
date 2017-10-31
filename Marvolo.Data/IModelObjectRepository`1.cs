using System.Collections.Generic;

namespace Marvolo.Data
{
    /// <summary>
    /// </summary>
    /// <typeparam name="TModelObject"></typeparam>
    public interface IModelObjectRepository<TModelObject> : IModelObjectRepository
    {
        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        TModelObject Add(TModelObject item);

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        TModelObject Delete(TModelObject item);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        IList<TModelObject> ToList();
    }
}