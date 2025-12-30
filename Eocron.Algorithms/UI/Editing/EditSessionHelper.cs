using Newtonsoft.Json;

namespace Eocron.Algorithms.UI.Editing;

public static class EditSessionHelper
{
    public static T DeepClone<T>(T source)
    {
        if (source == null)
        {
            return default;
        }
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source, Settings), Settings);
    }

    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
    {
        TypeNameHandling = TypeNameHandling.Objects
    };
}