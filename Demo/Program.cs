using Argentian.Wrap;
using Argentian.Engine;
using Argentian.Render;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Input;
using System.Drawing;
using OpenTK.Windowing.Common;

namespace Argentian.Demo {
    public static partial class Extensions {
        public static Size ToSize(this Vector2i v) {
            return new Size(v.X, v.Y);
        }
    }
    internal class Program: Disposable {
        static void Main(string[] args) {
            var root = Core.Config.FindRoot();
            Core.Config.Initialize(new string[] { "--root", root });
            var title = Core.Config.GetString("--title", "Argentian Demo");

            using var p = new Program(title);
            p.Run();
        }
        Program(string title) : base(title) {
            var nws = NativeWindowSettings.Default;
            nws.Size = Core.Config.GetInt2("--size", 1280, 720);
            nws.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            nws.APIVersion = new Version(4, 6);
            nws.Flags = OpenTK.Windowing.Common.ContextFlags.Debug | OpenTK.Windowing.Common.ContextFlags.ForwardCompatible;
            nws.Title = title;
            nws.Profile = ContextProfile.Compatability;
            window = new GameWindow(GameWindowSettings.Default, nws);
            renderer = new Renderer();

            Caches.SamplerDefs.Insert("nearest.samp", new Sampler.Def {
                magFilter = TextureMagFilter.Nearest,
                minFilter = TextureMinFilter.Nearest,
            });
            // Caches.SamplerDefs.Insert("bilinear.samp", new Sampler.Def {
            //     magFilter = TextureMagFilter.Linear,
            //     minFilter = TextureMinFilter.LinearMipmapLinear,
            // });
            // var samplerBilinear = Caches.Samplers.Get("bilinear.samp");

            Caches.ShaderProgramDefs.Insert("2d.mat", new ShaderProgram.Def {
                vertex = { "2d.vert.gl" },
                fragment = { "2d.frag.gl" },
            });
            // No `using` for these because they are being passed to the
            // pass, primitive, and texture bindings which will dispose them.
            var stretchProgram = Caches.NewShaderProgram("2d.mat");
            var capybarasImage = Caches.Textures.Get("capybaras.png");
            var samplerBilinear = new Sampler("bilinear.samp", new Sampler.Def {
                magFilter = TextureMagFilter.Linear,
                minFilter = TextureMinFilter.LinearMipmapLinear,
            });

            stretchPrim = Renderer.Quad("stretch.prim", stretchProgram);
            stretchPrim.Shader.SetTexture("tex", capybarasImage, samplerBilinear);

            // first pass pass

            stretchPass = new Pass {
                name = "stretch",
                def = {
                    order = 0,
                    settings = {
                        blends = {
                            new BlendUnit(BlendUnit.Write, Color4.Red)
                        },
                        // depth = new DepthUnit { function = DepthFunction.Always, write = false, clear = 1.0f },
                        // stencil = new StencilUnit { clear = uint.MaxValue },
                    },
                },
                prims = { stretchPrim },
            };
        }
        void Run() {
            window.UpdateFrame += UpdateFrame;
            window.RenderFrame += RenderFrame;
            window.TextInput += KeyChar;
            window.KeyDown += KeyDown;
            window.KeyUp += KeyUp;

            window.Run();

            window.KeyUp -= KeyUp;
            window.KeyDown -= KeyDown;
            window.TextInput -= KeyChar;
            window.RenderFrame -= RenderFrame;
            window.UpdateFrame -= UpdateFrame;

            GC.Collect(); // This can cause objects to be added to the delete queue
            GC.WaitForPendingFinalizers();
            Disposable.ProcessDeleteQueue();
        }

        private void KeyUp(KeyboardKeyEventArgs obj) {
        }

        private void KeyDown(KeyboardKeyEventArgs obj) {
        }

        Primitive stretchPrim;
        Pass stretchPass;
        void UpdateFrame(FrameEventArgs e) {
        }
        Stopwatch sw = Stopwatch.StartNew();
        void RenderFrame(FrameEventArgs e) {
            stretchPrim.Shader.SetUniform("time", (float)(sw.ElapsedMilliseconds * 0.001f));

            renderer.Queue(stretchPass);
            renderer.Draw(window.Context, window.ClientSize.ToSize());
            window.Context.SwapBuffers();
            Disposable.ProcessDeleteQueue();
        }

        void KeyChar(TextInputEventArgs e) {
            if (e.Unicode == 'x') {
                window.Close();
            }
        }
        protected override void DisposeCLR() {
            renderer.Dispose();
            window.Dispose();
        }
        protected override void Delete() {
            stretchPrim.Dispose();
        }
        GameWindow window;
        Renderer renderer;
    }
}
