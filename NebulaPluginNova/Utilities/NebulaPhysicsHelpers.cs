namespace Nebula.Utilities;

public static class NebulaPhysicsHelpers
{
    public static bool AnyNonTriggersBetween(Vector2 source, Vector2 dirNorm, float mag, int layerMask, out float distance)
    {
        var num = Physics2D.RaycastNonAlloc(source, dirNorm, PhysicsHelpers.castHits, mag, layerMask);
        var result = false;
        distance = mag;
        for (var i = 0; i < num; i++)
        {
            if (!PhysicsHelpers.castHits[i].collider.isTrigger)
            {
                result = true;

                var d = source.Distance(PhysicsHelpers.castHits[i].point);
                if (d < distance) distance = d;
            }
        }
        return result;
    }
}
