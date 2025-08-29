using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;

namespace RenderMaster;

public class TextureCache
{
    private static TextureCache? instance;
    private readonly Dictionary<string, BasicImageTexture> textureCache = new();
    private readonly Stack<int> availableUnits = new();

    private TextureCache()
    {
        for (int i = 31; i >= 0; i--)
        {
            availableUnits.Push(i);
        }
    }

    public static TextureCache Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new TextureCache();
            }
            return instance;
        }
    }

    public void AcquireUnit(BasicImageTexture texture)
    {
        if (texture.BoundUnit.HasValue)
        {
            return;
        }

        if (availableUnits.Count > 0)
        {
            texture.BoundUnit = availableUnits.Pop();
        }
    }

    public void ReleaseUnit(BasicImageTexture texture)
    {
        if (texture.BoundUnit.HasValue)
        {
            availableUnits.Push(texture.BoundUnit.Value);
            texture.BoundUnit = null;
        }
    }

    public BasicImageTexture GetTexture(string path)
    {
        if (!textureCache.TryGetValue(path, out var texture))
        {
            texture = new BasicImageTexture(path);
            textureCache[path] = texture;
        }
        return texture;
    }

    public void ClearCache()
    {
        foreach (var texture in textureCache.Values)
        {
            texture.Dispose();
        }
        textureCache.Clear();
        availableUnits.Clear();
        for (int i = 31; i >= 0; i--)
        {
            availableUnits.Push(i);
        }
    }
}

