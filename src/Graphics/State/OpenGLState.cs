using System.Drawing;
using OpenTK.Graphics.OpenGL;


namespace RenderMaster;

// captures a snapshot of key OpenGL state for restoration
public class OpenGLStateSnapshot
{

public bool DepthTestEnabled { get; private set; }
public bool CullFaceEnabled { get; private set; }
public bool ScissorTestEnabled { get; private set; }
public PolygonMode PolygonModeFront { get; private set; }
public PolygonMode PolygonModeBack { get; private set; }
public Rectangle Viewport { get; private set; }
public Rectangle ScissorBox { get; private set; }
public DepthFunction DepthFunc { get; private set; }
public BlendingFactorSrc BlendSrcRGB { get; private set; }
public BlendingFactorSrc BlendSrcAlpha { get; private set; }
public BlendingFactorDest BlendDstRGB { get; private set; }
public BlendingFactorDest BlendDstAlpha { get; private set; }
public CullFaceMode CullFaceMode { get; private set; }
public FrontFaceDirection FrontFace { get; private set; }


public OpenGLStateSnapshot()
{

    DepthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);


    CullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);

    ScissorTestEnabled = GL.IsEnabled(EnableCap.ScissorTest);


    int[] polygonModes = new int[2];
    GL.GetInteger(GetPName.PolygonMode, polygonModes);
    PolygonModeFront = (PolygonMode)polygonModes[0];
    PolygonModeBack = (PolygonMode)polygonModes[1];


    int[] viewport = new int[4];
    GL.GetInteger(GetPName.Viewport, viewport);
    Viewport = new Rectangle(viewport[0], viewport[1], viewport[2], viewport[3]);

    int[] scissorBox = new int[4];
    GL.GetInteger(GetPName.ScissorBox, scissorBox);
    ScissorBox = new Rectangle(scissorBox[0], scissorBox[1], scissorBox[2], scissorBox[3]);


    GL.GetInteger(GetPName.DepthFunc, out int depthFunc);
    DepthFunc = (DepthFunction)depthFunc;


    GL.GetInteger(GetPName.BlendSrcRgb, out int blendSrcRgb);
    BlendSrcRGB = (BlendingFactorSrc)blendSrcRgb;
    GL.GetInteger(GetPName.BlendSrcAlpha, out int blendSrcAlpha);
    BlendSrcAlpha = (BlendingFactorSrc)blendSrcAlpha;
    GL.GetInteger(GetPName.BlendDstRgb, out int blendDstRgb);
    BlendDstRGB = (BlendingFactorDest)blendDstRgb;
    GL.GetInteger(GetPName.BlendDstAlpha, out int blendDstAlpha);
    BlendDstAlpha = (BlendingFactorDest)blendDstAlpha;


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

    if (ScissorTestEnabled)
        GL.Enable(EnableCap.ScissorTest);
    else
        GL.Disable(EnableCap.ScissorTest);


    GL.PolygonMode(MaterialFace.Front, PolygonModeFront);
    GL.PolygonMode(MaterialFace.Back, PolygonModeBack);


    GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);

    GL.Scissor(ScissorBox.Left, ScissorBox.Top, ScissorBox.Width, ScissorBox.Height);


    GL.DepthFunc(DepthFunc);


    GL.BlendFuncSeparate(BlendSrcRGB, BlendDstRGB, BlendSrcAlpha, BlendDstAlpha);


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