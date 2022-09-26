using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Common;
using System.Drawing;
using OpenTK.Mathematics;

namespace Argentian.Render {
    public class Renderer: Wrap.Disposable {
        public Renderer(Def def_) : base("Renderer") { 
            def = def_;
            GL.DebugMessageCallback(debugProc, IntPtr.Zero);
            // TODO: Put debug flags in the Def
            if (def.debugOutput) {
                GL.Enable(EnableCap.DebugOutput);
            }
            if (def.debugSynchronous) {
                GL.Enable(EnableCap.DebugOutputSynchronous);
            }
            GL.Enable(EnableCap.TextureCubeMapSeamless);
            GL.Enable(EnableCap.TextureRectangle);
            GL.Enable(EnableCap.FramebufferSrgb);
            // Use DirectX's much more sensible conventions
            GL.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.ZeroToOne);
        }
        public Renderer() : this(new Def()) {}
        public void Shutdown() { 
            GC.Collect(); // This can cause objects to be added to the delete queue
            GC.WaitForPendingFinalizers();
            ProcessDeleteQueue();
            Dispose();
        }
        public void Queue(Pass pass) {
            passes.Add(pass.Name, pass);
        }
        public void Draw(IGraphicsContext ctx, Size size) {
            Wrap.Framebuffer.sizes["Present"] = size; // stinky - this should come from Core

            active.AddRange(passes.Values);
            passes = new Dictionary<string, Pass>();
            active.Sort((Pass a, Pass b) => Math.Sign(a.Order - b.Order));

            foreach (var pass in active) {
                pass.PerFrameSetup();
                allPrims.AddRange(pass.Primitives);
            }
            foreach(var prim in allPrims.Distinct()) {
                prim.SetupGeometry();
            }
            foreach (var pass in active) {
                pass.Bind();
                foreach (var prim in pass.Primitives.OrderBy(x => x.Order)) {
                    if (pass.Prepare(prim)) {
                        prim.Draw();
                    }
                }
                pass.End();
            }
            active.Clear();
            allPrims.Clear();
        }

        public class Def {
            public bool debugOutput = true;
            public bool debugSynchronous = true;
            // public int multisample = 4;
        }
        public Def def;
        Dictionary<string, Pass> passes = new();
        // working members during draw
        List<Pass> active = new();
        List<Wrap.Primitive> allPrims = new();

        static Vert[] screenQuadVerts = {
            new Vert(new Vector4(-1.0f,  1.0f, 0.0f, 1.0f),   new Vector2(0.0f, 1.0f)),  // top left
            new Vert(new Vector4( 1.0f,  1.0f, 0.0f, 1.0f),   new Vector2(1.0f, 1.0f)),  // top right
            new Vert(new Vector4(-1.0f, -1.0f, 0.0f, 1.0f),   new Vector2(0.0f, 0.0f)),  // bottom left
            new Vert(new Vector4( 1.0f, -1.0f, 0.0f, 1.0f),   new Vector2(1.0f, 0.0f)),  // bottom right
        };
        static ushort[] screenQuadIndices = {
            0, 1, 2,
            1, 3, 2,
        };
        static Wrap.Primitive.StreamDef screenQuadStreams = new Wrap.Primitive.StreamDef {
            name = "main",
            stride = (uint)Marshal.SizeOf<Vert>(),
            attributes = {
                    new Wrap.Primitive.VertexAttrib("position", 4,0, VertexAttribType.Float, false ),
                    new Wrap.Primitive.VertexAttrib("texcoord", 2,16, VertexAttribType.Float, false ),
                },
        };
        static Wrap.TypedBuffer<Vert> screenQuadVB = new("vertices", screenQuadVerts);
        static Wrap.TypedBuffer<ushort> screenQuadIB = new("indices", screenQuadIndices);
        public static Wrap.Primitive Quad(string name, Wrap.ShaderProgram material) {
            var result = new Wrap.Primitive(name, material,
                new Wrap.Primitive.Def {
                    primitiveType = PrimitiveType.Triangles,
                    streams = { screenQuadStreams },
                },
                new[] { screenQuadVB },
                screenQuadIB);
            return result;
        }
        protected override void DisposeCLR() {
            screenQuadVB.Dispose();
            screenQuadIB.Dispose();
        }
        static DebugSeverity minSeverity = DebugSeverity.DebugSeverityLow;
        static DebugSeverity traceSeverity = DebugSeverity.DebugSeverityMedium;
        static GLDebugProc debugProc = DebugCallback;
        static void DebugCallback(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
            string? msg = Marshal.PtrToStringUTF8(message);
            if(msg != null) {
                var sev = severity.ToString().Substring("DebugSeverity".Length);
                var tp = type.ToString().Substring("DebugType".Length);
                string result = $"{tp} {sev} ({id:x8}):{msg}";
                if(severity <= traceSeverity) {
                    Trace.WriteLine(result);
                    Trace.Flush();
                } else if (severity <= minSeverity){ 
                    Debug.WriteLine(result);
                    Debug.Flush();
                } // else discard
            }
        }

        protected override void Delete() { }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct Vert {
        public Vert(Vector4 position_, Vector2 texcoord_) {
            position = position_;
            texcoord = texcoord_;
        }
        public Vector4 position;
        public Vector2 texcoord;
    }
}
