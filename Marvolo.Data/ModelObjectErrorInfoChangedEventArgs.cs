using System;

namespace Marvolo.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class ModelObjectErrorInfoChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        public ModelObjectErrorInfoChangedEventArgs(string propertyName)
        {
            PropertyName = propertyName;
        }

        /// <summary>
        /// 
        /// </summary>
        public string PropertyName { get; }
    }
}