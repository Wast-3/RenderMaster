using System.Drawing;
using OpenTK.Graphics.OpenGL;


namespace RenderMaster;

// captures a snapshot of key OpenGL state for restoration
public class OpenGLStateSnapshot
{

public bool DepthTestEnabled { get; private set; }
public bool CullFaceEnabled { get; private set; }
public PolygonMode PolygonMode { get; private set; }
public Rectangle Viewport { get; private set; }
public DepthFunction DepthFunc { get; private set; }
public BlendingFactorSrc BlendSrc { get; private set; }
public BlendingFactorDest BlendDest { get; private set; }
public CullFaceMode CullFaceMode { get; private set; }
public FrontFaceDirection FrontFace { get; private set; }


public OpenGLStateSnapshot()
{

    DepthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);


    CullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);


    GL.GetInteger(GetPName.PolygonMode, out int polygonMode);
    PolygonMode = (PolygonMode)polygonMode;


    int[] viewport = new int[4];
    GL.GetInteger(GetPName.Viewport, viewport);
    Viewport = new Rectangle(viewport[0], viewport[1], viewport[2], viewport[3]);


    GL.GetInteger(GetPName.DepthFunc, out int depthFunc);
    DepthFunc = (DepthFunction)depthFunc;


    GL.GetInteger(GetPName.BlendSrc, out int blendSrc);
    BlendSrc = (BlendingFactorSrc)blendSrc;
    GL.GetInteger(GetPName.BlendDst, out int blendDest);
    BlendDest = (BlendingFactorDest)blendDest;


    GL.GetInteger(GetPName.CullFaceMode, out int cullFaceMode);
    CullFaceMode = (CullFaceMode)cullFaceMode;


    GL.GetInteger(GetPName.FrontFace, out int frontFace);
    FrontFace = (FrontFaceDirection)frontFace;
}



public void Restore()
{

    if (DepthTestEnabled)
        GL.Enable(EnableCap.DepthTest);
    else
        GL.Disable(EnableCap.DepthTest);


    if (CullFaceEnabled)
        GL.Enable(EnableCap.CullFace);
    else
        GL.Disable(EnableCap.CullFace);


    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode);


    GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);


    GL.DepthFunc(DepthFunc);


    GL.BlendFunc((BlendingFactor)BlendSrc, (BlendingFactor)BlendDest);


    GL.CullFace(CullFaceMode);


    GL.FrontFace(FrontFace);
}
}


// stack-based manager for OpenGL states
public class OpenGLStateStack
{
    private Stack<OpenGLStateSnapshot> stateStack = new Stack<OpenGLStateSnapshot>(); // saved states




    public void PushState()
    {
        stateStack.Push(new OpenGLStateSnapshot());
    }




    public void PopState()
    {
        if (stateStack.Count == 0) throw new InvalidOperationException("No state to pop!");


        stateStack.Pop().Restore();
    }
}