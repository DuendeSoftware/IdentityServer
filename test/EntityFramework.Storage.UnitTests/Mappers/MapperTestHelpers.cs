using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class MapperTestHelpers
{
    public static bool AllPropertiesAreMapped<TSource, TDestination>(
        TSource source,
        TDestination destination,
        IEnumerable<string> exclusions,
        out List<string> unmappedMembers)
    {

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
}