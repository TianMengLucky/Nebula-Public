namespace AddonBuilder;

public class AddonConfig
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public bool LoadRoles { get; set; } = false;
    public bool UseHiddenMembers { get; set; } = false;

    public bool Dev { get; set; } = false;

    public List<ServerConfig> Servers { get; set; } = [];

    public record ServerConfig(string DisplayName, string Ip, ushort Port);
}
