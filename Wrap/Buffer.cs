using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Argentian.Wrap {
    public class Buffer: Disposable {
        public Buffer(string name, BufferStorageMask flags_ = 0) : base(name) {
            flags = flags_;
            handle = GL.CreateBuffer();
            GL.ObjectLabel(ObjectIdentifier.Buffer, ( uint ) handle.Handle, Name.Length, Name);
        }

        public readonly BufferStorageMask flags;
        public int size = 0;
        public int length = 0;
        public int stride = 0;
        public BufferHandle handle;
        protected override void Delete() {
            GL.DeleteBuffer(handle);
        }
        public override string ToString() => $"Buffer {handle.Handle} '{Name}'[{length}] {size}/{stride}{DisposedString}";
    }
    public class TypedBuffer<T>: Buffer where T : unmanaged {
        public TypedBuffer(string name, ref T data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(handle, SetSize(1), in data, flags);
        }
        public TypedBuffer(string name, T[] data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(handle, SetSize(data.Length), in data[0], flags);
        }
        public TypedBuffer(string name, T[,] data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(handle, SetSize(data.Length), in data[0,0], flags);
        }
        public TypedBuffer(string name, T[,,] data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(handle, SetSize(data.Length), in data[0,0,0], flags);
        }

        public void Set(ref T data) {
            GL.NamedBufferSubData(handle, IntPtr.Zero, SetSize(1), in data);
        }
        public void Set(T[] data) {
            GL.NamedBufferSubData(handle, IntPtr.Zero, SetSize(data.Length), in data[0]);
        }
        public void Set(T[,] data) {
             GL.NamedBufferSubData(handle, IntPtr.Zero, SetSize(data.Length), in data[0,0]);
        }
        public void Set(T[,,] data) {
            GL.NamedBufferSubData(handle, IntPtr.Zero, SetSize(data.Length), in data[0,0,0]);
        }

        public void Update(ref T data, long offset) {
            GL.NamedBufferSubData(handle, (IntPtr)offset, SizeOf(1), in data);
        }
        public void Update(T[] data, long offset) {
            GL.NamedBufferSubData(handle, (IntPtr)offset, SizeOf(data.Length), in data[0]);
        }
        public void Update(T[,] data, long offset) {
                GL.NamedBufferSubData(handle, ( IntPtr ) offset, SizeOf(data.Length), in data[0,0]);
        }
        public void Update(T[,,] data, long offset) {
                GL.NamedBufferSubData(handle, ( IntPtr ) offset, SizeOf(data.Length), in data[0,0,0]);
        }

        static int SizeOf(int length) => Marshal.SizeOf<T>() * length;
        int SetSize(int length_) {
            stride = Marshal.SizeOf<T>();
            length = length_;
            size = stride * length;
            return size;
        }
        public override string ToString() => $"{typeof(T).Name} {base.ToString()}";
    }

}
