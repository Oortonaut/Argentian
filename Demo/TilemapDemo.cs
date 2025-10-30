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
using Argentian.Roguelike;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.PixelFormats;

namespace Argentian {
    public class TilemapDemo : Application {
        // Each MapLayer may turn into one or more primitives depending on the
        // combinations available in the source textures.
        struct MapLayer {
            public Layer<byte> Data; // the cell types
        }
        record PrimLayer(TilemapPrimitive Prim, Layer<Cell> Layer);
        private List<PrimLayer> PrimCells = new();

        IShaderProgram tilemapProgram;
        RenderPass tilemapPass;

        public Vector2i Size { get; private set; }
        public Vector2i TileSize { get; private set; } = new Vector2i(32, 32);
        public Vector2i TileOffset { get; private set; } = Vector2i.Zero;
        public Vector2i CellSize { get; private set; } = new Vector2i(32, 32);
        public TilemapFlags Flags { get; private set; }
        private List<MapLayer> MapLayers = new();

        public TilemapDemo(string title, Renderer renderer, Vector3i size, TilemapFlags flags = TilemapFlags.None) : base(title, renderer) {
            // Load tilemap shader program from YAML file
            tilemapProgram = Caches.NewShaderProgram("tilemap.mat");
            Size = size.Xy;
            Flags = flags;

            InitAnalyzer();
            GeneratePrimitives(size.Z);
            var primList = PrimCells.Select(prim => prim.Prim);

            // Setup render pass
            tilemapPass = new RenderPass {
                name = "tilemap",
                def = {
                    order = 0,
                    settings = {
                        blends = {
                            new BlendUnit(BlendUnit.Blend, Color4.Black.WithAlpha(0)),
                        },
                    },
                },
                prims = [.. primList],
            };
        }

        protected override void UpdateFrame(double deltaTime) {
            base.UpdateFrame(deltaTime);
            // Convert the map layers to cell layers
            GenerateCells();
        }
        protected override void RenderFrame(double deltaTime) {
            tilemapProgram.SetUniform("time", (double)(sw.ElapsedMilliseconds * 0.001));
            int i = 0;
            foreach (var layer in MapLayers) {
            }
            foreach (var (prim, cells) in PrimCells) {
                prim.shader.SetUniform("time", (double)(sw.ElapsedMilliseconds * 0.001));
                prim.SetCells(cells);
            }

            renderer.Queue(tilemapPass);
            base.RenderFrame(deltaTime);
        }

        private MappingAnalyzer<byte>? analyzer;

