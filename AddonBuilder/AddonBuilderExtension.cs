using System.Text.Json;

namespace AddonBuilder;

public static class AddonBuilderExtension
{
    public static string GetMetaString(this AddonConfig config)
    {
        return JsonSerializer.Serialize(new
        {
            config.Id,
            config.Name,
            config.Author,
            config.Description,
            config.Version,
            Build = 0,
            Dependency = new List<string>(),
            Hidden = false
        });
    }

    public static string GetBehaviourString(this AddonConfig config)
    {
        return JsonSerializer.Serialize(new
        {
            config.LoadRoles,
            config.UseHiddenMembers
        });
    }
}