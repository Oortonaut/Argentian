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
            framebuffer = fb_;
            int framebufferHandle = framebuffer.handle.Handle;
            settings = settings_;

            bool defaultFb = framebuffer.def != null;
            if (defaultFb) {
                if (framebuffer.def!.colors.Count != settings.blends.Count()) {
                    throw new ArgumentOutOfRangeException($"Framebuffer/PixelOps color target mismatch");
                }
                if (framebuffer.def.depth == null && settings.depth != null) {
                    throw new ArgumentOutOfRangeException($"Framebuffer/PixelOps depth unsupported");
                };
                if (framebuffer.def.stencil == null && settings.stencil != null) {
                    throw new ArgumentOutOfRangeException($"Framebuffer/PixelOps stencil unsupported");
                };
            }
            List<ColorBuffer> db = new();
            ColorBuffer cb = ColorBuffer.ColorAttachment0;
            foreach (var blend in settings.blends) {
                if (blend != null) db.Add(cb++);
            }
            drawBuffers = db.ToArray();
            if (framebuffer.def != null) {
                GL.NamedFramebufferDrawBuffers(framebufferHandle, drawBuffers.Length, in drawBuffers[0]);
            }
            var status = GL.CheckNamedFramebufferStatus(framebufferHandle, FramebufferTarget.Framebuffer);
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
            GL.ClearNamedFramebuffer(framebuffer.handle.Handle, GL_Buffer.Depth, 0, depth, 0);
        }
        void ClearStencil(uint stencil) {
            GL.ClearNamedFramebuffer(framebuffer.handle.Handle, GL_Buffer.Stencil, 0, 0.0f, (int)stencil);
        }
        void ClearDepthStencil(float depth, uint stencil) {
            GL.ClearNamedFramebuffer(framebuffer.handle.Handle, (GL_Buffer)All.DepthStencil, 0, depth, (int)stencil);
        }
        unsafe void ClearColor(int drawBuffer, Color4<Rgba> c) {
            GL.ClearNamedFramebufferfv(framebuffer.handle.Handle, GL_Buffer.Color, drawBuffer, &c.X);
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
        Framebuffer framebuffer;
        Settings settings;
        ColorBuffer[] drawBuffers;
        bool hasColor;
    }
}
