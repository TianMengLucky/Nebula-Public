using MS.Internal.Xml.XPath;

namespace Virial.Compat;

public struct Size
{
    public float Width;
    public float Height;

    public Size(float width, float height)
    {
        Width = width; Height = height;
    }

    internal Size(UnityEngine.Vector2 size)
    {
        Width = size.x;
        Height = size.y;
    }

    internal UnityEngine.Vector2 ToUnityVector() {
        return new(Width, Height);
    }

}

public struct FuzzySize
{
    public float? Width;
    public float? Height;

    public FuzzySize(float? width, float? height)
    {
        Width = width; Height = height;
        if (!Width.HasValue && !Height.HasValue) Width = 1f;
    }
}

public struct Vector2
{
    public float x , y;

    public Vector2()
    {
        x = 0f;
        y = 0f;
    }

    public Vector2(float x,float y)
    {
        this.x = x;
        this.y = y;
    }

    internal Vector2(UnityEngine.Vector2 v)
    {
        x = v.x;
        y = v.y;
    }

    internal UnityEngine.Vector2 ToUnityVector() => new(x, y);

    static public implicit operator UnityEngine.Vector2(Vector2 v) => v.ToUnityVector();
    static public Vector2 operator +(Vector2 v1, Vector2 v2) => new(v1.x + v2.x, v1.y + v2.y);
    static public Vector2 operator -(Vector2 v1, Vector2 v2) => new(v1.x - v2.x, v1.y - v2.y);
}


public struct Vector3
{
    public float x, y, z;

    public Vector3()
    {
        x = 0f;
        y = 0f;
        z = 0f;
    }

    public Vector3(float x, float y, float z = 0f)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3(UnityEngine.Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    internal UnityEngine.Vector3 ToUnityVector() => new UnityEngine.Vector3(x, y, z);

    static public implicit operator UnityEngine.Vector3(Vector3 v) => v.ToUnityVector();
}