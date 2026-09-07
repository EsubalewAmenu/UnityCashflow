using System;
using UnityEngine;

// Resource material references retain the shaders and variants in device builds.
public static class BoardMaterials
{
    public static Material CreateSurface()
    {
        var template = Resources.Load<Material>("BoardSurface");
        if (template == null) throw new InvalidOperationException("Missing BoardSurface material. Reimport the project before building.");
        return new Material(template);
    }
}
