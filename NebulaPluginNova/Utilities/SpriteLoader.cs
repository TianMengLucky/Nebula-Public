﻿using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Reflection;
using System.IO.Compression;
using Cpp2IL.Core.Extensions;
using Virial.Utilities;

namespace Nebula.Utilities;

public interface ITextureLoader
{
    Texture2D GetTexture();
}

public interface IDividedSpriteLoader
{
    Sprite GetSprite(int index);
    Image AsLoader(int index) => new WrapSpriteLoader(() => GetSprite(index));
    int Length { get; }
}

public static class GraphicsHelper
{
    public static Texture2D LoadTextureFromResources(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(path);
        if (stream == null) return null!;
        var byteTexture = new byte[stream.Length];
        stream.Read(byteTexture, 0, (int)stream.Length);
        LoadImage(texture, byteTexture, false);
        return texture;
    }

    public static Texture2D LoadTextureFromStream(Stream stream) => LoadTextureFromByteArray(stream.ReadBytes());

    public static Texture2D LoadTextureFromByteArray(byte[] data)
    {
        var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
        LoadImage(texture, data, false);
        return texture;
    }

    public static Texture2D LoadTextureFromDisk(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                var byteTexture = File.ReadAllBytes(path);
                LoadImage(texture, byteTexture, false);
                return texture;
            }
        }
        catch
        {
            //System.Console.WriteLine("Error loading texture from disk: " + path);
        }
        return null!;
    }

    public static Texture2D LoadTextureFromZip(ZipArchive? zip, string path)
    {
        if (zip == null) return null!;
        try
        {
            var entry = zip.GetEntry(path);
            if (entry != null)
            {
                var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                var stream = entry.Open();
                var byteTexture = new byte[entry.Length];
                stream.Read(byteTexture, 0, byteTexture.Length);
                stream.Close();
                LoadImage(texture, byteTexture, false);
                return texture;
            }
        }
        catch
        {
            System.Console.WriteLine("Error loading texture from disk: " + path);
        }
        return null!;
    }

    public static Sprite ToSprite(this Texture2D texture, float pixelsPerUnit) => ToSprite(texture, new Rect(0, 0, texture.width, texture.height),pixelsPerUnit);

    public static Sprite ToSprite(this Texture2D texture, Rect rect, float pixelsPerUnit)
    {
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    public static Sprite ToSprite(this Texture2D texture, Rect rect, Vector2 pivot,float pixelsPerUnit)
    {
        return Sprite.Create(texture, rect, pivot, pixelsPerUnit);
    }

    public static Sprite ToExpandableSprite(this Texture2D texture, float pixelsPerUnit,int x,int y)
    {
        return Sprite.CreateSprite(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(x, y, x, y), false);
    }

    internal delegate bool d_LoadImage(IntPtr tex, IntPtr data, bool markNonReadable);
    internal static d_LoadImage iCall_LoadImage = null!;
    public static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable)
    {
        if (iCall_LoadImage == null)
            iCall_LoadImage = IL2CPP.ResolveICall<d_LoadImage>("UnityEngine.ImageConversion::LoadImage");
        var il2cppArray = (Il2CppStructArray<byte>)data;
        return iCall_LoadImage.Invoke(tex.Pointer, il2cppArray.Pointer, markNonReadable);
    }
}

public class ResourceTextureLoader : ITextureLoader
{
    string address;
    Texture2D? texture = null;

    public ResourceTextureLoader(string address)
    {
        this.address = address;
    }

    public Texture2D GetTexture()
    {
        if (!texture) texture = GraphicsHelper.LoadTextureFromResources(address);
        return texture!;
    }
}

public class DiskTextureLoader : ITextureLoader
{
    string address;
    Texture2D texture = null!;
    bool isUnloadAsset = false;

    public DiskTextureLoader MarkAsUnloadAsset() { isUnloadAsset = true; return this; }
    public DiskTextureLoader(string address)
    {
        this.address = address;
    }

    public Texture2D GetTexture()
    {
        if (!texture)
        {
            texture = GraphicsHelper.LoadTextureFromDisk(address);
            if (isUnloadAsset) texture.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        }
        return texture;
    }
}

public class ZipTextureLoader : ITextureLoader
{
    ZipArchive archive;
    string address;
    Texture2D texture = null!;

