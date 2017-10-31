using System.ComponentModel;

namespace Marvolo.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface IModelObject : INotifyPropertyChanging, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        /// <summary>
        /// 
        /// </summary>
        IModelObjectErrorInfo ErrorInfo { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        void Invalidate(string propertyName);
    }
}