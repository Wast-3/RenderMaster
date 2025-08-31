using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Programs;
using RenderMaster.src.NewGraphics.Types;
using RenderMaster.Engine;

namespace RenderMaster.src.NewGraphics.Frame
{
    // Minimal encoder that binds pass state, material textures, VAOs and issues draws.
    // Shaders/program state is intentionally left out for now.
    static class DrawEncoder
    {
        public static void EncodeAndDraw(
            IReadOnlyList<ClassifiedDraw> draws,
            GPUResourceTable gpu,
            ProgramLibrary programs,
            ProgramUniforms uniforms,
            Func<Handle<GPUResourceTable.MaterialGPU>, MaterialBlock> materialBlockOf,
            Func<Matrix4x4, Matrix4x4> computeNormalWorld)
        {
            PassKind currentPass = (PassKind)(-1);
            int currentVao = -1;
            int lastMaterialId = -1;
            ProgramKey currentKey = default;
            ShaderProgram? currentProg = null;
            int lastMatUboOffset = -1;

            foreach (var d in draws)
            {
                if (d.Pass != currentPass)
                {
                    BindPassState(d.Pass);
                    currentPass = d.Pass;
                    currentVao = -1;
                    lastMaterialId = -1;
                    lastMatUboOffset = -1;
                }

                var variants = ProgramVariants.None;
                if (d.Pass == PassKind.ForwardTransparent) variants |= ProgramVariants.AlphaBlend;

            var key = new ProgramKey(d.Technique, d.Pass, variants, VertexLayoutVersion: 1);
                if (currentProg == null || !key.Equals(currentKey))
                {
                    currentProg = programs.Get(key);
                    GL.UseProgram(currentProg.Handle);
                    GlCheck("UseProgram");
                    currentKey = key;
                }

                if (d.Packet.Material.IsValid && d.Packet.Material.Id != lastMaterialId)
                {
                    var mb = materialBlockOf(d.Packet.Material);
                    var (buf, off) = uniforms.MaterialRing.Push(mb);
                    GL.BindBufferRange(BufferRangeTarget.UniformBuffer, BindingPoints.Material, buf, (IntPtr)off, 256);
                    lastMaterialId = d.Packet.Material.Id;
                    lastMatUboOffset = off;
                    BindMaterialTextures(gpu, d.Packet.Material);
                }
                else if (lastMatUboOffset >= 0)
                {
                    uniforms.MaterialRing.BindRange(BindingPoints.Material, lastMatUboOffset);
                }

                var ob = new ObjectBlock
                {
                    World = d.Packet.World,
                    NormalWorld = computeNormalWorld(d.Packet.World)
                };
                if (float.IsNaN(ob.World.M11) || float.IsNaN(ob.NormalWorld.M11))
                    RenderMaster.Engine.Logger.Log("ObjectBlock contains NaNs (world/normalWorld).", RenderMaster.Engine.LogLevel.Error);
                var (objBuf, objOff) = uniforms.ObjectRing.Push(ob);
                GL.BindBufferRange(BufferRangeTarget.UniformBuffer, BindingPoints.Object, objBuf, (IntPtr)objOff, 256);

                ref readonly var mesh = ref gpu.GetMesh(d.Packet.Mesh);
                if (mesh.indexCount == 0 && d.Packet.Span.IndexCount == 0)
                    RenderMaster.Engine.Logger.Log($"Draw with empty index buffer: meshId={d.Packet.Mesh.Id}", RenderMaster.Engine.LogLevel.Warning);
                if (d.Packet.Span.IndexStart + d.Packet.Span.IndexCount > mesh.indexCount && mesh.indexCount > 0)
                    RenderMaster.Engine.Logger.Log(
                        $"Submesh out-of-range: start={d.Packet.Span.IndexStart} count={d.Packet.Span.IndexCount} meshIndexCount={mesh.indexCount}",
                        RenderMaster.Engine.LogLevel.Error);
                if (mesh.vao != currentVao)
                {
                    GL.BindVertexArray(mesh.vao);
                    GlCheck("BindVertexArray");
                    currentVao = mesh.vao;
                }
                var indexSize = GetIndexElementSizeBytes(mesh.indexType);
                var offsetBytes = d.Packet.Span.IndexStart * indexSize;
                GL.DrawElements(PrimitiveType.Triangles, d.Packet.Span.IndexCount, mesh.indexType, new IntPtr(offsetBytes));
            }

            GL.ColorMask(true, true, true, true);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
        }

        public static Matrix4x4 ComputeNormalWorld(Matrix4x4 world)
        {
            Matrix4x4.Invert(world, out var inv);
            return inv;
        }

        private static void BindPassState(PassKind pass)
        {
            switch (pass)
            {
                case PassKind.ForwardOpaque:
                    GL.Disable(EnableCap.Blend);
                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthMask(true);
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(CullFaceMode.Back);
                    GL.ColorMask(true, true, true, true);
                    break;

                case PassKind.ForwardTransparent:
                    GL.Enable(EnableCap.Blend);
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthMask(false);
                    GL.Enable(EnableCap.CullFace);
                    GL.CullFace(CullFaceMode.Back);
                    GL.ColorMask(true, true, true, true);
                    break;

                case PassKind.DepthOnly:
                    GL.Disable(EnableCap.Blend);
                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthMask(true);
                    GL.ColorMask(false, false, false, false);
                    break;

                case PassKind.Shadow:
                    // Placeholder: often depth-only with bias-specific state
                    GL.Disable(EnableCap.Blend);
                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthMask(true);
                    GL.ColorMask(false, false, false, false);
                    break;
            }
            GlCheck("BindPassState");
        }

        private static void BindMaterialTextures(GPUResourceTable gpu, Types.Handle<GPUResourceTable.MaterialGPU> matHandle)
        {
            ref readonly var mat = ref gpu.GetMaterial(matHandle);

            // Semantic → texture unit mapping (keep stable)
            const int BaseColorUnit = 0;
            const int NormalUnit = 1;
            const int MetallicRoughnessUnit = 2;

            if (mat.textures.TryGetValue("BaseColorTexture", out var bc))
            {
                GL.BindTextureUnit(BaseColorUnit, bc.textureId);
                GL.BindSampler(BaseColorUnit, bc.samplerId);
            }

            if (mat.textures.TryGetValue("NormalTexture", out var n))
            {
                GL.BindTextureUnit(NormalUnit, n.textureId);
                GL.BindSampler(NormalUnit, n.samplerId);
            }

            if (mat.textures.TryGetValue("MetallicRoughnessTexture", out var mr))
            {
                GL.BindTextureUnit(MetallicRoughnessUnit, mr.textureId);
                GL.BindSampler(MetallicRoughnessUnit, mr.samplerId);
            }
        }

        private static int GetIndexElementSizeBytes(DrawElementsType type) => type switch
        {
            DrawElementsType.UnsignedByte => 1,
            DrawElementsType.UnsignedShort => 2,
            DrawElementsType.UnsignedInt => 4,
            _ => 4,
        };

        static void GlCheck(string where)
        {
            var err = GL.GetError();
            if (err != ErrorCode.NoError)
                RenderMaster.Engine.Logger.Log($"GL ERROR after {where}: {err}", RenderMaster.Engine.LogLevel.Error);
        }
    }
}

