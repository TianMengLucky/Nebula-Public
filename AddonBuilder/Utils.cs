using System.Security.Cryptography;

namespace AddonBuilder;

public static class Utils
{
    private static readonly MD5 md5 = MD5.Create();
    
    public static async Task<string> GetHash(this Stream stream)
    {
        var hash_bytes = await md5.ComputeHashAsync(stream);
        var hash_string = Convert.ToHexString(hash_bytes);
        return hash_string;
    }

    public static async Task<string?> GetHashFormFile(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine("File not found: " + path);
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await stream.GetHash();
    }
}