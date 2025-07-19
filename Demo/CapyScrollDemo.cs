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
    public class CapyScrollDemo : Application {
        private RenderPrimitive stretchPrim;
        private RenderPass stretchPass;

        public CapyScrollDemo(string title, Renderer renderer) : base(title, renderer) {
            // Initialize shader program
            Caches.ShaderProgramDefs.Insert("2d.mat", new ShaderProgram.Def {
                vertex = { "2d.vert.gl" },
                fragment = { "2d.frag.gl" },
            });

            // Create resources
            var stretchProgram = Caches.NewShaderProgram("2d.mat");
            var capybarasImage = Caches.Textures.Get("capybaras.png");
            var samplerBilinear = new Sampler("bilinear.samp", new Sampler.Def {
                magFilter = TextureMagFilter.Linear,
                minFilter = TextureMinFilter.LinearMipmapLinear,
            });

            // Setup primitive
            stretchPrim = renderer.Quad("stretch.prim", stretchProgram);
            stretchPrim.Shader.SetTexture("tex", capybarasImage, samplerBilinear);

            // Setup render pass
            stretchPass = new RenderPass {
                name = "stretch",
                def = {
                    order = 0,
                    settings = {
                        blends = {
                            new BlendUnit(BlendUnit.Write, Color4.Red)
                        },
                    },
                },
                prims = { stretchPrim },
            };
        }

        protected override void RenderFrame(double deltaTime) {
            stretchPrim.Shader.SetUniform("time", (float)(sw.ElapsedMilliseconds * 0.001f));

            renderer.Queue(stretchPass);
        }
        protected override void Delete() {
            stretchPrim?.Dispose();
        }
    }
}
