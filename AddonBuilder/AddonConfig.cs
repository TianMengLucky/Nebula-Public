using System.Text.Json.Serialization;

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

public class CosmicConfig
{
    
}

public class BaseCosmicInfo
{
    public string Name { get; set; }
    public string Author { get; set; }
    
    [JsonIgnore]
    public PackageInfo Package { get; set; }
    
    [JsonInclude, JsonPropertyName("package")]
    public string PackageId => Package.Package;
}

public class StampCosmicInfo : BaseCosmicInfo
{
    public int FPS { get; set; }
    public bool Adaptive { get; set; }
    public bool IsUnlockable { get; set; }
    public bool IsLocalizable { get; set; }
    public StampFileInfo Image { get; set; }

    public class StampFileInfo
    {
        public string Hash { get; set; }
        public string Address { get; set; }
        public string? ExHash { get; set; }
        public string? ExAddress { get; set; }
        public string? ExIsFront { get; set; }
        public int Length { get; set; }
    }
}

public class PackageInfo
{
    public string Package { get; set; }
    public string Format { get; set; }
    public int Priority { get; set; }
    public bool IsLocalizable { get; set; }
}

public class CosmicContentFile
{
    public string BundleName
    {
        get;
        set;
    }

    public List<BaseCosmicInfo> hats { get; set; } = [];
    public List<BaseCosmicInfo> Visors { get; set; } = [];
    public List<BaseCosmicInfo> Nameplates { get; set; } = [];
    public List<BaseCosmicInfo> Stamps { get; set; } = [];
}

public class LocalizationInfo
{
    public string Language { get; set; }
    
    public List<FileInfo> Entries { get; set; }

    public record FileInfo(string Hash, string Address);
}