    public ZipTextureLoader(ZipArchive zip,string address)
    {
        archive = zip;
        this.address = address;
    }

    public Texture2D GetTexture()
    {
        if (!texture)
        {
            texture = GraphicsHelper.LoadTextureFromZip(archive, address);
            if(texture!=null) texture.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
        }
        return texture!;
    }
}

public class StreamTextureLoader : ITextureLoader
{
    Texture2D texture;

    public StreamTextureLoader(Stream stream)
    {
        texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
        GraphicsHelper.LoadImage(texture, stream.ReadBytes(), false);
        texture.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
    }

    public Texture2D GetTexture() => texture;
}

public class UnloadTextureLoader : ITextureLoader
{
    Texture2D texture = null!;
    
    public UnloadTextureLoader(byte[] byteTexture, bool isFake = false)
    {
        texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
        GraphicsHelper.LoadImage(texture, byteTexture, false);
        if (!isFake) texture.hideFlags |= HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;
    }

    public class AsyncLoader
    {
        public UnloadTextureLoader? Result { get; private set; } = null;
        Func<Stream?> stream;
        bool IsFake;

        public AsyncLoader(Func<Stream?> stream, bool isFake = false)
        {
            this.stream = stream;
            IsFake = isFake;
        }

        private async Task<byte[]> ReadStreamAsync(Action<Exception>? exceptionHandler = null)
        {
            try
            {
                var myStream = stream.Invoke();
                if (myStream == null) return [];

                List<byte> bytes = [];

                MemoryStream dest = new();
                await myStream.CopyToAsync(dest);
                return dest.ToArray();
            }
            catch(Exception ex)
            {
                exceptionHandler?.Invoke(ex);

            }
            return [];
        }

        public IEnumerator LoadAsync(Action<Exception>? exceptionHandler = null)
        {
            if (stream == null) yield break;

            
            var task = ReadStreamAsync(exceptionHandler);
            while (!task.IsCompleted) yield return new WaitForSeconds(0.15f);
            
            Result = new UnloadTextureLoader(task.Result,IsFake);
        }
    }

    public Texture2D GetTexture() => texture;
    
}

public class AssetTextureLoader : ITextureLoader
{
    string address;
    Texture2D texture = null!;

    public AssetTextureLoader(string address)
    {
        this.address = address;
    }

    public Texture2D GetTexture()
    {
        if (!texture) texture = NebulaAsset.LoadAsset<Texture2D>(address);
        return texture;
    }
}

public class SpriteLoader : Image
{
    Sprite sprite = null!;
    float pixelsPerUnit;
    ITextureLoader textureLoader;
    public SpriteLoader(ITextureLoader textureLoader, float pixelsPerUnit)
    {
        this.textureLoader=textureLoader;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public Sprite GetSprite()
    {
        if (!sprite) sprite = textureLoader.GetTexture().ToSprite(pixelsPerUnit);
        sprite.hideFlags = textureLoader.GetTexture().hideFlags;
        return sprite;
    }

    static public SpriteLoader FromResource(string address, float pixelsPerUnit) => new SpriteLoader(new ResourceTextureLoader(address), pixelsPerUnit);
}

internal class NebulaSpriteLoader : AssetBundleResource<Sprite>, Image
{
    protected override AssetBundle AssetBundle => NebulaAsset.AssetBundle;

    Sprite Image.GetSprite() => Asset;

    public NebulaSpriteLoader(string name) : base(name) { }
}

public class ResourceExpandableSpriteLoader : Image
{
    Sprite sprite = null!;
    string address;
    float pixelsPerUnit;
    //端のピクセル数
    int x, y;
    public ResourceExpandableSpriteLoader(string address, float pixelsPerUnit,int x,int y)
    {
        this.address = address;
        this.pixelsPerUnit = pixelsPerUnit;
        this.x = x;
        this.y = y;
    }

    public Sprite GetSprite()
    {
        if (!sprite)
            sprite = GraphicsHelper.LoadTextureFromResources(address).ToExpandableSprite(pixelsPerUnit, x, y);
        return sprite;
    }
}


public class DividedSpriteLoader : Image, IDividedSpriteLoader
{
    float pixelsPerUnit;
    Sprite[] sprites;
    ITextureLoader texture;
    Tuple<int, int>? division, size;
    public Vector2 Pivot = new Vector2(0.5f, 0.5f);

