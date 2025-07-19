using System;
using System.Runtime.InteropServices;
using Argentian.Wrap;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Argentian.Render.Prims {
    public class TilemapPrimitive: RenderPrimitive {

        public TilemapPrimitive(
            string name_,
            Renderer renderer,
            ShaderProgram shader_,
            Def format_): base(name_, shader_, format_, [renderer.screenQuadVB], renderer.screenQuadIB) {
            def = format_;
            viewportOrigin = def.viewportOrigin;
        }
        // Dynamic
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Cell {
            public ushort data;
            public ushort fg; // 4 4 4 4
            public ushort bg; // 4 4 4 4
            public byte flags;
            public byte stencil;
        };

        [Flags]
        public enum TilemapFlags: uint {
            WrapX = 1 << 0,
            WrapY = 1 << 1,
        }

        // Static information about the spacing and indexing
        public struct TextBuf {
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
        }

        public struct FontDef {
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
                // locate tiles
                shader.SetUniform($"{prefix}.tileSize", tileSize);
                shader.SetUniform($"{prefix}.tileOrigin", tileOrigin);
                shader.SetUniform($"{prefix}.tileGap", tileGap);
                shader.SetUniform($"{prefix}.tileCount", tileCount);
                // final position in cell
                shader.SetUniform($"{prefix}.tileOffset", tileOffset);
                shader.SetUniform($"{prefix}.tileScale", tileScale);
                shader.SetUniform($"{prefix}.fontSize", texture.Size);

                // Set texture uniforms
                shader.SetTexture($"{prefix}.texture", texture, sampler);
            }
        }
        public new class Def: RenderPrimitive.Def {
            // dynamic defaults
            public Vector2 viewportOrigin = Vector2.Zero;

            public TextBuf textBuffer = new TextBuf {
                bufferSize = new Vector2i(132, 80), cursorPos = new Vector2i(1, -1), cursorColor = Vector3.One,
            };

            public FontDef[] fonts = new FontDef[8]; // Initialize array for up to 8 fonts
        }

        public Vector2 viewportOrigin = Vector2.Zero;
        public new Def def;
        public TextBuf textBuffer => def.textBuffer;
        public FontDef[] fonts => def.fonts;
        double time = 0;
        private Cell[,]? cells = null;
        public TypedBuffer<Cell> cellsBuffer;

        public void SetOrigin(Vector2 origin) {
            viewportOrigin = origin;
        }
        public override void BindDraw() {
            var shader = Shader;
            if (cells == null) {
                cells = new Cell[def.textBuffer.bufferSize.Y, def.textBuffer.bufferSize.X];
                for (int Y = 0; Y < def.textBuffer.bufferSize.Y; Y++) {
                    for (int X = 0; X < def.textBuffer.bufferSize.X; X++) {
                        cells[Y, X] = new Cell {
                            data = (ushort)(X + Y * 12),
                            fg = 0x0FFF,
                            bg = 0x0000,
                        };
                    }
                }
                cellsBuffer = new TypedBuffer<Cell>("cellsBuffer", cells, BufferStorageMask.DynamicStorageBit);
            }

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

            // Set time uniform
            shader.SetUniform("time", time);

            // THIS IS PERFORMANCE!
            cellsBuffer.Set(cells!);
            shader.SetShaderStorageBlock($"cells", cellsBuffer);
            def.textBuffer.Bind("buf", shader);

            base.BindDraw();
        }
    }
}
