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
    public static partial class Extensions {
        static Dictionary<EnableCap, bool> cmdList_enableState = new();
        public static void Enable(this EnableCap cap, bool e) {
            if (cmdList_enableState.TryGetValue(cap, out var curr) &&
                curr == e) {
                return;
            }

            cmdList_enableState[cap] = e;
            if (e) {
                GL.Enable(cap);
            } else {
                GL.Disable(cap);
            }
        }
        static CullFaceMode cfm = default;
        public static void Set(this CullFaceMode mode) {
            if (mode != cfm) {
                cfm = mode;
                GL.CullFace(mode);
            }
        }
    }
    public class Framebuffer: Disposable {
        public class Def {
            public List<Attachment> colors = new();
            public Attachment? depth = null;
            public Attachment? stencil = null;
        }
        public Framebuffer(string name, Def def) : base(name) {
            Construct(def);
        }
        public Framebuffer(string name) : base(name) {
            Construct((Def?)null);
        }

        // Attachmenty stuff
        public Def? def = null;
        public void Bind() {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
        }
        public void Construct(Def? def_) {
            def = def_;

            if (def != null) {
                handle = GL.CreateFramebuffer();
                GL.ObjectLabel(ObjectIdentifier.Framebuffer, (uint)handle.Handle, Name.Length, Name);

                var attachment = FramebufferAttachment.ColorAttachment0;
                foreach (var surface in def.colors) {
                    surface.Bind(handle, attachment++);
                }
            }
        }
        public FramebufferHandle handle = default;
        protected override void Delete() {
            if (handle != default) 
                GL.DeleteFramebuffer(handle);
        }
        public override string ToString() => $"Framebuffer {handle.Handle} '{Name}'{GetSize()}{DisposedString}";
        public Size GetSize() {
            if (def == null) {
                return sizes["Present"];
            } else {
                return def.depth?.GetSize() ??
                    def.stencil?.GetSize() ??
                    (def.colors.Count > 0 ? def.colors[0].GetSize() : Size.Empty);
            }
        }
        // Setup and drawbuffers
        public static Dictionary<string, Size> sizes = new();
        public static void Reset() {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, default);
        }
    }
}
