namespace RenderMaster.src.NewGraphics.Types
{
    // A contiguous index range you can draw with one material (one glTF primitive)
    public readonly struct SubmeshSpan
    {
        public readonly int IndexStart;   // into the global index buffer (in elements, not bytes)
        public readonly int IndexCount;   // number of indices to draw
        public SubmeshSpan(int start, int count) { IndexStart = start; IndexCount = count; }
    }
}

