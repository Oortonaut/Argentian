using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Argentian.Wrap {
    public abstract class Disposable: IDisposable {
        //|## Public 
        public Disposable(string name_) { name = name_; }
        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public bool IsDisposed => disposed;
        public string DisposedString => disposed ? "  (Disposed)" : "";
        public override string ToString() => $"{Name}{DisposedString}";

        //|## Implement in Child
        //|  * {i} disposes of any IDisposable managed objects you own.
        //|    This isn't called during background garbage collections,
        //|    since those objects will be collected anyway.
        protected virtual void DisposeCLR() { }
        //|  * {i} is always called, and is used to clean up unmanaged
        //|    resources, system handles, and the like.
        protected virtual void QueueDelete() => DeleteQueue.Add(this);
        protected abstract void Delete();
        //|  * Yeah just give us the name.
        public string Name => name;
        readonly string name;
        //|## The Workings
        //|This is all the stuff that makes it work.
        bool disposed = false;
        protected void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    // TODO: dispose managed state (managed objects)
                    DisposeCLR();
                }
                QueueDelete();
                disposed = true;
            }
        }
        ~Disposable() {
            Dispose(disposing: false);
        }
        static readonly List<Disposable> DeleteQueue = new List<Disposable>();
        public static void ProcessDeleteQueue() {
            // GC.Collect();
            // GC.WaitForPendingFinalizers();
            foreach(var x in DeleteQueue) {
                x.Delete();
            };
            DeleteQueue.Clear();
        }
    }
#if false
    public abstract class GlDisposable<THandle>: Disposable where THandle : struct{
        public readonly THandle handle;
        public readonly ObjectIdentifier type;
        Action<THandle> deleter;
#region Public API
        public delegate void GLCreateFunc(int count, out THandle result);
        public delegate THandle ResultCreateFunc();
        static THandle Exec(GLCreateFunc f) { f(1, out THandle result); return result; }
        public GlDisposable(string name_, ObjectIdentifier type_, THandle handle_, Action<THandle> deleter_) {
            name = name_;
            handle = handle_;
            deleter = deleter_;
            type = type_;
            if(Handle > 0) {
                GL.ObjectLabel(type, (uint)Handle, Name.Length, Name);
            }
        }
        public int Handle => handle switch {
            BufferHandle h => h.Handle,
            VertexArrayHandle h => h.Handle,
            FramebufferHandle h => h.Handle,
            SamplerHandle h => h.Handle,
            ProgramHandle h => h.Handle,
            TextureHandle h => h.Handle,
            ProgramHandle h => h.Handle,
            ProgramHandle h => h.Handle,
            ProgramHandle h => h.Handle,
            ProgramHandle h => h.Handle,
            _ => 0,
        };

        public GlDisposable(string name_, ObjectIdentifier type_, GLCreateFunc creator_, Action<THandle> deleter_)
            : this(name_, type_, Exec(creator_), deleter_) {}
        public GlDisposable(string name_, ObjectIdentifier type_, ResultCreateFunc creator_, Action<THandle> deleter_)
            : this(name_, type_, creator_(), deleter_) { }
        public override string Name => name;
        readonly string name;
        public override string ToString() => $"{Name}: #{handle} {type} {DisposedString}";
#endregion
#region Creation
#endregion
    }
#endif
}
