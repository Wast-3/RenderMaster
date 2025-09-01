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

            //create exactly one buffer
            GL.CreateBuffers(1, out _buffer);
            //mark the buffer as being written to by the cpu so that it's placed in a performant region of memory, keep the buffer permanently mapped, let driver handle coherency (no explicit flushes)
            GL.NamedBufferStorage(_buffer, _size, IntPtr.Zero,
                BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapCoherentBit);

            //get a pointer to the buffer memory
            _ptr = GL.MapNamedBufferRange(_buffer, IntPtr.Zero, _size,
                BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapCoherentBit);

            RenderMaster.Engine.Logger.Log(
                $"UboRing<{typeof(T).Name}> stride={_stride} size={_size} perFrame={_perFrameCount} frames={_frames}",
                RenderMaster.Engine.LogLevel.Debug);
        }

        public void BeginFrame()
        {
            _frameIndex = (_frameIndex + 1) % _frames;
            _cursor = 0;
        }

        public (int buffer, int offset) Push(in T data)
        {
            if (_cursor >= _perFrameCount)
            {
                RenderMaster.Engine.Logger.Log($"UboRing<{typeof(T).Name}> exhausted: cursor={_cursor} perFrame={_perFrameCount}", RenderMaster.Engine.LogLevel.Error);
                throw new InvalidOperationException("UboRing exhausted for frame");
            }
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
