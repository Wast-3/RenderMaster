using System.Drawing;
using OpenTK.Graphics.OpenGL;

// This class captures and restores the state of various OpenGL settings. 
// It stores the current settings related to depth testing, face culling, 
// polygon mode, viewport, depth function, blending, and face culling orientation.
public class OpenGLStateSnapshot
{
    // Properties to hold the OpenGL state
    public bool DepthTestEnabled { get; private set; } // Whether depth testing is enabled
    public bool CullFaceEnabled { get; private set; }  // Whether face culling is enabled
    public PolygonMode PolygonMode { get; private set; } // Current polygon mode (fill, line, or point)
    public Rectangle Viewport { get; private set; } // Current OpenGL viewport dimensions
    public DepthFunction DepthFunc { get; private set; } // Current depth function used for depth testing
    public BlendingFactorSrc BlendSrc { get; private set; } // Source factor for blending
    public BlendingFactorDest BlendDest { get; private set; } // Destination factor for blending
    public CullFaceMode CullFaceMode { get; private set; } // Current mode for face culling (front, back, or both)
    public FrontFaceDirection FrontFace { get; private set; } // Orientation of front-facing polygons (clockwise or counter-clockwise)

    // Constructor that captures the current OpenGL state and stores it in the properties.
    public OpenGLStateSnapshot()
    {
        // Capture whether depth testing is currently enabled
        DepthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);

        // Capture whether face culling is currently enabled
        CullFaceEnabled = GL.IsEnabled(EnableCap.CullFace);

        // Capture the current polygon mode (GL_LINE, GL_FILL, etc.)
        GL.GetInteger(GetPName.PolygonMode, out int polygonMode);
        PolygonMode = (PolygonMode)polygonMode;

        // Capture the current viewport dimensions
        int[] viewport = new int[4];
        GL.GetInteger(GetPName.Viewport, viewport);
        Viewport = new Rectangle(viewport[0], viewport[1], viewport[2], viewport[3]);

        // Capture the current depth function used for comparing depth values
        GL.GetInteger(GetPName.DepthFunc, out int depthFunc);
        DepthFunc = (DepthFunction)depthFunc;

        // Capture the current blending factors for source and destination
        GL.GetInteger(GetPName.BlendSrc, out int blendSrc);
        BlendSrc = (BlendingFactorSrc)blendSrc;
        GL.GetInteger(GetPName.BlendDst, out int blendDest);
        BlendDest = (BlendingFactorDest)blendDest;

        // Capture the current face culling mode (which faces are culled)
        GL.GetInteger(GetPName.CullFaceMode, out int cullFaceMode);
        CullFaceMode = (CullFaceMode)cullFaceMode;

        // Capture the orientation of front-facing polygons
        GL.GetInteger(GetPName.FrontFace, out int frontFace);
        FrontFace = (FrontFaceDirection)frontFace;
    }

    // This method restores the captured OpenGL state.
    // It sets the OpenGL settings to match the values stored in this snapshot.
    public void Restore()
    {
        // Restore depth testing state
        if (DepthTestEnabled)
            GL.Enable(EnableCap.DepthTest);
        else
            GL.Disable(EnableCap.DepthTest);

        // Restore face culling state
        if (CullFaceEnabled)
            GL.Enable(EnableCap.CullFace);
        else
            GL.Disable(EnableCap.CullFace);

        // Restore the polygon mode
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode);

        // Restore the viewport dimensions
        GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);

        // Restore the depth function
        GL.DepthFunc(DepthFunc);

        // Restore the blending factors
        GL.BlendFunc((BlendingFactor)BlendSrc, (BlendingFactor)BlendDest);

        // Restore the face culling mode
        GL.CullFace(CullFaceMode);

        // Restore the front face orientation
        GL.FrontFace(FrontFace);
    }
}

// This class manages a stack of OpenGL state snapshots. 
// It allows you to save the current OpenGL state by pushing a snapshot onto the stack, 
// and later restore it by popping the snapshot off the stack.
public class OpenGLStateStack
{
    // Stack to hold the OpenGL state snapshots
    private Stack<OpenGLStateSnapshot> stateStack = new Stack<OpenGLStateSnapshot>();

    // Pushes the current OpenGL state onto the stack.
    // This method captures the current state using the OpenGLStateSnapshot class 
    // and then stores that snapshot on the stack.
    public void PushState()
    {
        stateStack.Push(new OpenGLStateSnapshot());
    }

    // Pops the top OpenGL state from the stack and restores it.
    // This method restores the OpenGL state to what it was when the top snapshot was created.
    // If the stack is empty, it throws an InvalidOperationException.
    public void PopState()
    {
        if (stateStack.Count == 0) throw new InvalidOperationException("No state to pop!");

        // Restore the OpenGL state from the snapshot at the top of the stack
        stateStack.Pop().Restore();
    }
}
