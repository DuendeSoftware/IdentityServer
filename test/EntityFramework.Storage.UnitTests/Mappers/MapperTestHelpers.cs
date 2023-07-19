using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class MapperTestHelpers
{
    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource, TDestination> mapper,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(DefaultConstructor<TSource>(), NoExclusions, EmptyCustomization<TSource>(), mapper, Array.Empty<string>(), out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource> creator,
        Func<TSource, TDestination> mapper,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(creator, NoExclusions, EmptyCustomization<TSource>(), mapper, NoExclusions, out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Action<TSource> customizer,
        Func<TSource, TDestination> mapper,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(DefaultConstructor<TSource>(), NoExclusions, customizer, mapper, Array.Empty<string>(), out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource, TDestination> mapper,
        IEnumerable<string> notMapped,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(DefaultConstructor<TSource>(), NoExclusions, EmptyCustomization<TSource>(), mapper, notMapped, out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Action<TSource> customizer,
        Func<TSource, TDestination> mapper,
        IEnumerable<string> notMapped,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(DefaultConstructor<TSource>(), NoExclusions, customizer, mapper, notMapped, out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        IEnumerable<string> excludeFromAutoInitialization,
        Action<TSource> customizer,
        Func<TSource, TDestination> mapper,
        IEnumerable<string> notMapped,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(DefaultConstructor<TSource>(), excludeFromAutoInitialization, customizer, mapper, notMapped, out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource> creator,
        Action<TSource> customizer,
        Func<TSource, TDestination> mapper,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(creator, NoExclusions, customizer, mapper, NoExclusions, out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource> creator,
        Func<TSource, TDestination> mapper,
        IEnumerable<string> notMapped,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(creator, NoExclusions, EmptyCustomization<TSource>(), mapper, notMapped, out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource> creator,
        IEnumerable<string> notAutoInitialized,
        Action<TSource> customizer,
        Func<TSource, TDestination> mapper,
        IEnumerable<string> notMapped,
        out List<string> unmappedMembers)
    {

        var sourceType = typeof(TSource);
        var source = creator();
        var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();


        foreach(var property in sourceProperties.Where(p => !notAutoInitialized.Contains(p.Name)))
        {
            property.SetValue(source, GetNonDefaultValue(property.PropertyType));
        }

        customizer(source);
        var destination = mapper(source);

        unmappedMembers = new List<string>();
        var destinationType = typeof(TDestination);
        var destinationProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in destinationProperties)
        {
            if (!notMapped.Contains(property.Name))
            {
                var propertyValue = property.GetValue(destination);

                if (propertyValue == GetDefaultValue(property.PropertyType))
                {
                    unmappedMembers.Add(property.Name);
                }
            }
        }

        return unmappedMembers.Count == 0;
    }

    private static object GetDefaultValue(Type type)
    {
        if(type.IsAbstract ||
           type == typeof(string))
        {
            return null;
        }
        return Activator.CreateInstance(type);
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

        if (type == typeof(DateTime)) 
        {
            return DateTime.MaxValue;
        }

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1); 
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            var itemType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(itemType);
            var collection = Activator.CreateInstance(listType);

            // Add a non-default item to the collection
            var nonDefaultValue = GetNonDefaultValue(itemType);
            listType.GetMethod("Add")?.Invoke(collection, new[] { nonDefaultValue });

            return collection;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var dictionary = Activator.CreateInstance(dictionaryType);

            // Add a non-default item to the dictionary
            var nonDefaultKey = GetNonDefaultValue(keyType);
            var nonDefaultValue = GetNonDefaultValue(valueType);
            dictionaryType.GetMethod("Add")?.Invoke(dictionary, new[] { nonDefaultKey, nonDefaultValue });

            return dictionary;
        }

        if (type.IsValueType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                return GetNonDefaultValue(underlyingType);
            }

            return Activator.CreateInstance(type);
        }

        return Activator.CreateInstance(type);
    }


    private static Action<TSource> EmptyCustomization<TSource>()
    {
        return src => { };
    }

    private static Func<TSource> DefaultConstructor<TSource>()
    {
        return Activator.CreateInstance<TSource>;
    }

    private static string[] NoExclusions = Array.Empty<string>();
}