using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Argentian.Wrap {
    public unsafe class Buffer: Disposable {
        public Buffer(string name, BufferStorageMask flags_ = 0) : base(name) {
            flags = flags_;
            handle = GL.CreateBuffer();
            GL.ObjectLabel(ObjectIdentifier.Buffer, ( uint ) handle.Handle, Name.Length, Name);
        }

        public readonly BufferStorageMask flags;
        public int length = 0;
        public int count = 0;
        public int stride = 0;
        public BufferHandle handle;
        protected int SetCount<T>(int count_) {
            stride = Marshal.SizeOf<T>();
            count = count_;
            length = stride * count;
            return length;
        }
        protected override void Delete() {
            GL.DeleteBuffer(handle);
        }
        public override string ToString() => $"Buffer {handle.Handle} '{Name}'[{length}]={count}*{stride}{DisposedString}";
    }
    public unsafe class TypedBuffer<T>: Buffer where T : unmanaged {
        public TypedBuffer(string name, ref T data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(handle, SetCount<T>(1), in data, flags);
        }
        public TypedBuffer(string name, T[] data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(handle, SetCount<T>(data.Length), in data[0], flags);
        }
        public TypedBuffer(string name, T[,] data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(handle, SetCount<T>(data.Length), in data[0,0], flags);
        }
        public TypedBuffer(string name, T[,,] data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(handle, SetCount<T>(data.Length), in data[0,0,0], flags);
        }
        public TypedBuffer(string name, int count, BufferStorageMask flags_ = BufferStorageMask.DynamicStorageBit) : base(name, flags_) {
            GL.NamedBufferStorage(handle, SetCount<T>(count), IntPtr.Zero, flags);
        }

        public void Set(ref T data) {
            GL.NamedBufferSubData(handle, IntPtr.Zero, SetCount<T>(1), in data);
        }
        public void Set(T[] data) {
            GL.NamedBufferSubData(handle, IntPtr.Zero, SetCount<T>(data.Length), in data[0]);
        }
        public void Set(T[,] data) {
             GL.NamedBufferSubData(handle, IntPtr.Zero, SetCount<T>(data.Length), in data[0,0]);
        }
        public void Set(T[,,] data) {
            GL.NamedBufferSubData(handle, IntPtr.Zero, SetCount<T>(data.Length), in data[0,0,0]);
        }
        public void Update(ref T data, long offset, int length = 1) {
            GL.NamedBufferSubData(handle, Offset(offset), SizeOf(length), in data);
        }
        public void Update(ReadOnlySpan<T> data, long offset) {
            GL.NamedBufferSubData(handle, Offset(offset), SizeOf(data.Length), in data[0]);
        }
        public void Update(T[,] data, long offset) {
            GL.NamedBufferSubData(handle, Offset(offset), SizeOf(data.Length), in data[0,0]);
        }
        public void Update(T[,,] data, long offset) {
            GL.NamedBufferSubData(handle, Offset(offset), SizeOf(data.Length), in data[0,0,0]);
        }
        static IntPtr Offset(long offset) => (IntPtr)(offset * Marshal.SizeOf<T>());
        static int SizeOf(int length) => Marshal.SizeOf<T>() * length;
        public override string ToString() => $"{typeof(T).Name} {base.ToString()}";
    }
    public unsafe class MappedBuffer<T>: TypedBuffer<T> where T : unmanaged {
        public MappedBuffer(string name_, int count_, 
            BufferStorageMask flags_ = BufferStorageMask.MapReadBit | BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit | BufferStorageMask.MapCoherentBit,
            MapBufferAccessMask access_ = MapBufferAccessMask.MapReadBit | MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit
            )
            : base(name_, count_, flags_) {
            mapping = (T*)GL.MapNamedBufferRange(handle, IntPtr.Zero, length, access_);
        }
        public unsafe Span<T> Map() => new Span<T>((T*)mapping, count);
        protected T* mapping;
        protected override void Delete() {
            base.Delete();
            GL.UnmapNamedBuffer(handle);
        }
    }
}
