using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using OpenTK.Mathematics;

namespace Argentian.Wrap {
    public class Renderbuffer: Disposable {
        // TODO: Use Def?
        public Renderbuffer(string name, InternalFormat storage, Vector2i size_): base(name) {
            size = size_;
            native = new RenderbufferHandle(GL.CreateRenderbuffer());
            GL.ObjectLabel(ObjectIdentifier.Renderbuffer, ( uint ) native.Handle, Name.Length, Name);
            GL.NamedRenderbufferStorage(native.Handle, storage, size.X, size.Y);
        }
        public RenderbufferHandle native;
        protected override void Delete() {
            GL.DeleteRenderbuffer(native.Handle);
        }
        public override string ToString() => $"Renderbuffer {native.Handle} '{Name}'[{size}]{DisposedString}";
        public Vector2i size;
    }
}
