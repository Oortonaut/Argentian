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
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace Argentian {
    public abstract class Application : Disposable {
        protected GameWindow window;
        protected Framebuffer framebuffer;
        protected Renderer renderer;
        protected Stopwatch sw = Stopwatch.StartNew();
        protected DateTime startTime = DateTime.Now;
        protected DateTime Now => startTime + sw.Elapsed;
        public InputQueue InputQueue;

        protected Application(string title, Renderer inRenderer) : base(title) {
            renderer = inRenderer;
            InputQueue = new InputQueue();
            InitializeSamplers();
        }

        private void InitializeSamplers() {
            // TODO: Have a yaml file with all of the various default textures,
            // samplers, buffers, etc.

            Caches.SamplerDefs.Insert("nearest.samp", new Sampler.Def {
                magFilter = TextureMagFilter.Nearest,
                minFilter = TextureMinFilter.Nearest,
            });
        }

        public void AttachToWindow(GameWindow gameWindow) {
            window = gameWindow;
            window.UpdateFrame += TkUpdateFrame;
            window.RenderFrame += TkRenderFrame;
            window.TextInput += TkKeyChar;
            window.KeyDown += TkKeyDown;
            window.KeyUp += TkKeyUp;

            framebuffer = new Framebuffer("client"); // no def gives us the main framebuffer
        }

        public void DetachFromWindow() {
            if (framebuffer != null) {
                framebuffer.Dispose();
                framebuffer = null;
            }
            if (window != null) {
                window.KeyUp -= TkKeyUp;
                window.KeyDown -= TkKeyDown;
                window.TextInput -= TkKeyChar;
                window.RenderFrame -= TkRenderFrame;
                window.UpdateFrame -= TkUpdateFrame;
                window = null;
            }
        }

        protected virtual void UpdateFrame(double deltaTime) { }
        void TkUpdateFrame(FrameEventArgs e) {
            UpdateFrame(e.Time);
            renderer.UpdateFrame(e.Time);
        }

        protected virtual void RenderFrame(double deltaTime) { }
        void TkRenderFrame(FrameEventArgs e) {
            RenderFrame(e.Time);
            renderer.RenderFrame(e.Time, window!);
        }

        protected virtual void KeyDown(Keys keys, KeyModifiers modifiers, int scanCode, bool isRepeat) { }
        void TkKeyDown(KeyboardKeyEventArgs e) { KeyDown(e.Key, e.Modifiers, e.ScanCode, e.IsRepeat); }

        protected virtual void KeyUp(Keys keys, KeyModifiers modifiers, int scanCode) { }
        void TkKeyUp(KeyboardKeyEventArgs e) { KeyUp(e.Key, e.Modifiers, e.ScanCode); }

        protected virtual void KeyChar(char c, string text) { }
        void TkKeyChar(TextInputEventArgs e) { KeyChar((char)e.Unicode, e.AsString); }

        protected override void QueueDelete() {
            base.QueueDelete();
            DetachFromWindow();
        }

        protected override void DisposeCLR() {
        }
        protected override void Delete() {
        }
    }
}
