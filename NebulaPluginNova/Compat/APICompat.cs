namespace Nebula.Compat;

public static class APICompat
{
    static public Color ToUnityColor(this Virial.Color color) => new Color(color.R,color.G,color.B,color.A);
}