    public DividedSpriteLoader(ITextureLoader textureLoader, float pixelsPerUnit, int x, int y, bool isSize = false)
    {
        this.pixelsPerUnit = pixelsPerUnit;
        if (isSize)
        {
            size = new(x, y);
            division = null;
        }
        else
        {
            division = new(x, y);
            size = null;
        }
        sprites = null!;
        texture = textureLoader;
    }

    public Sprite GetSprite(int index)
    {
        if (size == null || division == null || sprites == null)
        {
            var texture2D = texture.GetTexture();
            if (size == null)
                size = new(texture2D.width / division!.Item1, texture2D.height / division!.Item2);
            else if (division == null)
                division = new(texture2D.width / size!.Item1, texture2D.height / size!.Item2);
            sprites = new Sprite[division!.Item1 * division!.Item2];
        }

        if (!sprites[index])
        {
            var texture2D = texture.GetTexture();
            var _x = index % division!.Item1;
            var _y = index / division!.Item1;
            sprites[index] = texture2D.ToSprite(new Rect(_x * size.Item1, (division.Item2 - _y - 1) * size.Item2, size.Item1, size.Item2), Pivot, pixelsPerUnit);
        }
        return sprites[index];
    }

    public Sprite GetSprite() => GetSprite(0);

    public Image AsLoader(int index) => new WrapSpriteLoader(() => GetSprite(index));

    public int Length {
        get {
            if (division == null) GetSprite(0);
            return division!.Item1 * division!.Item2;
        }
    }

    static public DividedSpriteLoader FromResource(string address, float pixelsPerUnit, int x, int y, bool isSize = false)
         => new DividedSpriteLoader(new ResourceTextureLoader(address), pixelsPerUnit, x, y, isSize);
    static public DividedSpriteLoader FromDisk(string address, float pixelsPerUnit, int x, int y, bool isSize = false)
         => new DividedSpriteLoader(new DiskTextureLoader(address), pixelsPerUnit, x, y, isSize);
}

public class XOnlyDividedSpriteLoader : Image, IDividedSpriteLoader
{
    float pixelsPerUnit;
    Sprite[] sprites;
    ITextureLoader texture;
    int? division, size;
    public Vector2 Pivot = new Vector2(0.5f, 0.5f);

    public XOnlyDividedSpriteLoader(ITextureLoader textureLoader, float pixelsPerUnit, int x, bool isSize = false)
    {
        this.pixelsPerUnit = pixelsPerUnit;
        if (isSize)
        {
            size = x;
            division = null;
        }
        else
        {
            division = x;
            size = null;
        }
        sprites = null!;
        texture = textureLoader;
    }

    public Sprite GetSprite(int index)
    {
        if (!size.HasValue || !division.HasValue || sprites == null)
        {
            var texture2D = texture.GetTexture();
            if (size == null)
                size = texture2D.width / division;
            else if (division == null)
                division = texture2D.width / size!;
            sprites = new Sprite[division!.Value];
        }

        if (!sprites[index])
        {
            var texture2D = texture.GetTexture();
            sprites[index] = texture2D.ToSprite(new Rect(index * size!.Value, 0, size!.Value, texture2D.height), Pivot, pixelsPerUnit);
        }
        return sprites[index];
    }

    public Sprite GetSprite() => GetSprite(0);

    public int Length
    {
        get
        {
            if (!division.HasValue) GetSprite(0);
            return division!.Value;
        }
    }

    public Image WrapLoader(int index) => new WrapSpriteLoader(() => GetSprite(index));

    static public XOnlyDividedSpriteLoader FromResource(string address, float pixelsPerUnit, int x, bool isSize = false)
         => new XOnlyDividedSpriteLoader(new ResourceTextureLoader(address), pixelsPerUnit, x, isSize);
    static public XOnlyDividedSpriteLoader FromDisk(string address, float pixelsPerUnit, int x, bool isSize = false)
         => new XOnlyDividedSpriteLoader(new DiskTextureLoader(address), pixelsPerUnit, x, isSize);
}

public class WrapSpriteLoader : Image
{
    Func<Sprite> supplier;

    public WrapSpriteLoader(Func<Sprite> supplier)
    {
        this.supplier = supplier;
    }

    public Sprite GetSprite() => supplier.Invoke();
}