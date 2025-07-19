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
using Argentian.Wrap;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Argentian.Render {
    public static partial class Extensions {
        public static Size ToSize(this Vector2i v) {
            return new Size(v.X, v.Y);
        }
        public static Vector2i ToVector2i(this Size s) {
            return new Vector2i(s.Width, s.Height);
        }
        public static Vector2 ToVector2(this Size s) {
            return new Vector2(s.Width, s.Height);
        }
        public static Vector2 ToVector2(this Vector2i v) {
            return new Vector2(v.X, v.Y);
        }
    }
    public class Renderer: Wrap.Disposable {

        #region Configuration

        public Def def { get; private set; }
        public class Def {
            public bool debugOutput = true;
            public bool debugSynchronous = true;

            // public int multisample = 4;

            // Debug settings copied to static debug info
            public DebugSeverity minSeverity = DebugSeverity.DebugSeverityLow;
            public DebugSeverity traceSeverity = DebugSeverity.DebugSeverityNotification;
        }

        #endregion

        #region Event Handling

        public void UpdateFrame(double deltaTime) {
            foreach (var pass in framePasses.Values) {
                pass.UpdateFrame(deltaTime);
            }
        }
        public void RenderFrame(double deltaTime, GameWindow window) {
            Framebuffer.sizes["client"] = window.ClientSize;
            DrawFrame(window.Context);
            window.Context.SwapBuffers();
            Disposable.ProcessDeleteQueue();
        }

        #endregion

        #region Lifetime

        public Renderer(Def def_): base("Renderer") {
            def = def_;
            GenerateQuadGeometry();
            SetupGL();
            SetupGLDebug();
        }
        public Renderer(): this(new Def()) { }
        public void Shutdown() {
            GL.Disable(EnableCap.DebugOutput);
            GC.Collect(); // This can cause objects to be added to the delete queue
            GC.WaitForPendingFinalizers();
            ProcessDeleteQueue();
            Dispose();
        }

        void SetupGL() {
            GL.Enable(EnableCap.TextureCubeMapSeamless);
            GL.Enable(EnableCap.TextureRectangle);
            GL.Enable(EnableCap.FramebufferSrgb);
            // Use DirectX's much more sensible conventions
            GL.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.ZeroToOne);
        }

        #endregion

        #region Frame

        // Add a pass to be rendered. It will be ordered according to its dependencies.
        public void Queue(RenderPass pass) {
            framePasses.Add(pass.Name, pass);
        }

        // working members during draw
        List<IPass> DrawFrame_passes = new();
        List<IPrimitive> DrawFrame_prims = new();
        void DrawFrame(IGraphicsContext ctx) {
            DrawFrame_passes.AddRange(framePasses.Values);
            framePasses = new Dictionary<string, RenderPass>();
            //DrawFrame_passes.Sort((RenderPass a, RenderPass b) => Math.Sign(a.Order - b.Order));
            // TODO: Topological sort


            foreach (var pass in DrawFrame_passes) {
                if (pass.SetupFrame(this)) {
                    DrawFrame_prims.AddRange(pass.Primitives);
                }
            }
            foreach (var prim in DrawFrame_prims.Distinct()) {
                prim.BindDraw();
            }
            foreach (var pass in DrawFrame_passes) {
                pass.BindTarget();
                foreach (var prim in pass.Primitives.OrderBy(x => x.Order)) {
                    if (pass.Matches(prim)) {
                        pass.Bind(prim);
                        prim.SubmitDraw();
                    }
                }
                pass.EndFrame();
            }
            DrawFrame_passes.Clear();
            DrawFrame_prims.Clear();
        }

        // The passes to be drawn this frame. New passes need to be added every frame.
        Dictionary<string, RenderPass> framePasses = new();

        #endregion

        #region Screen Quads

        void GenerateQuadGeometry() {
            screenQuadVB = new TypedBuffer<VertPosUV>("vertices", screenQuadVerts);
            screenQuadIB = new TypedBuffer<ushort>("indices", screenQuadIndices);
        }

        static VertPosUV[] screenQuadVerts = {
            new VertPosUV(new Vector4(-1.0f, 1.0f, 0.0f, 1.0f), new Vector2(0.0f, 1.0f)), // top left
            new VertPosUV(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), new Vector2(1.0f, 1.0f)), // top right
            new VertPosUV(new Vector4(-1.0f, -1.0f, 0.0f, 1.0f), new Vector2(0.0f, 0.0f)), // bottom left
            new VertPosUV(new Vector4(1.0f, -1.0f, 0.0f, 1.0f), new Vector2(1.0f, 0.0f)), // bottom right
        };

        static ushort[] screenQuadIndices = {
            0, 1, 2, 1, 3, 2,
        };

        public RenderPrimitive.StreamDef screenQuadStream = new RenderPrimitive.StreamDef {
            name = "main",
            stride = (uint)Marshal.SizeOf<VertPosUV>(),
            attributes = {
                new RenderPrimitive.VertexAttrib("position", 4, 0, VertexAttribType.Float, false), new RenderPrimitive.VertexAttrib("texCoord", 2, 16, VertexAttribType.Float, false),
            },
        };

        public TypedBuffer<VertPosUV>? screenQuadVB;
        public TypedBuffer<ushort>? screenQuadIB;
        public RenderPrimitive.Def ScreenQuadPrimitiveDef => new RenderPrimitive.Def {
            primitiveType = PrimitiveType.Triangles,
            vertexStreams = {
                screenQuadStream
            },
        };

        public Wrap.RenderPrimitive Quad(string name, ShaderProgram material) {
            var result = new RenderPrimitive(name, material, ScreenQuadPrimitiveDef, [screenQuadVB!], screenQuadIB!);
            return result;
        }
        protected override void DisposeCLR() {
            screenQuadVB!.Dispose();
            screenQuadIB!.Dispose();
        }

        #endregion

        #region Debug

        static GLDebugProc? DebugCallbackProc;
        static DebugSeverity minSeverity = DebugSeverity.DebugSeverityHigh;
        static DebugSeverity traceSeverity = DebugSeverity.DebugSeverityHigh;

        void SetupGLDebug() {
            // TODO: Put debug flags in the Def
            if (def.debugOutput) {
                DebugCallbackProc = DebugCallback;
                GL.DebugMessageCallback(DebugCallbackProc, IntPtr.Zero);
                GL.Enable(EnableCap.DebugOutput);
            }
            if (def.debugSynchronous) {
                GL.Enable(EnableCap.DebugOutputSynchronous);
            }
            // Lol GL severity numbers are ordered in reverse
            if (def.minSeverity >= minSeverity) {
                minSeverity = def.minSeverity;
            }
            if (def.traceSeverity >= traceSeverity) {
                traceSeverity = def.traceSeverity;
            }
        }
        static void DebugCallback(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
            string? msg = Marshal.PtrToStringUTF8(message);
            if (msg != null) {
                var sev = severity.ToString().Substring("DebugSeverity".Length);
                var tp = type.ToString().Substring("DebugType".Length);
                string result = $"{tp} {sev} ({id:x8}):{msg}\n";
                if (severity <= traceSeverity) {
                    switch (severity) {
                    case DebugSeverity.DebugSeverityHigh: Trace.TraceError(result); break;
                    case DebugSeverity.DebugSeverityMedium: Trace.TraceWarning(result); break;
                    default: Trace.TraceInformation(result); break;
                    }
                    if (DebugFlush) Trace.Flush();
                }
                if (severity <= minSeverity) {
                    Debug.Write(result);
                    Console.Write(result);
                    if (DebugFlush) Debug.Flush();
                }
            }
        }
        public static bool DebugFlush = false;

        #endregion

        protected override void Delete() { }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertPosUV {
        public VertPosUV(Vector4 position_, Vector2 texcoord_) {
            position = position_;
            texCoord = texcoord_;
        }
        public Vector4 position;
        public Vector2 texCoord;
    }
}
