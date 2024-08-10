namespace Nebula.Extensions;

public static class MapBehaviourExtension
{
    public static bool CanIdentifyImpostors = false;
    public static bool CanIdentifyDeadBodies = false;
    public static bool AffectedByCommSab = true;
    public static bool AffectedByFakeAdmin = true;
    public static bool ShowDeadBodies = true;
    public static Color? MapColor = null;
    public static void InitializeModOption(this MapCountOverlay overlay)
    {
        CanIdentifyImpostors = false;
        CanIdentifyDeadBodies = false;
        AffectedByCommSab = true;
        AffectedByFakeAdmin = true;
        ShowDeadBodies = GeneralConfigurations.ShowDeadBodiesOnAdminOption;
        MapColor = null;
    }

    public static void SetModOption(this MapCountOverlay overlay, bool? canIdentifyImpostors = null, bool? canIdentifyDeadBodies = null, bool? affectedByCommSab = null, bool? affectedByFakeAdmin = null, bool? showDeadBodies = null, Color ? mapColor = null)
    {
        if (canIdentifyImpostors.HasValue) CanIdentifyImpostors = canIdentifyImpostors.Value;
        if (canIdentifyDeadBodies.HasValue) CanIdentifyDeadBodies = canIdentifyDeadBodies.Value;
        if (affectedByCommSab.HasValue) AffectedByCommSab = affectedByCommSab.Value;
        if (affectedByFakeAdmin.HasValue) AffectedByFakeAdmin = affectedByFakeAdmin.Value;
        if (showDeadBodies.HasValue) ShowDeadBodies = showDeadBodies.Value;
        if (mapColor.HasValue)
        {
            MapColor = mapColor.Value;
            overlay.BackgroundColor.SetColor(MapColor ?? Color.green);
        }
    }

    public static void UpdateCount(this CounterArea counterArea, int cnt, int impostors, int deadBodies)
    {
        while (counterArea.myIcons.Count < cnt)
        {
            var item = counterArea.pool.Get<PoolableBehavior>();
            counterArea.myIcons.Add(item);
        }
        while (counterArea.myIcons.Count > cnt)
        {
            var poolableBehavior = counterArea.myIcons._items[counterArea.myIcons.Count - 1];
            counterArea.myIcons.RemoveAt(counterArea.myIcons.Count - 1);
            poolableBehavior.OwnerPool.Reclaim(poolableBehavior);
        }

        for (var i = 0; i < counterArea.myIcons.Count; i++)
        {
            var num = i % counterArea.MaxColumns;
            var num2 = i / counterArea.MaxColumns;
            var num3 = (float)(Mathf.Min(cnt - num2 * counterArea.MaxColumns, counterArea.MaxColumns) - 1) * counterArea.XOffset / -2f;
            counterArea.myIcons._items[i].transform.position = counterArea.transform.position + new Vector3(num3 + (float)num * counterArea.XOffset, (float)num2 * counterArea.YOffset, -1f);

            if (impostors > 0)
            {
                impostors--;
                PlayerMaterial.SetColors(Palette.ImpostorRed, counterArea.myIcons[i].GetComponent<SpriteRenderer>());
            }
            else if (deadBodies > 0)
            {
                deadBodies--;
                PlayerMaterial.SetColors(Palette.DisabledGrey, counterArea.myIcons[i].GetComponent<SpriteRenderer>());
            }
            else
            {
                PlayerMaterial.SetColors(new Color(224f / 255f, 255f / 255f, 0f / 255f), counterArea.myIcons[i].GetComponent<SpriteRenderer>());
            }
        }
    }
}
