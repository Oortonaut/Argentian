using System.Collections.Generic;
using Argentian.Wrap;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Drawing;

namespace Argentian.Render {
    public struct XorShiftStar32 {
        public XorShiftStar32() {}
        public uint x = 188888881;
        public uint Step(int delta = 10501) {
            x += (uint)delta;
            x *= 124000421;

            x ^= x << 13;
            x ^= x >> 21;
            x ^= x << 11;

            return x;
        }
        public float StepFloat(int delta = 10501) => Step(delta) / 4294967296.0f;
    };
    public static class StringExtensionMethods {
        public static uint Atom(this string str) {
            unchecked {
                var rand = new XorShiftStar32();
                foreach (var c in str) {
                    rand.Step(c);
                }
                return rand.x;
            }
        }
    }
    // public enum RootSize { }
    // public static partial class RootSizes {
    //     public static readonly RootSize Render = Make("Render");
    //     public static readonly RootSize Present = Make("Present");
    // 
    //     static RootSize Make(string s) => (RootSize)s.Atom();
    // }
    public class Pass {
        public class Def {
            public PixelOps.Settings settings = new();

            public int order = 0;

            public Vector4? viewport;
            public Cull cullMode = Cull.Back;
            public enum Cull {                                             
                Front = 1,
                Off = 0,
                Back = -1,
            }
        }
        public string name = "";
        public Def def = new();
        public Framebuffer framebuffer = new Framebuffer("Default FB");
        public List<Primitive> prims = new List<Primitive>();
        public string Name => name;
        public long Order => def.order;
        PixelOps? blends = null;
        public virtual void PerFrameSetup() {
            if (blends == null) {
                blends = new(framebuffer, def.settings);
            }
        }
        public virtual void Bind() {
            framebuffer.Bind();
            blends!.Bind();
            Size size = framebuffer.GetSize();

            // viewport
            if(def.viewport != null) {
                GL.ViewportIndexedf(0,
                    def.viewport.Value.X,
                    def.viewport.Value.Y,
                    def.viewport.Value.Z,
                    def.viewport.Value.W);
            } else {
                GL.ViewportIndexedf(0, 0, 0, size.Width, size.Height);
            }
            currCull = GlCull.Dirty;
        }
        GlCull currCull = GlCull.Dirty;
        enum GlCull {
            Dirty = -2,
            Back = -1,
            None = 0,
            Front = 1,
        }

        public bool Prepare(Primitive prim) {
            var newCull = (GlCull)((int)def.cullMode * (int)prim.format.windingMode);
            if(currCull != newCull) {
                switch(newCull) {
                case GlCull.Back:
                    EnableCap.CullFace.Enable(true);
                    CullFaceMode.Back.Set();
                    break;
                case GlCull.None:
                    EnableCap.CullFace.Enable(false);
                    break;
                case GlCull.Front:
                    EnableCap.CullFace.Enable(true);
                    CullFaceMode.Front.Set();
                    break;
                }
                currCull = newCull;
            }
            return true;
        }
        public IEnumerable<Primitive> Primitives => prims;
        public virtual void End() {
            // for (uint unit = 0; unit < 16; ++unit) {
            //     GL.BindTextureUnit(unit, default);
            //     GL.BindSampler(unit, default);
            // }
        }
    }
}
