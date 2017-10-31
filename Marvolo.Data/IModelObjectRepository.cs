using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Marvolo.Data
{
    /// <summary>
    /// </summary>
    public interface IModelObjectRepository : INotifyCollectionChanged
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int Count();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<int> CountAsync();
    }
}