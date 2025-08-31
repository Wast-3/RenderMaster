using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace RenderMaster.src.NewGraphics.Programs
{
    sealed class UniformBuffer<T> : IDisposable where T : struct
    {
        readonly int _buffer;
        readonly int _size;
        readonly IntPtr _ptr;

        public UniformBuffer()
        {
            _size = Marshal.SizeOf<T>();
            GL.CreateBuffers(1, out _buffer);
            GL.NamedBufferStorage(_buffer, _size, IntPtr.Zero,
                BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapCoherentBit);
            _ptr = GL.MapNamedBufferRange(_buffer, IntPtr.Zero, _size,
                BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapCoherentBit);
        }

        public void Update(in T data)
        {
            unsafe
            {
                System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(
                    (void*)_ptr,
                    System.Runtime.CompilerServices.Unsafe.AsPointer(ref System.Runtime.CompilerServices.Unsafe.AsRef(data)),
                    (uint)_size);
            }
        }

        public void Bind(int bindingPoint) =>
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, bindingPoint, _buffer);

        public void Dispose()
        {
            GL.UnmapNamedBuffer(_buffer);
            GL.DeleteBuffer(_buffer);
        }
    }
}