        void InitAnalyzer() {
            var tilesetName = "BadAttitudeTiles.png";
            var (stream, filePath) = Core.Config.ReadStream(Core.Config.texturePath, tilesetName);
            var textureData = Wrap.Extensions.LoadImage2D<Rgba32>(stream);
            analyzer = new MappingAnalyzer<byte>(textureData, TileSize, TileOffset,
                    [.. Enumerable.Range(0, 16).Select(x => new Vector2i(x, 0))]
                );
        }
        void GenerateCells() {
            var corners = MapLayers[0].Data;
            corners.Fill(5);
            //MapLayers[0].Data.FillRect(3, new Vector2i(2, 5), new Vector2i(11, 15));
            //MapLayers[0].Data.FillRect(4, new Vector2i(5, 8), new Vector2i(14, 18));
            // corners.Line(2, new Vector2i(2, 2), new Vector2i(5, 3));
            // corners.Line(2, new Vector2i(2, 6), new Vector2i(5, 8));
            // corners.Line(2, new Vector2i(2, 10), new Vector2i(5, 13));
            // corners.Line(2, new Vector2i(2, 14), new Vector2i(5, 18));
            //
            corners.Line(2, new Vector2(8.0f ,  6.5f), new Vector2(12.0f ,  7.5f));
            corners.Line(2, new Vector2(8.25f, 10.5f), new Vector2(12.25f, 11.5f));
            corners.Line(2, new Vector2(8.5f , 14.5f), new Vector2(12.5f , 15.5f));
            corners.Line(2, new Vector2(8.75f, 18.5f), new Vector2(12.75f, 19.5f));

            //corners.Line(2, 3.0f, new Vector2i(15, 3), new Vector2i(30, 3));
            //corners.Line(3, new Vector2i(15, 3), new Vector2i(30, 3));
            //corners.Line(2, 3.0f, new Vector2i(15, 10), new Vector2i(30, 15));
            //corners.Line(3, new Vector2i(15, 10), new Vector2i(30, 15));
            //corners.Line(2, 3.0f, new Vector2i(3, 15), new Vector2i(3, 30));
            //corners.Line(2, 3.0f, new Vector2i(10, 15), new Vector2i(15, 30));
            //corners.Line(8, 1.5f, new Vector2i(25, 41), new Vector2i(3, 14));
            ////corners.Line(2, new Vector2i(2, 3), new Vector2i(26, 18));
            //corners.Polygon(6, 1.25f,
            //    new Vector2i(12, 8),
            //    new Vector2i(16, 12),
            //    new Vector2i(12, 16),
            //    new Vector2i(8, 12)
            //    );
            //corners.Polygon(7,
            //    new Vector2i(12, 8),
            //    new Vector2i(16, 12),
            //    new Vector2i(12, 16),
            //    new Vector2i(8, 12)
            //);

            var tilesetSize = analyzer.tilesetSize;
            var templateMapping = analyzer.Mappings;
            var tilesetterMapping = new Dictionary<uint, List<Cell>>();
            foreach (var (code, tileset) in templateMapping) {
                tilesetterMapping[(uint)code] = new List<Cell>();
                var cell = new Cell {
                    data = 0,
                    fg = 0xFFFF,
                    bg = 0x0000,
                    flags = 0,
                    stencil = 0,
                };
                foreach (var XYP in tileset) {
                    var XY = XYP / TileSize;
                    cell.data = (ushort)(XY.X + XY.Y * tilesetSize.X);
                    tilesetterMapping[(uint)code].Add(cell);
                }
            }
            var tiling = new Autotile(corners);
            for (int Y = 0; Y < Size.Y; ++Y) {
                for (int X = 0; X < Size.X; ++X) {
                    uint code = tiling.CalcDualWangCornerSelector(new Vector2i(X, Y));
                    int depth = 0;
                    if (code > 0) {
                        if (tilesetterMapping.TryGetValue(code, out var cells)) {
                            PrimCells[depth++]
                                .Layer
                                .cells[Y, X] =
                                cells[0];
                        } else {
                            var selectors = new uint[] {
                                code & 0xFF, (code >> 8) & 0xFF, (code >> 16) & 0xFF, (code >> 24) & 0xFF,
                            };
                            var sorted = selectors
                                .Where(x => x > 0)
                                .OrderBy(x => x)
                                .Distinct()
                                .Select(b => (byte)b)
                                .ToList();
                            for (int i = 0; i < sorted.Count - 1; ++i) {
                                var iSelector = sorted[i];
                                for (int j = i + 1; j < sorted.Count; ++j) {
                                    var jSelector = sorted[j];
                                    var layerCode = tiling.CalcDualWangCornerSelector(new Vector2i(X, Y), iSelector, jSelector);
                                    if (layerCode > 0) {
                                        if (tilesetterMapping.TryGetValue(layerCode, out var layerCells)) {
                                            PrimCells[depth++].Layer.cells[Y, X] = layerCells[0];
                                            sorted.RemoveAt(j);
                                            sorted.RemoveAt(i);
                                            --i;
                                            break;
                                        }
                                    }
                                }
                            }
                            for (int i = 0; i < sorted.Count; ++i) {
                                var selector = sorted[i];
                                var layerCode = tiling.CalcDualWangCornerSelector(new Vector2i(X, Y), selector);
                                if (layerCode > 0) {
                                    if (tilesetterMapping.TryGetValue(layerCode, out var layerCells)) {
                                        PrimCells[depth++].Layer.cells[Y, X] = layerCells[0];
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public int TileLayers { get; set; } = 5;
        void GeneratePrimitives(int sizeZ) {
            // Setup primitive
            var tilemapDef = new TilemapPrimitive.Def {
                primitiveType = PrimitiveType.Triangles,
                vertexStreams = [renderer.screenQuadStream],
                textBuffer = new TilemapPrimitive.TextBuf {
                    flags = Flags,
                    bufferSize = Size,
                    cellSize = CellSize,
                },
                fonts = [
                    new TilemapPrimitive.FontDef {
                        tileSize = TileSize,
                        tileScale = new Vector2(1, 1),
                        texture = Caches.Textures.Get("BadAttitudeTiles.png"),
                        sampler = Caches.Samplers.Get("nearest.samp"),
                    },
                    new TilemapPrimitive.FontDef {
                        tileSize = new Vector2i(16, 16),
                        tileScale = new Vector2(2, 2),
                        texture = Caches.Textures.Get("autotile47.png"),
                        sampler = Caches.Samplers.Get("nearest.samp"),
                    }
                ],
            };

            for (int i = 0; i < sizeZ; i++) {
                MapLayers.Add(new MapLayer {
                    Data = new Layer<byte>(LayerGeometry.Square, Size + Vector2i.One),
                });
            }
            for (int i = 0; i < 4; i++) {
                PrimCells.Add(new PrimLayer(
                    new TilemapPrimitive($"Tilemap {i}", renderer, tilemapProgram, tilemapDef),
                    new Layer<Cell>(LayerGeometry.Square, Size)
                    ));
            }

            //tilemapPrim = renderer.Quad("tilemap.prim", tilemapProgram);
        }

        protected override void Delete() {
            PrimCells.Clear();
            MapLayers.Clear();
            base.Delete();
        }

    }
}
