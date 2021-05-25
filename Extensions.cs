using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NestSimplifier
{
    public static class Extensions
    {
        public static bool HasProperty<T>(this T obj, string propertyName, bool normalize = false)
        {
            var properties = obj.GetType().GetProperties();

            if (normalize)
            {
                bool hasProperty = properties.Where(w => w.Name.Trim().ToUpper() == propertyName.Trim().ToUpper()) != null;
                return hasProperty;
            }
            else
            {
                bool hasProperty = properties.Where(w => w.Name == propertyName) != null;
                return hasProperty;
            }
            
        }

        public static bool PropertyNotExistsOrIsNullOrEmpty<T>(this T obj, string propertyName, bool normalize = false)
        {
            if (obj.HasProperty(propertyName, normalize))
            {
                var propertyValue = obj.GetPropertyValue(propertyName);
                if ((propertyValue == null) || string.IsNullOrEmpty(propertyValue.ToString().Trim()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public static object GetPropertyValue<T>(this T obj, string propertyName)
        {
            var properties = obj.GetType().GetProperties();

            var foundProperty = properties.Where(w => w.Name.Trim() == propertyName.Trim()).FirstOrDefault();

            if (foundProperty != null)
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

        public static void CreateDynamicFrom<T>(this T obj)
        {
            ExpandoObject expandoObj = new ExpandoObject();

            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties();

            foreach (var prop in properties)
            {
                var name = prop.Name;
                var value = prop.GetValue(obj);

                ((IDictionary<string, object>)expandoObj)[name] = value;
            }

        }

    }
}
