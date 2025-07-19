using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using OpenTK.Mathematics;

namespace Argentian.Wrap {
    public abstract record Attachment {
        public record RenderBuffer(Wrap.Renderbuffer rb): Attachment {
            public override Vector2i GetSize() => rb.size;
            public override void Bind(FramebufferHandle frameBuffer, FramebufferAttachment dest) {
                GL.NamedFramebufferRenderbuffer(frameBuffer.Handle, dest, RenderbufferTarget.Renderbuffer, (int)rb.native);
            }
        };
        public record Texture(Wrap.Texture img, uint mip = 0, uint layer = 0): Attachment() {
            public override Vector2i GetSize() => new Vector2i(
                img.def.size.X >> (int)mip,
                img.def.size.Y >> (int)mip);
            public override void Bind(FramebufferHandle frameBuffer, FramebufferAttachment dest) {
                GL.NamedFramebufferTexture(frameBuffer.Handle, dest, (int)img.native, (int)mip);
            }
        };
        public abstract void Bind(FramebufferHandle frameBuffer, FramebufferAttachment dest);
        public abstract Vector2i GetSize();
    }
}
