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
using Argentian.Render.Prims;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Argentian {
    public class TilemapDemo : Application {
        private RenderPrimitive tilemapPrim;
        private RenderPass tilemapPass;

        public TilemapDemo(string title, Renderer renderer) : base(title, renderer) {
            // Load tilemap shader program from YAML file
            var tilemapProgram = Caches.NewShaderProgram("tilemap.mat");

            int W = 16, H = 16;
            // Setup primitive
            var tilemapDef = new TilemapPrimitive.Def {
                primitiveType = PrimitiveType.Triangles,
                vertexStreams = [renderer.screenQuadStream],
                textBuffer = new TilemapPrimitive.TextBuf {
                    flags = TilemapPrimitive.TilemapFlags.WrapX | TilemapPrimitive.TilemapFlags.WrapY,
                    bufferSize = new Vector2i(W, H),
                    cellSize = new Vector2i(32, 32),
                },
                fonts = [
                    new TilemapPrimitive.FontDef {
                        tileCount = new Vector2i(5, 24),
                        tileSize = new Vector2i(32, 32),
                        tileScale = new Vector2(1, 1),
                        texture = Caches.Textures.Get("BadAttitudeTiles.png"),
                        sampler = Caches.Samplers.Get("nearest.samp"),
                    },
                    new TilemapPrimitive.FontDef {
                        tileCount = new Vector2i(12, 32),
                        tileSize = new Vector2i(32, 32),
                        tileScale = new Vector2(1, 1),
                        texture = Caches.Textures.Get("NewTown.bmp"),
                        sampler = Caches.Samplers.Get("nearest.samp"),
                    }
                ],
            };
            //tilemapPrim = renderer.Quad("tilemap.prim", tilemapProgram);
            tilemapPrim = new TilemapPrimitive($"{title} primitive", renderer, tilemapProgram, tilemapDef);

            // Setup uniforms for tilemap (you'll need to set these based on your needs)
            // tilemapPrim.Shader.SetUniform("vp.origin", new Vector2(0, 0));
            // tilemapPrim.Shader.SetUniform("vp.size", new Vector2(80, 25));
            // tilemapPrim.Shader.SetUniform("buf.cellSizePixels", new Vector2(80, 25));
            // etc.

            // Setup render pass
            tilemapPass = new RenderPass {
                name = "tilemap",
                def = {
                    order = 0,
                    settings = {
                        blends = {
                            new BlendUnit(BlendUnit.Write, Color4.White)
                        },
                    },
                },
                prims = { tilemapPrim },
            };
        }

        protected override void RenderFrame(double deltaTime) {
            tilemapPrim.Shader.SetUniform("time", (double)(sw.ElapsedMilliseconds * 0.001));

            renderer.Queue(tilemapPass);
            base.RenderFrame(deltaTime);
        }

        protected override void Delete() {
            tilemapPrim?.Dispose();
            base.Delete();
        }

    }
}
