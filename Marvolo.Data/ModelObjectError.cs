namespace Marvolo.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class ModelObjectError
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public ModelObjectError(string message)
            : this(string.Empty, message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="message"></param>
        public ModelObjectError(string propertyName, string message)
        {
            Message = message;
            PropertyName = propertyName;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 
        /// </summary>
        public string PropertyName { get; }
    }
}