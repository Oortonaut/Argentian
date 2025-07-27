using System;
using System.Collections.Generic;
using System.Linq;
using Argentian.Render.Prims;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.PixelFormats;

namespace Argentian.Roguelike {
    public static partial class Extensions {
        public static float Dist(this Color4<Rgba> a, Color4<Rgba> b) {
            var dr = a.X - b.X;
            var dg = a.Y - b.Y;
            var db = a.Z - b.Z;
            var da = a.W - b.W;
            return dr * dr + dg * dg + db * db + da * da;
        }
        public static Color4<Rgba> ToColor4(this Rgba32 color)  => new Color4<Rgba>(color.R, color.G, color.B, color.A);
        public static Dictionary<uint, List<Vector2i>> GenerateTemplateMapping(string path, Vector2i patternSize, Vector2i tileSize, Vector2i tileOffset, params uint[] parts) {
            List<Color4<Rgba>> foundColors = [new Color4<Rgba>(0, 0, 0, 0)];

            var (stream, filePath) = Core.Config.ReadStream(Core.Config.texturePath, path);
            var textureData = Wrap.Extensions.LoadImage2D<Rgba32>(stream);

            int W = textureData.GetLength(1);
            int H = textureData.GetLength(0);

            var result = new Dictionary<uint, List<Vector2i>>();
            int partsLength = parts.Length - 1;

            for (int Y = 0; Y < patternSize.Y; ++Y) {
                for (int X = 0; X < patternSize.X; ++X) {
                    Vector2i XY = tileOffset + new Vector2i(X, Y);
                    Vector2i P = XY * tileSize;
                    int cornerRadius = 4;
                    // test the four interior corners
                    var C0 = CharacterizeColor(P.X, P.Y, 1, 1, cornerRadius);
                    var C1 = CharacterizeColor(P.X, P.Y, -1, 1, cornerRadius);
                    var C2 = CharacterizeColor(P.X, P.Y, 1, -1, cornerRadius);
                    var C3 = CharacterizeColor(P.X, P.Y, -1, -1, cornerRadius);
                    var C0c = Math.Clamp(MatchColor(C0), 0, partsLength);
                    var C1c = Math.Clamp(MatchColor(C1), 0, partsLength);
                    var C2c = Math.Clamp(MatchColor(C2), 0, partsLength);
                    var C3c = Math.Clamp(MatchColor(C3), 0, partsLength);
                    uint code = 0;
                    code += parts[C0c] * 0x01_00_00_00;
                    code += parts[C1c] * 0x00_01_00_00;
                    code += parts[C2c] * 0x00_00_01_00;
                    code += parts[C3c] * 0x00_00_00_01;
                    if (!result.ContainsKey(code)) {
                        result[code] = new List<Vector2i>();
                    }
                    result[code].Add(XY);
                }
            }
            return result;
            //////////////////////////////////////////////////////////////////
            Color4<Rgba> CharacterizeColor(int X, int Y, int DX, int DY, int radius = 3) {
                if (DX < 0) {
                    X += tileSize.X + DX;
                }
                if (DY < 0) {
                    Y += tileSize.Y + DY;
                }
                if (X < 0 || X >= W || Y < 0 || Y >= H) {
                    return new Color4<Rgba>(0, 0, 0, 0);
                }
                Color4<Rgba> mean = new();

                for (int IY = 0; IY < radius; ++IY) {
                    for (int IX = 0; IX < radius; ++IX) {
                        var colorRaw = textureData[Y + IY * DY, X + IX * DX];
                        var color = new Color4<Rgba>(colorRaw.R, colorRaw.G, colorRaw.B, colorRaw.A);
                        mean.X += color.X;
                        mean.Y += color.Y;
                        mean.Z += color.Z;
                        mean.W += color.W;
                    }
                }
                mean.X /= radius * radius;
                mean.Y /= radius * radius;
                mean.Z /= radius * radius;
                mean.W /= radius * radius;

                var bestFit = textureData[Y, X].ToColor4();
                var bestDist = mean.Dist(bestFit);
                for (int IY = 0; IY < radius && bestDist > 0; ++IY) {
                    for (int IX = 0; IX < radius && bestDist > 0; ++IX) {
                        var color = textureData[Y + IY * DY, X + IX * DX].ToColor4();
                        var fit = mean.Dist(color);
                        if (fit < bestDist) {
                            bestFit = color;
                            bestDist = fit;
                        }
                    }
                }
                return bestFit;
                /////////////////////////////////////
            }

            int MatchColor(Color4<Rgba> color, float radius = 6) {
                var found = foundColors.FindIndex((c)  => {
                    var dr = color.X - c.X;
                    var dg = color.Y - c.Y;
                    var db = color.Z - c.Z;
                    var da = color.W - c.W;
                    var dist = dr * dr + dg * dg + db * db + da * da;
                    return dist < radius * radius;
                });
                if (found < 0) {
                    foundColors.Add(color);
                    found = foundColors.Count - 1;
                }
                return found;
            }

        }

    }
    public enum LayerGeometry {
        Square,
        DualSquare,
        HexAcross,
        HexDown,
        Triangle,
    }
    public class Layer<T>(LayerGeometry geometry, T[,] cells, T defaultCell = default) {
        public LayerGeometry geometry = geometry;
        public T[,] cells = cells;
        public Vector2i size = new Vector2i(cells.GetLength(1), cells.GetLength(0));
        public T defaultCell = defaultCell;
        public ref T this[Vector2i index] => ref cells[index.Y, index.X];
        public ref T this[int Y, int X] => ref cells[Y, X];
        public T At(Vector2i position) {
            if (position.Y >= 0 && position.X >= 0 && position.Y < size.Y && position.X < size.X) {
                return cells[position.Y, position.X];
            }
            return defaultCell;
        }
        public T At(int X, int Y) {
            if (Y >= 0 && X >= 0 && Y < size.Y && X < size.X) {
                return cells[Y, X];
            }
            return defaultCell;
        }
        public T Selector(int X, int Y, params T[] options) {
            if (Y >= 0 && X >= 0 && Y < size.Y && X < size.X) {
                if (options.Contains(cells[Y, X])) {
                    return cells[Y, X];
                };
            }
            return defaultCell;
        }

