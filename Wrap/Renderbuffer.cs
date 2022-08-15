using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace Argentian.Wrap {
    public class Renderbuffer: Disposable {
        // TODO: Use Def?
        public Renderbuffer(string name, InternalFormat storage, Size size_): base(name) {
            size = size_;
            handle = GL.CreateRenderbuffer();
            GL.ObjectLabel(ObjectIdentifier.Renderbuffer, ( uint ) handle.Handle, Name.Length, Name);
            GL.NamedRenderbufferStorage(handle, storage, size.Width, size.Height);
        }
        public RenderbufferHandle handle;
        protected override void Delete() {
            GL.DeleteRenderbuffer(handle);
        }
        public override string ToString() => $"Renderbuffer {handle.Handle} '{Name}'[{size}]{DisposedString}";
        public Size size;
    }
}
