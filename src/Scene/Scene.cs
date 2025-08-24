using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace RenderMaster;



public class Scene(string name, int width, int height)
{

    string name = name;


    public List<Model> sceneModels = new();


    public Camera camera = new Camera(new Vector3(2, 0, 0), new Vector3(0, 0, 0), 0.8f,(float)width / (float)height, 1, 100000000);







    public void AddModel(Model model)
    {
        sceneModels.Add(model);
    }



    public void RemoveModel(Model model)
    {
        sceneModels.Remove(model);
    }



    public void Update(double deltaTime)
    {
        // Placeholder for simulation updates
    }




    public void RenderScene(FrameEventArgs args)
    {


        GL.ClearColor(0.4f, 0.4f, 0.4f, 1.0f);


        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


        foreach (Model model in sceneModels)
        {
            model.Render(args, camera);
        }
    }



    public void RenderSceneSetup()
    {
        GL.Enable(EnableCap.DepthTest);
    }
}
