using System.Collections.Generic;

namespace RenderMaster.src.ControlPlane;

sealed class StableIdRegistry<T> where T : class
{
    private readonly Dictionary<T, int> _ids =
        new Dictionary<T, int>(ReferenceEqualityComparer<T>.Instance);
    private int _next = 1; // start at 1 to keep 0 as "unset" if you want

    public int GetOrAdd(T obj)
    {
        if (!_ids.TryGetValue(obj, out var id))
        {
            id = _next++;
            _ids[obj] = id;
        }
        return id;
    }
}
