using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NestSimplifier
{
    public static class Extensions
    {
        public static bool HasProperty<T>(this T obj, string propertyName)
        {
            var properties = obj.GetType().GetProperties();
            bool hasProperty = properties.Where(w => w.Name.Trim().ToUpper() == propertyName.Trim().ToUpper()) != null;
            return hasProperty;
        }

        public static object GetPropertyValue<T>(this T obj, string propertyName)
        {
            var properties = obj.GetType().GetProperties();

            var foundProperty = properties.Where(w => w.Name.Trim() == propertyName.Trim()).FirstOrDefault();

            if(foundProperty != null)
            {
                return foundProperty.GetValue(obj);
            }
            else
            {
                return null;
            }
        }

        public static T SetPropertyValue<T>(this T obj, string propertyName, object propertyValue)
        {
            if (obj.HasProperty(propertyName))
            {
                var property = obj.GetType().GetProperty(propertyName);
                property.SetValue(obj, propertyValue);
            }            

            return obj;
        }


    }
}
