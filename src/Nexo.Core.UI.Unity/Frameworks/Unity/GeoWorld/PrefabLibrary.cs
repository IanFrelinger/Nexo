using System;
using UnityEngine;

namespace Nexo.GeoWorld.Unity;

[CreateAssetMenu(menuName = "Nexo/GeoWorld/Prefab Library", fileName = "GeoWorldPrefabLibrary")]
public sealed class PrefabLibrary : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string kind;
        public GameObject prefab;
    }

    public Entry[] entries;

    public GameObject GetPrefab(string kind)
    {
        if (entries == null) return null;
        for (var i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e != null && string.Equals(e.kind, kind, StringComparison.OrdinalIgnoreCase))
            {
                return e.prefab;
            }
        }
        return null;
    }
}

