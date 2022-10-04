using System;
using System.Reflection;

namespace Faster.Ioc.Tests
{
    public class PrivateObject<TType>
    {

        private readonly object _privateObject;

        public PrivateObject(object obj)
        {
            _privateObject = obj ?? throw new ArgumentNullException(nameof(obj));
        }

        public TResult GetField<TResult>(string fieldName)
        {
            var field = typeof(TType).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException("Field not found");
            }
            
            return (TResult)field.GetValue(_privateObject);
        }

        public TResult GetProperty<TResult>(string fieldName)
        {
            var property = typeof(TType).GetProperty(fieldName, BindingFlags.NonPublic);
            if (property == null)
            {
                throw new InvalidOperationException("Field not found");
            }

            return (TResult)property.GetValue(_privateObject);
        }

    }
}
