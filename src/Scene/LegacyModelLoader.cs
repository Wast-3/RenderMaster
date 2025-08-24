namespace RenderMaster;

public class LegacyModelLoader : IModelLoader
{
    public float[] loadModel(string AssetPath)
    {
        List<float> vertices = new List<float>();

        using (var stream = File.OpenRead(AssetPath))
        using (var reader = new StreamReader(stream))
        {
            int totalLines = File.ReadLines(AssetPath).Count();
            int currentLine = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                currentLine++;
                string[] parts = line.Split(' ');

                if (currentLine % 5 == 0)
                {
                }

                for (int i = 0; i < parts.Length; i++)
                {
                    vertices.Add(float.Parse(parts[i]));
                }
            }
        }

        return vertices.ToArray();
    }
}
