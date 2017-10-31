using System;
using System.Threading;
using System.Threading.Tasks;

namespace Marvolo.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface IModelObjectWorkspace : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        event EventHandler AcceptedChanges;

        /// <summary>
        /// 
        /// </summary>
        event EventHandler RejectedChanges;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool HasChanges();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool Save();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<bool> SaveAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> SaveAsync(CancellationToken token);

        /// <summary>
        /// 
        /// </summary>
        void Undo();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task UndoAsync();
    }
}