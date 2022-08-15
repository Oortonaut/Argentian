using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using OpenTK.Mathematics;
using System.ComponentModel;

namespace Argentian.Wrap {
    using GL_Buffer = OpenTK.Graphics.OpenGL.Buffer;
    public class PixelOps {
        public class Settings {
            public List<BlendUnit?> blends = new();
            public DepthUnit? depth = null;
            public StencilUnit? stencil = null;
            public Color4<Rgba> constantColor = new();
        }
        public PixelOps(Framebuffer fb_, Settings settings_) {
            fb = fb_;
            settings = settings_;

            bool defaultFb = fb.def != null;
            if (defaultFb) {
                if (fb.def!.colors.Count != settings.blends.Count()) {
                    throw new ArgumentOutOfRangeException($"Framebuffer/PixelOps color target mismatch");
                }
                if (fb.def.depth == null && settings.depth != null) {
                    throw new ArgumentOutOfRangeException($"Framebuffer/PixelOps depth unsupported");
                };
                if (fb.def.stencil == null && settings.stencil != null) {
                    throw new ArgumentOutOfRangeException($"Framebuffer/PixelOps stencil unsupported");
                };
            }
            List<ColorBuffer> db = new();
            ColorBuffer cb = ColorBuffer.ColorAttachment0;
            foreach (var blend in settings.blends) {
                if (blend != null) db.Add(cb++);
            }
            drawBuffers = db.ToArray();
            if (fb.def != null) {
                GL.NamedFramebufferDrawBuffers(fb.handle, drawBuffers.Length, in drawBuffers[0]);
            }
            var status = GL.CheckNamedFramebufferStatus(fb.handle, FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete) {
                //Reset();
                throw new ArgumentException($"Bad framebuffer {status}");
            }
            hasColor = drawBuffers.Length > 0;
        }
        public void Bind() {
            uint i = 0;
            foreach (var blend in settings.blends) {
                if (blend != null) {
                    blend.Bind(i);
                    ++i;
                }
            }
            settings.depth?.Bind();
            settings.stencil?.Bind();

            var (r, g, b, a) = settings.constantColor;
            GL.BlendColor(r, g, b, a);
            EnableCap.Blend.Enable(hasColor);
            EnableCap.DepthTest.Enable(settings.depth != null);
            EnableCap.StencilTest.Enable(settings.stencil != null);

            Clear();
        }
        // Clearing
        void ClearDepth(float depth) {
            GL.ClearNamedFramebuffer(fb.handle, GL_Buffer.Depth, 0, depth, 0);
        }
        void ClearStencil(uint stencil) {
            GL.ClearNamedFramebuffer(fb.handle, GL_Buffer.Stencil, 0, 0.0f, (int)stencil);
        }
        void ClearDepthStencil(float depth, uint stencil) {
            var GL_DEPTH_STENCIL = (GL_Buffer)0x84F9;
            GL.ClearNamedFramebuffer(fb.handle, GL_DEPTH_STENCIL, 0, depth, (int)stencil);
        }
        void ClearColor(int drawBuffer, Color4<Rgba> c) {
            GL.ClearNamedFramebufferf(fb.handle, GL_Buffer.Color, drawBuffer, in c.X);
        }
        public void Clear() {
            int drawBuffer = 0;
            foreach (var blend in settings.blends) {
                if (blend != null) {
                    if (blend.clear is Color4<Rgba> color) ClearColor(drawBuffer, color);
                    ++drawBuffer;
                }
            }
            var depth = settings.depth;
            var stencil = settings.stencil;
            bool hasDepth = depth != null;
            bool hasStencil = stencil != null;
            bool depthClear = hasDepth && depth!.clear.HasValue;
            bool stencilClear = hasStencil && stencil!.clear.HasValue;
            if (depthClear && stencilClear) ClearDepthStencil(depth!.clear!.Value, stencil!.clear!.Value);
            else if (depthClear) ClearDepth(depth!.clear!.Value);
            else if (stencilClear) ClearStencil(stencil!.clear!.Value);
        }
        Framebuffer fb;
        Settings settings;
        ColorBuffer[] drawBuffers;
        bool hasColor;
    }
}
