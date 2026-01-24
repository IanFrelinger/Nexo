using System;
using UnityEngine;

namespace Nexo.GeoWorld.Unity;

[CreateAssetMenu(menuName = "Nexo/GeoWorld/Material Library", fileName = "GeoWorldMaterialLibrary")]
public sealed class MaterialLibrary : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string materialName;
        public Material material;
    }

    public Entry[] entries;

    public Material GetMaterial(string name)
    {
        if (entries == null) return null;
        for (var i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e != null && string.Equals(e.materialName, name, StringComparison.OrdinalIgnoreCase))
            {
                return e.material;
            }
        }
        return null;
    }
}

