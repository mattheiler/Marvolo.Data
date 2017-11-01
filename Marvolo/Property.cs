using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Marvolo
{
    public static class Property
    {
        public static PropertyAccessorInfo GetAccessorInfo(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException(nameof(propertyInfo));

            return new PropertyAccessorInfo(propertyInfo.GetGetMethodDelegate(true), propertyInfo.GetSetMethodDelegate(true));
        }

        public static PropertyGetMethod GetGetMethodDelegate(this PropertyInfo property, bool nonPublic = false)
        {
            var method = property.GetGetMethod(nonPublic);
            if (method == null) return null;

            var objType = property.DeclaringType;
            if (objType == null) throw new InvalidOperationException("expected declaring type");

            var parameters = method.GetParameters();
            var parameterTypes = parameters.Select(parameter => parameter.ParameterType);

            var obj = Expression.Parameter(typeof(object));
            var index = Expression.Parameter(typeof(object[]));

            var instance = method.IsStatic ? null : Expression.Convert(obj, objType);
            var arguments = parameterTypes.Select((type, i) => Expression.Convert(Expression.ArrayAccess(index, Expression.Constant(i)), type));
            var body = Expression.Convert(Expression.Call(instance, method, arguments), typeof(object));

            return Expression.Lambda<PropertyGetMethod>(body, obj, index).Compile();
        }

        public static PropertySetMethod GetSetMethodDelegate(this PropertyInfo property, bool nonPublic = false)
        {
            var method = property.GetSetMethod(nonPublic);
            if (method == null) return null;

            var objType = property.DeclaringType;
            if (objType == null) throw new InvalidOperationException("expected declaring type");

            var parameters = method.GetParameters();
            var parameterTypes = parameters.Take(parameters.Length - 1).Select(parameter => parameter.ParameterType);

            var obj = Expression.Parameter(typeof(object));
            var index = Expression.Parameter(typeof(object[]));
            var value = Expression.Parameter(typeof(object));

            var instance = method.IsStatic ? null : Expression.Convert(obj, objType);
            var arguments = parameterTypes.Select((type, i) => Expression.Convert(Expression.ArrayAccess(index, Expression.Constant(i)), type)).Concat(new[] { Expression.Convert(value, property.PropertyType) });
            var body = Expression.Call(instance, method, arguments);

            return Expression.Lambda<PropertySetMethod>(body, obj, index, value).Compile();
        }
    }
}