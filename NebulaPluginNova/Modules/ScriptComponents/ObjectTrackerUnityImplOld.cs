﻿using Nebula.Compat;
using Virial.Components;
using Virial.Events.Game;
using Virial.Game;

namespace Nebula.Modules.ScriptComponents;

public static class HighlightHelpers
{
    public static void SetHighlight(Renderer renderer, Color color)
    {
        renderer.material.SetFloat("_Outline", 1f);
        renderer.material.SetColor("_OutlineColor", color);
    }

    internal static void SetHighlight(GamePlayer player, Color color) => SetHighlight(player.VanillaPlayer.cosmetics.currentBodySprite.BodySprite, color);
    internal static void SetHighlight(DeadBody? player, Color color)
    {
        if (player != null) SetHighlight(player.bodyRenderers[0], color);
    }

    public static void SetHighlight(GamePlayer player, Virial.Color color) => SetHighlight(player, color.ToUnityColor());
    public static void SetDeadBodyHighlight(GamePlayer player, Virial.Color color) => SetHighlight(player.RelatedDeadBody, color.ToUnityColor());
}

public static class ObjectTrackers
{
    static public Predicate<GamePlayer> StandardPredicate = p => !p.AmOwner && !p.IsDead && !p.Unbox().IsInvisible && !p.IsDived;
    static public Predicate<GamePlayer> ImpostorKillPredicate = p => StandardPredicate(p) && !p.IsImpostor;


    public static ObjectTracker<GamePlayer> ForPlayer(float? distance, GamePlayer tracker, Predicate<GamePlayer> predicate, Color? color = null, bool canTrackInVent = false, bool ignoreCollider = false) => ForPlayer(distance, tracker, predicate, null, color ?? Color.yellow, canTrackInVent, ignoreCollider);
    public static ObjectTracker<GamePlayer> ForPlayer(float? distance, GamePlayer tracker, Predicate<GamePlayer> predicate, Predicate<GamePlayer>? predicateHeavier, Color? color, bool canTrackInVent = false, bool ignoreCollider = false)
    {
        if (!canTrackInVent)
        {
            var lastPredicate = predicate;
            predicate = p => !p.VanillaPlayer.inVent && lastPredicate(p);
        }
        IEnumerable<PlayerControl> FastPlayers() => PlayerControl.AllPlayerControls.GetFastEnumerator();

        return new ObjectTrackerUnityImpl<GamePlayer, PlayerControl>(tracker.VanillaPlayer, distance ?? AmongUsUtil.VanillaKillDistance, FastPlayers, predicate, predicateHeavier, p => p.GetModInfo(), p => p.GetTruePosition(), p => p.cosmetics.currentBodySprite.BodySprite, color, ignoreCollider);
    }

    public static ObjectTracker<GamePlayer> ForDeadBody(float? distance, GamePlayer tracker, Predicate<GamePlayer> predicate, Predicate<GamePlayer>? predicateHeavier = null, Color? color = null, bool ignoreCollider = false)
    {
        return new ObjectTrackerUnityImpl<GamePlayer, DeadBody>(tracker.VanillaPlayer, distance ?? AmongUsUtil.VanillaKillDistance, () => Helpers.AllDeadBodies().Where(d => d.bodyRenderers.Any(r => r.enabled)), predicate, predicateHeavier, d => NebulaGameManager.Instance.GetPlayer(d.ParentId), d => d.TruePosition, d => d.bodyRenderers[0], color, ignoreCollider);
    }

    public static ObjectTracker<Vent> ForVents(float? distance, GamePlayer tracker, Predicate<Vent> predicate, Color color, bool ignoreColliders = false)
    {
        return new ObjectTrackerUnityImpl<Vent, Vent>(tracker.VanillaPlayer, distance ?? AmongUsUtil.VanillaKillDistance, () => ShipStatus.Instance.AllVents, predicate, _ => true, v => v, v => v.transform.position, v => v.myRend, color, ignoreColliders);
    }
}



public class ObjectTrackerUnityImpl<V,T> : INebulaScriptComponent, ObjectTracker<V>, IGameOperator where T : MonoBehaviour where V : class
{
    V? ObjectTracker<V>.CurrentTarget => currentTarget?.Item2;
    bool ObjectTracker<V>.IsLocked { get => isLocked; set => isLocked = value; }

    private Tuple<T,V>? currentTarget = null;

    PlayerControl tracker;
    Func<IEnumerable<T>> allTargets;
    Predicate<V> predicate;
    Predicate<V>? predicateHeavier = null;
    Func<T, V> converter;
    Func<T, Vector2> positionConverter;
    Func<T, SpriteRenderer> rendererConverter;
    Color color = Color.yellow;
    bool ignoreColliders;
    float maxDistance;
    private bool isLocked = false;

    public ObjectTrackerUnityImpl(PlayerControl tracker, float maxDistance, Func<IEnumerable<T>> allTargets, Predicate<V> predicate, Predicate<V>? predicateHeavier, Func<T, V> converter, Func<T, Vector2> positionConverter, Func<T, SpriteRenderer> rendererConverter, Color? color = null, bool ignoreColliders = false)
    {
        this.tracker = tracker;
        this.allTargets = allTargets;
        this.predicate = predicate;
        this.predicateHeavier = predicateHeavier;
        this.converter = converter;
        this.positionConverter = positionConverter;
        this.rendererConverter = rendererConverter;
        this.maxDistance = maxDistance;
        this.ignoreColliders = ignoreColliders;
        if(color.HasValue) this.color = color.Value;
    }

    private void ShowTarget()
    {
        if (currentTarget == null) return;

        HighlightHelpers.SetHighlight(rendererConverter.Invoke(currentTarget!.Item1), color);
    }

    void HudUpdate(GameHudUpdateEvent ev)
    {
        if (isLocked)
        {
            ShowTarget();
            return;
        }

        if (!tracker)
        {
            currentTarget = null;
            return;
        }

        var myPos = tracker.GetTruePosition();

        var distance = maxDistance;
        Tuple<T,V>? candidate = null;

        foreach (var t in allTargets())
        {
            if (!t) continue;

            var v = converter(t);
            if (v == null) continue;

            if (!predicate(v)) continue;

            var pos = positionConverter(t);
            var dVec = pos - myPos;
            var magnitude = dVec.magnitude;
            if (distance < magnitude) continue;

            if (!ignoreColliders && PhysicsHelpers.AnyNonTriggersBetween(myPos, dVec.normalized, magnitude, Constants.ShipAndObjectsMask)) continue;
            if (!(predicateHeavier?.Invoke(v) ?? true)) continue;

            candidate = new(t,v);
            distance = magnitude;
        }

        currentTarget = candidate;
        ShowTarget();
    }
}