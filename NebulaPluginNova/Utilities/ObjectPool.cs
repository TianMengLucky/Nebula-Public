using Object = UnityEngine.Object;

namespace Nebula.Utilities;

public class ObjectPool<T> where T : Component
{
    List<T> activatedObjects = [];
    List<T> inactivatedObjects = [];
    public Action<T>? OnInstantiated { get; set; }
    Func<T> generator;
    Transform parent;

    public ObjectPool(T original, Transform parent){
        generator = () => Object.Instantiate(original, this.parent);
        this.parent = parent;
    }

    public ObjectPool(Func<Transform,T> generator, Transform parent)
    {
        this.generator = () => generator.Invoke(this.parent!);
        this.parent = parent;
    }

    public void DestroyAll()
    {
        foreach (var obj in activatedObjects) Object.Destroy(obj.gameObject);
        foreach (var obj in inactivatedObjects) Object.Destroy(obj.gameObject);
        activatedObjects.Clear();
        inactivatedObjects.Clear();
    }

    public T Instantiate()
    {
        if(inactivatedObjects.Count > 0)
        {
            var result = inactivatedObjects[inactivatedObjects.Count - 1];
            inactivatedObjects.RemoveAt(inactivatedObjects.Count - 1);

            activatedObjects.Add(result);
            result.gameObject.SetActive(true);
            return result;
        }
        else
        {
            var result = generator.Invoke();
            OnInstantiated?.Invoke(result);
            activatedObjects.Add(result);
            return result;
        }
    }

    public void RemoveAll()
    {
        foreach(var obj in activatedObjects)
        {
            obj.gameObject.SetActive(false);
            inactivatedObjects.Add(obj);
        }
        activatedObjects.Clear();
    }

    public void Inactivate(T obj)
    {
        activatedObjects.Remove(obj);
        inactivatedObjects.Add(obj);
        obj.gameObject.SetActive(false);
    }

    public int Count => activatedObjects.Count;
}
