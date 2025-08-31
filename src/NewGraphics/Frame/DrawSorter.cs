using System.Collections.Generic;

namespace RenderMaster.src.NewGraphics.Frame
{
    static class DrawSorter
    {
        public static void SortInPlace(List<ClassifiedDraw> draws)
        {
            // Sort by the pre-baked 64-bit key (ascending). Transparent draws use a
            // higher Pass id so they naturally come after opaque.
            draws.Sort((a, b) => a.SortKey.CompareTo(b.SortKey));

            // TODO: For transparent, refine ordering by depth back-to-front.
        }
    }
}

