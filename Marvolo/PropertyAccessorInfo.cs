namespace Marvolo
{
    public class PropertyAccessorInfo
    {
        private readonly PropertyGetMethod _getMethod;

        private readonly PropertySetMethod _setMethod;

        public PropertyAccessorInfo(PropertyGetMethod getMethod, PropertySetMethod setmethod)
        {
            _getMethod = getMethod;
            _setMethod = setmethod;
        }

        public bool CanRead => _getMethod != null;

        public bool CanWrite => _setMethod != null;

        public object GetValue(object obj, object[] index)
        {
            return _getMethod(obj, index);
        }

        public void SetValue(object obj, object[] index, object value)
        {
            _setMethod(obj, index, value);
        }
    }
}