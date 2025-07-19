using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Argentian.Wrap {
    public unsafe class GpuBuffer: Disposable {
        public GpuBuffer(string name, BufferStorageMask flags_ = 0) : base(name) {
            flags = flags_;
            native = new BufferHandle(GL.CreateBuffer());
            GL.ObjectLabel(ObjectIdentifier.Buffer, ( uint ) native.Handle, Name.Length, Name);
        }

        public readonly BufferStorageMask flags;
        public Vector3i size = Vector3i.Zero;
        public Vector4i stride = Vector4i.Zero;
        public Vector3i offset = Vector3i.Zero;
        public BufferHandle native;
        public int length => stride.W;
        public int count => size.X * size.Y * size.Z;
        protected int SetCount<T>(Vector3i count_) {
            size = count_;
            stride.X = Marshal.SizeOf<T>();
            stride.Y = stride.X * size.X;
            stride.Z = stride.Y * size.Y;
            stride.W = stride.Z * size.Z;
            offset = Vector3i.Zero;
            return stride.W;
        }
        protected int SetCount<T>(Vector2i count_) {
            return SetCount<T>(new Vector3i(count_.X, count_.Y, 1));
        }
        protected int SetCount<T>(int count_) {
            return SetCount<T>(new Vector3i(count_, 1, 1));
        }
        protected override void Delete() {
            GL.DeleteBuffer(native.Handle);
        }
        public override string ToString() => $"Buffer {native.Handle} '{Name}'[{stride.W}]={size}*{stride.X}{DisposedString}";
    }
    public unsafe class TypedBuffer<T>: GpuBuffer where T : unmanaged {
        public TypedBuffer(string name, ref T data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(native.Handle, SetCount<T>(1), in data, flags);
        }
        public TypedBuffer(string name, T[] data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(native.Handle, SetCount<T>(data.Length), in data[0], flags);
        }
        public TypedBuffer(string name, T[,] data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(native.Handle, SetCount<T>(data.Length), in data[0,0], flags);
        }
        public TypedBuffer(string name, T[,,] data, BufferStorageMask flags_ = 0) : base(name, flags_) {
            GL.NamedBufferStorage(native.Handle, SetCount<T>(data.Length), in data[0,0,0], flags);
        }
        public TypedBuffer(string name, int count, BufferStorageMask flags_ = BufferStorageMask.DynamicStorageBit) : base(name, flags_) {
            GL.NamedBufferStorage(native.Handle, SetCount<T>(count), IntPtr.Zero, flags);
        }

        public void Set(ref T data) {
            GL.NamedBufferSubData(native.Handle, IntPtr.Zero, SetCount<T>(1), in data);
        }
        public void Set(T[] data) {
            GL.NamedBufferSubData(native.Handle, IntPtr.Zero, SetCount<T>(data.Length), in data[0]);
        }
        public void Set(T[,] data) {
             GL.NamedBufferSubData(native.Handle, IntPtr.Zero, SetCount<T>(data.Length), in data[0,0]);
        }
        public void Set(T[,,] data) {
            GL.NamedBufferSubData(native.Handle, IntPtr.Zero, SetCount<T>(data.Length), in data[0,0,0]);
        }
        public void Update(ref T data, long offset = 0, int length = 1) {
            GL.NamedBufferSubData(native.Handle, Offset(offset), SizeOf(length), in data);
        }
        public void Update(ReadOnlySpan<T> data, long offset = 0) {
            GL.NamedBufferSubData(native.Handle, Offset(offset), SizeOf(data.Length), in data[0]);
        }
        public void Update(T[,] data) { Update(data, Vector2i.Zero); }
        public void Update(T[,] data, Vector2i offset) {
            if (offset.X == 0 && data.GetUpperBound(0) == size.X) {
               GL.NamedBufferSubData(native.Handle, Offset(offset), SizeOf(data.Length), in data[0,0]);
            } else {
                int MaxY = Math.Min(offset.Y + data.GetLength(1), size.Y);
                for (int Y = offset.Y; Y < MaxY; Y++) {
                    GL.NamedBufferSubData(native.Handle, Offset(new Vector2i(offset.X, Y)), SizeOf(data.GetLength(1)), in data[0,Y]);
                }
            }
        }
        public void Update(T[,,] data) { Update(data, Vector3i.Zero); }
        public void Update(T[,,] data, Vector3i offset) {
            // If we're doing all th
            if (offset.X == 0 && offset.Y == 0 && data.GetUpperBound(0) == size.X && data.GetUpperBound(1) == size.Y) {
                GL.NamedBufferSubData(native.Handle, Offset(offset), SizeOf(data.Length), in data[0,0,0]);
            } else {
                int MaxZ = Math.Min(offset.Z + data.GetLength(2), size.Z);
                for (int Z = offset.Z; Z < MaxZ; Z++) {
                    Update(data, new Vector3i(offset.X, offset.Y, Z));
                }
            }
        }
        IntPtr Offset(long offset) => (IntPtr)(offset * stride.X);
        IntPtr Offset(Vector2i offset) => (IntPtr)(offset.X * stride.X + offset.Y * stride.Y);
        IntPtr Offset(Vector3i offset) => (IntPtr)(offset.X * stride.X + offset.Y * stride.Y + offset.Z * stride.Z);
        int SizeOf(int length) => length * stride.X;
        int SizeOf(Vector2i length) => length.X * stride.X + length.Y * stride.Y;
        int SizeOf(Vector3i length) => length.X * stride.X + length.Y * stride.Y + length.Z * stride.Z;
        public override string ToString() => $"{typeof(T).Name} {base.ToString()}";
    }
    public unsafe class MappedBuffer<T>: TypedBuffer<T> where T : unmanaged {
        public MappedBuffer(string name_, int count_, 
            BufferStorageMask flags_ = BufferStorageMask.MapReadBit | BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit | BufferStorageMask.MapCoherentBit,
            MapBufferAccessMask access_ = MapBufferAccessMask.MapReadBit | MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit
            )
            : base(name_, count_, flags_) {
            mapping = (T*)GL.MapNamedBufferRange(native.Handle, IntPtr.Zero, stride.W, access_);
        }
        public unsafe Span<T> Map() => new Span<T>((T*)mapping, stride.W);
        protected T* mapping;
        protected override void Delete() {
            base.Delete();
            GL.UnmapNamedBuffer(native.Handle);
        }
    }
}
