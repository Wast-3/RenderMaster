using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace RenderMaster.src.NewGraphics.Programs
{
    sealed class UboRing<T> : IDisposable where T : struct
    {
        readonly int _buffer;
        readonly int _size;
        readonly int _stride;
        readonly int _frames;
        readonly int _perFrameCount;
        readonly IntPtr _ptr;
        int _frameIndex;
        int _cursor;

        public UboRing(int perFrameCount, int frames = 3)
        {
            _stride = ((Marshal.SizeOf<T>() + 255) / 256) * 256;
            _perFrameCount = perFrameCount;
            _frames = frames;
            _size = _stride * perFrameCount * frames;

            GL.CreateBuffers(1, out _buffer);
            GL.NamedBufferStorage(_buffer, _size, IntPtr.Zero,
                BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapCoherentBit);
            _ptr = GL.MapNamedBufferRange(_buffer, IntPtr.Zero, _size,
                BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapCoherentBit);
        }

        public void BeginFrame()
        {
            _frameIndex = (_frameIndex + 1) % _frames;
            _cursor = 0;
        }

        public (int buffer, int offset) Push(in T data)
        {
            if (_cursor >= _perFrameCount)
                throw new InvalidOperationException("UboRing exhausted for frame");
            int offset = (_frameIndex * _perFrameCount + _cursor) * _stride;
            unsafe
            {
                System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(
                    (void*)((nint)_ptr + offset),
                    System.Runtime.CompilerServices.Unsafe.AsPointer(ref System.Runtime.CompilerServices.Unsafe.AsRef(data)),
                    (uint)Marshal.SizeOf<T>());
            }
            _cursor++;
            return (_buffer, offset);
        }

        public void BindRange(int bindingPoint, int offset) =>
            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, bindingPoint, _buffer, (IntPtr)offset, _stride);

        public void Dispose()
        {
            GL.UnmapNamedBuffer(_buffer);
            GL.DeleteBuffer(_buffer);
        }
    }
}
