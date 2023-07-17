using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class MapperTestHelpers
{
    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        // TSource source,
        // TDestination destination,
        Func<TSource, TDestination> mapper,
        IEnumerable<string> exclusions,
        out List<string> unmappedMembers)
    {

        var sourceType = typeof(TSource);
        var source = Activator.CreateInstance(sourceType);
        var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach(var property in sourceProperties)
        {
            property.SetValue(source, GetNonDefaultValue(property.PropertyType));
        }


        var destination = mapper((TSource) source);


        unmappedMembers = new List<string>();
        var destinationType = typeof(TDestination);
        var destinationProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in destinationProperties)
        {
            if (!exclusions.Contains(property.Name))
            {
                var propertyValue = property.GetValue(destination);
                if (propertyValue == null)
                {
                    unmappedMembers.Add(property.Name);
                }
            }
        }

        return unmappedMembers.Count == 0;
    }

    private static object GetNonDefaultValue(Type type)
    {
        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(string))
        {
            return "Non-empty string";
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            var itemType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(itemType);
            return Activator.CreateInstance(listType);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            return Activator.CreateInstance(dictionaryType);
        }

        return Activator.CreateInstance(type);
    }
}