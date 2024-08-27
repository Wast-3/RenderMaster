using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;

namespace RenderMaster
{
    public class TextureCache
    {
        private static TextureCache instance;
        private Dictionary<string, BasicImageTexture> textureCache = new Dictionary<string, BasicImageTexture>();

        // Private constructor for singleton
        private TextureCache() { }

        // Public accessor for the singleton instance
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

        // Method to get or load a texture
        public BasicImageTexture GetTexture(string path, TextureUnit unit)
        {
            if (!textureCache.ContainsKey(path))
            {
                // Texture not in cache, load it
                var texture = new BasicImageTexture(path, unit);
                textureCache[path] = texture;
                return texture;
            }

            return textureCache[path];
        }

        // Optional: Clearing the cache (e.g., on scene unload or application exit)
        public void ClearCache()
        {
            foreach (var texture in textureCache.Values)
            {
                GL.DeleteTexture(texture.TextureId);  // Ensure OpenGL resources are also freed
            }
            textureCache.Clear();
        }
    }
}
