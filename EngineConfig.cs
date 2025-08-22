namespace RenderMaster
{
    public class EngineConfig
    {
        public static List<String> Directories;
        public static string BaseDirectory;
        public static string ShaderDirectory;
        public static string ModelDirectory;
        public static string TextureDirectory;
        public static string LoggingDirectory;
        public static string FontDirectory;

        static EngineConfig()
        {
            BaseDirectory = "H:\\Google Drive Sync\\dev\\Development\\RenderMaster\\EngineBaseDir";
            ShaderDirectory = Path.Combine(BaseDirectory, "Shaders");
            ModelDirectory = Path.Combine(BaseDirectory, "Models");
            TextureDirectory = Path.Combine(BaseDirectory, "Textures");
            LoggingDirectory = Path.Combine(BaseDirectory, "Logs");
            FontDirectory = Path.Combine(BaseDirectory, "Fonts");

            if (!Directory.Exists(BaseDirectory))
            {
                Directory.CreateDirectory(BaseDirectory);
            }

            Directories = new List<string>();
            Directories.Add(ShaderDirectory);
            Directories.Add(ModelDirectory);
            Directories.Add(TextureDirectory);
            Directories.Add(LoggingDirectory);
            Directories.Add(FontDirectory);



            foreach (string dir in Directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

        }

    }
}