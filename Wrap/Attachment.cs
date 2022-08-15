using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace Argentian.Wrap {
    public abstract record Attachment {
        public record RenderBuffer(Wrap.Renderbuffer rb): Attachment {
            public override Size GetSize() => rb.size;
            public override void Bind(FramebufferHandle frameBuffer, FramebufferAttachment dest) {
                GL.NamedFramebufferRenderbuffer(frameBuffer, dest, RenderbufferTarget.Renderbuffer, rb.handle);
            }
        };
        public record Texture(Wrap.Texture img, uint mip = 0, uint layer = 0): Attachment() {
            public override Size GetSize() => new Size(
                img.def.size.Width >> ( int ) mip,
                img.def.size.Height >> ( int ) mip);
            public override void Bind(FramebufferHandle frameBuffer, FramebufferAttachment dest) {
                GL.NamedFramebufferTexture(frameBuffer, dest, img.handle, ( int ) mip);
            }
        };
        public abstract void Bind(FramebufferHandle frameBuffer, FramebufferAttachment dest);
        public abstract Size GetSize();
    }
}
