using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Argentian.Engine;
using Argentian.Roguelike;
using Argentian.Wrap;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.PixelFormats;

namespace Argentian.Render.Prims {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Cell {
        public ushort data;
        public ushort fg; // 4 4 4 4
        public ushort bg; // 4 4 4 4
        public byte flags;
        public byte stencil;
    };
    public struct LandscapeCell {
        public ushort layer0;
        public ushort layer1;
        public ushort layer2;
        public ushort lighting; // 4 4 4 4 at corners UL\UR\LL\LR
    };
    [Flags]
    public enum TilemapFlags: uint {
        None = 0,
        WrapX = 1 << 0,
        WrapY = 1 << 1,
    }
    public class TilemapPrimitive: RenderPrimitive {

        public TilemapPrimitive(
            string name_,
            Renderer renderer,
            IShaderProgram shader_,
            Def format_): base(name_, shader_, format_.Conform(), [renderer.screenQuadVB], renderer.screenQuadIB) {
            def = format_;
            viewportOrigin = def.viewportOrigin;
        }
        // Dynamic

        // Static information about the spacing and indexing
        public class TextBuf {
            public TilemapFlags flags;
            public Vector2i bufferSize; // size of the buffer in cells

            public Vector2i cellSize;

            // cursor display
            public Vector2i cursorPos; // position of the cursor in the buffer

            public Vector3 cursorColor; // color of the cursor

            // dynamic scrolling    TextBuf <-> Viewport
            public Vector2 cellOffset; // window UL corner in cells

            internal void Bind(string prefix, IShaderProgram shader) {
                // See tilemap.h.gl
                shader.SetUniform($"{prefix}.flags", (uint)flags);
                // Set buffer uniforms
                shader.SetUniform($"{prefix}.bufferSize", bufferSize);
                shader.SetUniform($"{prefix}.cellSize", cellSize);
                // Set cursor uniforms
                shader.SetUniform($"{prefix}.cursorPos", cursorPos);
                shader.SetUniform($"{prefix}.cursorColor", cursorColor);
                // Set cell offset uniforms
                shader.SetUniform($"{prefix}.cellOffset", cellOffset);
            }

            public TextBuf Conform() { return this;  }
        }

        public class FontDef {
            // locate tiles inside texture
            public Vector2i tileSize; // source size
            public Vector2i tileOrigin; // offset inside texture / top left spacing
            public Vector2i tileGap; // includes any gap between tiles; from the left of the first tile to the left of the next, not including origin

            public Vector2i tileCount; // number of tiles in each direction. calculated if non-positive.

            // final position in cell
            public Vector2 tileOffset; // offset the font in the cell

            public Vector2 tileScale; // scale the font in the cell (for example, using a small font in a larger cell or shrinking to fit in a corner), final size

            // texture and sampler
            public Texture texture;
            public Sampler sampler;

            internal void Bind(string prefix, IShaderProgram shader) {
                shader.SetUniform($"{prefix}.tileSize", tileSize);
                shader.SetUniform($"{prefix}.tileOrigin", tileOrigin);
                shader.SetUniform($"{prefix}.tileGap", tileGap);
                shader.SetUniform($"{prefix}.tileCount", tileCount);
                shader.SetUniform($"{prefix}.tileOffset", tileOffset);
                shader.SetUniform($"{prefix}.tileScale", tileScale);
                shader.SetUniform($"{prefix}.fontSize", texture.Size);

                shader.SetTexture($"{prefix}.texture", texture, sampler);
            }

            public FontDef Conform() {
                if (tileCount.X <= 0 || tileCount.Y <= 0) {
                    tileCount = (texture.Size - tileOrigin) / (tileSize + tileGap);
                }
                return this;
            }
        }
        public new class Def: RenderPrimitive.Def {
            // dynamic defaults
            public Vector2 viewportOrigin = Vector2.Zero;

            public TextBuf textBuffer = new TextBuf {
                bufferSize = new Vector2i(40, 25), cursorPos = new Vector2i(1, -1), cursorColor = Vector3.One,
            };

            public FontDef[] fonts = []; // Initialize array for up to 8 fonts

            public override Def Conform() {
                for (int i = 0; i < fonts.Length; ++i) {
                    fonts[i] = fonts[i].Conform();
                }
                textBuffer = textBuffer.Conform();
                return base.Conform() as Def;
            }
        }

        public Vector2 viewportOrigin = Vector2.Zero;
        public new Def def;
        public TextBuf textBuffer => def.textBuffer;
        public FontDef[] fonts => def.fonts;
        private Layer<byte> corners = null;
        //private Cell[,]? cells = null;
        public MappedBuffer<Cell>? cellsBuffer = null;

        public void SetOrigin(Vector2 origin) {
            viewportOrigin = origin;
        }
        public void SetCells(Layer<Cell> layer) {
            if (cellsBuffer == null) {
                cellsBuffer = new MappedBuffer<Cell>($"{Name} cells buffer", layer.size.X * layer.size.Y);
            }

            var from = layer.Map;
            var to = cellsBuffer.Map;
            from.CopyTo(to);
        }

        public override void BindDraw() {
            var shader = Shader;

            // Set viewport uniforms
            shader.SetUniform("vp.origin", def.viewportOrigin);
            shader.SetUniform("vp.size", Framebuffer.sizes["client"]);

            // Set font uniforms
            for (int i = 0; i < def.fonts.Length; i++) {
                var font = def.fonts[i];
                if (font.texture != null) {
                    font.Bind($"font[{i}]", shader);
                }
            }

            // THIS IS PERFORMANCE!
            shader.SetShaderStorageBlock($"cells", cellsBuffer);
            def.textBuffer.Bind("buf", shader);

            base.BindDraw();
        }
    }
}
