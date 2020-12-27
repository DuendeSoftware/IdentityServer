using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace IntegrationTests
{
    public static class JsonElementExtensions
    {
        public static T ToObject<T>(this JsonElement element)
        {
            var json = element.GetRawText();
            return JsonSerializer.Deserialize<T>(json);
        }
        
        public static T ToObject<T>(this JsonDocument document)
        {
            var json = document.RootElement.GetRawText();
            return JsonSerializer.Deserialize<T>(json);
        }
        
        public static List<string> ToStringList(this JsonElement element)
        {
            return element.EnumerateArray().Select(item => item.GetString()).ToList();
        }
    }
}