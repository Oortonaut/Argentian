using System.Collections.Generic;
using System.Linq;
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
    //     public static readonly RootSize Present = Make("client");
    // 
    //     static RootSize Make(string s) => (RootSize)s.Atom();
    // }
    public interface IPass {
        string Name { get; }
        void UpdateFrame(double deltaTime);
        bool SetupFrame(Renderer renderer);
        void BindTarget();
        void Bind(IPrimitive prim);
        void EndFrame();
        Vector2i GetSize();
        bool Matches(IPrimitive prim);
        IEnumerable<IPrimitive> Primitives { get; }
    }
    public class RenderPass: IPass {
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
        public Framebuffer framebuffer = new Framebuffer("client");
        public List<RenderPrimitive> prims = new List<RenderPrimitive>();
        public string Name => name;
        public long Order => def.order;
        public PixelOps? blends = null;
        public virtual void UpdateFrame(double deltaTime) { }
        public virtual bool SetupFrame(Renderer renderer) {
            if (blends == null) {
                blends = new(framebuffer, def.settings);
            }
            return true;
        }
        public virtual void BindTarget() {
            framebuffer.Bind();
            blends!.Bind();
            Vector2i size = framebuffer.GetSize();

            // viewport
            if(def.viewport != null) {
                GL.ViewportIndexedf(0,
                    def.viewport.Value.X,
                    def.viewport.Value.Y,
                    def.viewport.Value.Z,
                    def.viewport.Value.W);
            } else {
                GL.ViewportIndexedf(0, 0, 0, size.X, size.Y);
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
        PassFlags mask = PassFlags.None;
        PassFlags test = PassFlags.None;
        public void Bind(IPrimitive prim_) {
            // Do some work to avoid switching culling mode
            if (prim_ is RenderPrimitive prim) {
                var newCull = (GlCull)((int)def.cullMode * (int)prim.def.windingMode);
                if (currCull != newCull) {
                    switch (newCull) {
                    case GlCull.Back:
                        EnableCap.CullFace.Enable(true);
                        TriangleFace.Back.Set();
                        break;
                    case GlCull.None:
                        EnableCap.CullFace.Enable(false);
                        break;
                    case GlCull.Front:
                        EnableCap.CullFace.Enable(true);
                        TriangleFace.Front.Set();
                        break;
                    }
                    currCull = newCull;
                }
            }
        }
        // This will likely be devirtualized
        public bool Matches(IPrimitive prim_) {
            return (prim_.Flags & mask) == test;
        }
        public IEnumerable<IPrimitive> Primitives => prims;
        public virtual void EndFrame() {
            // TODO: Make default objects for textures and samplers
            // since GL doesn't allow unbinding
            // for (uint unit = 0; unit < 16; ++unit) {
            //     GL.BindTextureUnit(unit, default);
            //     GL.BindSampler(unit, default);
            // }
        }
        public Vector2i GetSize() => framebuffer.GetClientSize();
    }
}
