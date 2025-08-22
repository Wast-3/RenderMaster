using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using RenderMaster.Engine;

namespace RenderMaster
{
    public class TextureCache
    {
        private static TextureCache instance;
        private Dictionary<string, BasicImageTexture> textureCache = new Dictionary<string, BasicImageTexture>();


        private TextureCache() { }


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


        private int currentTextureUnit = 0;


        public TextureUnit GetTextureUnitForInt(int i)
        {
            switch (i)
            {
                case 0:
                    return TextureUnit.Texture0;
                case 1:
                    return TextureUnit.Texture1;
                case 2:
                    return TextureUnit.Texture2;
                case 3:
                    return TextureUnit.Texture3;
                case 4:
                    return TextureUnit.Texture4;
                case 5:
                    return TextureUnit.Texture5;
                case 6:
                    return TextureUnit.Texture6;
                case 7:
                    return TextureUnit.Texture7;
                case 8:
                    return TextureUnit.Texture8;
                case 9:
                    return TextureUnit.Texture9;
                case 10:
                    return TextureUnit.Texture10;
                case 11:
                    return TextureUnit.Texture11;
                case 12:
                    return TextureUnit.Texture12;
                case 13:
                    return TextureUnit.Texture13;
                case 14:
                    return TextureUnit.Texture14;
                case 15:
                    return TextureUnit.Texture15;
                case 16:
                    return TextureUnit.Texture16;
                case 17:
                    return TextureUnit.Texture17;
                case 18:
                    return TextureUnit.Texture18;
                case 19:
                    return TextureUnit.Texture19;
                case 20:
                    return TextureUnit.Texture20;
                case 21:
                    return TextureUnit.Texture21;
                case 22:
                    return TextureUnit.Texture22;
                case 23:
                    return TextureUnit.Texture23;
                case 24:
                    return TextureUnit.Texture24;
                case 25:
                    return TextureUnit.Texture25;
                case 26:
                    return TextureUnit.Texture26;
                case 27:
                    return TextureUnit.Texture27;
                case 28:
                    return TextureUnit.Texture28;
                case 29:
                    return TextureUnit.Texture29;
                case 30:
                    return TextureUnit.Texture30;
                case 31:
                    return TextureUnit.Texture31;
                default:
                    return TextureUnit.Texture0;
            }
        }


        public BasicImageTexture GetTexture(string path)
        {
            Logger.Log("Cache hit: request for texture path: " + path, LogLevel.Debug);
            if (!textureCache.ContainsKey(path))
            {

                Logger.Log("Texture not in cache, loading new texture", LogLevel.Debug);
                var textureUnit = GetTextureUnitForInt(currentTextureUnit);
                var texture = new BasicImageTexture(path, textureUnit);
                textureCache[path] = texture;
                Logger.Log("Used texture unit: " + textureUnit + " In int: " + currentTextureUnit, LogLevel.Debug);
                this.currentTextureUnit += 1;

                return texture;

            }

            Logger.Log("Found texture in cache, returning cached texture", LogLevel.Info);
            return textureCache[path];
        }


        public void ClearCache()
        {
            foreach (var texture in textureCache.Values)
            {
                GL.DeleteTexture(texture.TextureId);
            }
            textureCache.Clear();
        }
    }
}