        public void Fill(T cell) {
            for (int Y = 0; Y < size.Y; Y++) {
                for (int X = 0; X < size.X; X++) {
                    this[Y, X] = cell;
                }
            }
        }
        public void FillRect(T cell, Vector2i ul, Vector2i lr) 
        {
            var minX = Math.Max(0, Math.Min(ul.X, lr.X));
            var minY = Math.Max(0, Math.Min(ul.Y, lr.Y));
            var maxX = Math.Min(size.X, Math.Max(ul.X, lr.X));
            var maxY = Math.Min(size.Y, Math.Max(ul.Y, lr.Y));

            for (int y = minY; y <= maxY; y++) {
                for (int x = minX; x <= maxX; x++) {
                    this[y, x] = cell;
                }
            }
        }
        
        public void Line(T cell, Vector2i from, Vector2i to) {
            var x0 = from.X;
            var y0 = from.Y;
            var x1 = to.X;
            var y1 = to.Y;
            
            var dx = Math.Abs(x1 - x0);
            var sx = x0 < x1 ? 1 : -1;
            var dy = -Math.Abs(y1 - y0);
            var sy = y0 < y1 ? 1 : -1;
            var error = dx + dy;
            
            while (true) {
                this[y0, x0] = cell;
                
                var e2 = 2 * error;
                
                if (e2 >= dy) {
                    if (x0 == x1) break;
                    error += dy;
                    x0 += sx;
                }

                if (e2 <= dx) {
                    if (y0 == y1) break;
                    error += dx;
                    y0 += sy;
                }
            }
        }
    }
    public struct Autotile(Layer<byte> Layer) {
        public uint CalcDualWangCornerSelector(Vector2i position, params byte[] selectors) {
            uint result = 0;
            byte sel0 = layer.Selector(position.X + 1, position.Y + 1, selectors);
            byte sel1 = layer.Selector(position.X, position.Y + 1, selectors);
            byte sel2 = layer.Selector(position.X + 1, position.Y, selectors);
            byte sel3 = layer.Selector(position.X, position.Y, selectors);

            result |= (uint)sel0 * 0x1;
            result |= (uint)sel1 * 0x100;
            result |= (uint)sel2 * 0x10000;
            result |= (uint)sel3 * 0x1000000;
            return result;
        }

        public Layer<byte> layer = Layer;

        public TilemapPrimitive.Cell[,] ApplyDualWangCorner(Dictionary<uint, List<TilemapPrimitive.Cell>> mapping) {
            TilemapPrimitive.Cell[,] result = new TilemapPrimitive.Cell[layer.size.Y - 1, layer.size.X - 1];

            for (int Y = 0; Y < layer.size.Y; ++Y) {
                for (int X = 0; X < layer.size.X; ++X) {
                    uint code = CalcDualWangCornerSelector(new Vector2i(X, Y), 1);
                    if (code > 0 && mapping.TryGetValue(code, out var cells)) {
                        result[Y, X] = cells[0];
                    }
                }
            }
            return result;
        }
    }
}
