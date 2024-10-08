﻿using Virial;
using Virial.Game;
using Object = UnityEngine.Object;

namespace Nebula.Modules.ScriptComponents;

[NebulaRPCHolder]
public abstract class NebulaSyncObject : INebulaScriptComponent, IGameOperator
{
    static private Dictionary<int, Func<float[], NebulaSyncObject>> instantiaters = new();
    static private Dictionary<int, NebulaSyncObject> allObjects = new();

    static protected void RegisterInstantiater(string tag, Func<float[], NebulaSyncObject> instantiater)
    {
        var hash = tag.ComputeConstantHash();
        if (instantiaters.ContainsKey(hash)) NebulaPlugin.Log.Print(NebulaLog.LogLevel.FatalError, $"Duplicated Instantiater Error ({tag})");
        instantiaters[hash] = instantiater;
    }

    public int ObjectId { get; private set; }
    public GamePlayer Owner { get; private set; } = null!;
    public bool AmOwner => Owner.AmOwner;
    private int TagHash { get; set; }

    private static int AvailableId(byte issuerId)
    {
        var idMask = issuerId << 16;
        while (true)
        {
            var cand = System.Random.Shared.Next(0xFFFF) | idMask;

            if (!allObjects.ContainsKey(cand)) return cand;
        }
    }
    public NebulaSyncObject()
    {
    }

    public virtual void OnInstantiated() { }
    public virtual void OnReleased() { 
        allObjects.Remove(ObjectId);
    }

    private float[] argumentsCache;

    static private byte OwnerIdFromObjectId(int objId) => (byte)(objId >>= 16);

    static private RemoteProcess<(int id,int tagHash, float[] arguments, bool skipLocal)> RpcInstantiateDef = new(
        "InstantiateObj",
        (message,local) =>
        {
            if (local && message.skipLocal) return;

            var obj = instantiaters[message.tagHash]?.Invoke(message.arguments);

            if (obj == null) return;

            obj.argumentsCache = message.arguments;
            obj.ObjectId = message.id;
            obj.Owner = NebulaGameManager.Instance!.GetPlayer(OwnerIdFromObjectId(obj.ObjectId))!;
            obj.TagHash = message.tagHash;
            if (allObjects.ContainsKey(obj.ObjectId)) throw new Exception("[NebulaSyncObject] Duplicated Key Error");
            obj.OnInstantiated();
            allObjects.Add(obj.ObjectId, obj);
        });

    static private RemoteProcess<int> RpcDestroyDef = new("DestroyObj",
       (message, _) =>
       {
           if (allObjects.TryGetValue(message, out var obj)) obj.ReleaseIt();
       });

    static public NebulaSyncObject? RpcInstantiate(string tag, float[]? arguments)
    {
        var id = AvailableId(PlayerControl.LocalPlayer.PlayerId);
        RpcInstantiateDef.Invoke(new(id, tag.ComputeConstantHash(), arguments ?? [], false));
        return allObjects[id];
    }

    static public NebulaSyncObject? LocalInstantiate(string tag, float[]? arguments)
    {
        var id = AvailableId(PlayerControl.LocalPlayer.PlayerId);
        RpcInstantiateDef.LocalInvoke(new(id, tag.ComputeConstantHash(), arguments ?? [], false));
        return allObjects[id];
    }

    //ローカルでの生成を全体に反映させます。
    public void ReflectInstantiationGlobally()
    {
        RpcInstantiateDef.Invoke(new(ObjectId, TagHash, argumentsCache, true));
    }

    static public void RpcDestroy(int id)
    {
        RpcDestroyDef.Invoke(id);
    }

    static public void LocalDestroy(int id)
    {
        RpcDestroyDef.LocalInvoke(id);
    }

    static public T? GetObject<T>(int id) where T : NebulaSyncObject
    {
        if (allObjects.TryGetValue(id, out var obj)) return obj as T;
        return default(T);
    }

    static public IEnumerator<T> GetObjects<T>(string tag) where T : NebulaSyncObject
    {
        var hash = tag.ComputeConstantHash();
        foreach(var obj in allObjects.Values)
        {
            if (obj.TagHash != hash) continue;
            var t = obj as T;
            if (t is not null) yield return t;
        }
    }
}

public class NebulaSyncStandardObject : NebulaSyncObject, IReleasable
{
    public enum ZOption
    {
        Back,
        Front,
        Just
    }

    public SpriteRenderer MyRenderer { get; private set; }

    public NebulaSyncStandardObject(Vector2 pos,ZOption zOrder,bool canSeeInShadow,Sprite sprite,Color color)
    {
        MyRenderer = UnityHelper.CreateObject<SpriteRenderer>("NebulaObject", null, pos, null);
        ZOrder = zOrder;
        CanSeeInShadow = canSeeInShadow;
        Sprite = sprite;
        Color = color;
    }

    public NebulaSyncStandardObject(Vector2 pos, ZOption zOrder, bool canSeeInShadow, Sprite sprite, bool semitransparent = false)
     : this(pos, zOrder, canSeeInShadow, sprite, semitransparent ? new Color(1, 1, 1, 0.5f) : Color.white) { }

    private ZOption zOrder;
    public ZOption ZOrder
    {
        get => zOrder;
        set {
            zOrder = value;
            Position = MyRenderer.transform.position;
        }
    }

    public Vector2 Position
    {
        get => MyRenderer.transform.position; 
        set {
            Vector3 pos = value;
            
            var z = value.y / 1000f;
            switch (ZOrder)
            {
                case ZOption.Back:
                    z += 0.001f;
                    break;
                case ZOption.Front:
                    z += -1f;
                    break;
            }

            pos.z = z;

            MyRenderer.transform.position = pos;
        }
    }

    public bool CanSeeInShadow
    {
        get => MyRenderer.gameObject.layer == LayerExpansion.GetObjectsLayer();
        set => MyRenderer.gameObject.layer = value ? LayerExpansion.GetObjectsLayer() : LayerExpansion.GetDefaultLayer();
    }

    public Sprite Sprite
    {
        get => MyRenderer.sprite;
        set => MyRenderer.sprite = value;
    }

    public Color Color
    {
        get => MyRenderer.color;
        set => MyRenderer.color = value;
    }

    public override void OnReleased()
    {
        base.OnReleased();
        if(MyRenderer) Object.Destroy(MyRenderer.gameObject);
    }
}