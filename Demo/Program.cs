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

namespace Argentian {

    internal class Program: Disposable {
        GameWindow window;
        Renderer renderer;
        private Application app;

        private void InitializeWindow(string title) {
            var nws = NativeWindowSettings.Default;
            nws.Size = Core.Config.GetInt2("--size", 1280, 720);
            nws.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            nws.APIVersion = new Version(4, 6);
            nws.Flags = OpenTK.Windowing.Common.ContextFlags.Debug | OpenTK.Windowing.Common.ContextFlags.ForwardCompatible;
            nws.Title = title;
            nws.Profile = ContextProfile.Core;
            window = new GameWindow(GameWindowSettings.Default, nws);
        }


        Program(string title): base(title) {
            InitializeWindow(title);
            renderer = new Renderer();

            //app = new CapyScrollDemo(title, renderer);
            app = new TilemapDemo(title, renderer, new Vector3i(128, 128, 16));
            app.AttachToWindow(window);
        }
        void Run() {
            window.Run();

            GC.Collect(); // This can cause objects to be added to the delete queue
            GC.WaitForPendingFinalizers();
        }
        protected override void QueueDelete() {
            base.QueueDelete();
        }
        protected override void DisposeCLR() {
            renderer.Dispose();
            window.Dispose();
            app.Dispose();
        }
        protected override void Delete() {
        }

        static void Main(string[] args) {
            var root = Core.Config.FindRoot();
            Core.Config.Initialize(new string[] { "--root", root });
            var title = Core.Config.GetString("--title", "Argentian Demo");

            var program = new Program(title);
            program.Run();
        }
    }
}
