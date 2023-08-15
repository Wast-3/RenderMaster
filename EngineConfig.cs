namespace RenderMaster
{
    public class EngineConfig
    {
        public List<String> Directories;
        public string BaseDirectory;
        public string ShaderDirectory;
        public string ModelDirectory;
        public string TextureDirectory;
        public string LoggingDirectory;
        public string FontDirectory;

        public EngineConfig()
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

            // Create all directories if they don't already exist:

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