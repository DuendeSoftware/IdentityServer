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
        return AllPropertiesAreMapped(DefaultConstructor<TSource>(), EmptyCustomization<TSource>(), mapper, Array.Empty<string>(), out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource> creator,
        Func<TSource, TDestination> mapper,
    out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(creator, EmptyCustomization<TSource>(), mapper, NoExclusions, out unmappedMembers);
    }


    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Action<TSource> customizer,
        Func<TSource, TDestination> mapper,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(DefaultConstructor<TSource>(), customizer, mapper, Array.Empty<string>(), out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource, TDestination> mapper,
        IEnumerable<string> exclusions,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(DefaultConstructor<TSource>(), EmptyCustomization<TSource>(), mapper, exclusions, out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Action<TSource> customizer,
        Func<TSource, TDestination> mapper,
        IEnumerable<string> exclusions,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(DefaultConstructor<TSource>(), customizer, mapper, exclusions, out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource> creator,
        Action<TSource> customizer,
        Func<TSource, TDestination> mapper,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(creator, customizer, mapper, NoExclusions, out unmappedMembers);
    }

    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource> creator,
        Func<TSource, TDestination> mapper,
        IEnumerable<string> exclusions,
        out List<string> unmappedMembers)
    {
        return AllPropertiesAreMapped(creator, EmptyCustomization<TSource>(), mapper, exclusions, out unmappedMembers);
    }


    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        Func<TSource> creator,
        Action<TSource> customizer,
        Func<TSource, TDestination> mapper,
        IEnumerable<string> exclusions,
        out List<string> unmappedMembers)
    {

        var sourceType = typeof(TSource);
        var source = creator();
        var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach(var property in sourceProperties)
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
            if (!exclusions.Contains(property.Name))
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
            return Activator.CreateInstance(listType);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            return Activator.CreateInstance(dictionaryType);
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