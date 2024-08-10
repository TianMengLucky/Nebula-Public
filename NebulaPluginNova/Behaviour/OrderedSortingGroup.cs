using Il2CppInterop.Runtime.Injection;
using UnityEngine.Rendering;

namespace Nebula.Behaviour;

public class ZOrderedSortingGroup : MonoBehaviour
{
    static ZOrderedSortingGroup() => ClassInjector.RegisterTypeInIl2Cpp<ZOrderedSortingGroup>();
    private SortingGroup? group = null;
    private Renderer? renderer = null;
    public int ConsiderParents = 0;
    public void SetConsiderParentsTo(Transform parent)
    {
        var num = 0;
        var t = transform;
        while(!(t == parent || t == null))
        {
            num++;
            t = t.parent;
        }
        ConsiderParents = num;
    }
    public void Start()
    {
        if(!gameObject.TryGetComponent<Renderer>(out renderer)) group = gameObject.AddComponent<SortingGroup>();
    }

    private float rate = 20000f;
    private int baseValue = 5;
    public void Update()
    {
        var z = transform.localPosition.z;
        var t = transform;
        for (var i = 0; i < ConsiderParents; i++)
        {
            t = t.parent;
            z += t.localPosition.z;
        }
        var layer = baseValue - (int)(rate * z);
        if (group != null)group.sortingOrder = layer;
        if(renderer != null) renderer.sortingOrder = layer;
    }
}
