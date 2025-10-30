using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        public static Vector4 ToTkVector4(this Rgba32 color)  => new Vector4(color.R, color.G, color.B, color.A) / 255.0f;
        public static Vector4i ToTkVector4i(this Rgba32 color)  => new Vector4i(color.R, color.G, color.B, color.A);
        public static Vector4 Sqrt(this Vector4 v) => new Vector4(MathF.Sqrt(v.X), MathF.Sqrt(v.Y), MathF.Sqrt(v.Z), MathF.Sqrt(v.W));

    }
    public enum SelectorCode: uint { None = 0 }
    public class MappingAnalyzer<T> {
        public readonly Dictionary<SelectorCode, List<Vector2i>> Mappings = new();
        // First baseTile should be the empty tile
        public MappingAnalyzer(Rgba32[,] textureData, Vector2i tileSize, Vector2i patternOffset, params Vector2i[] baseTiles) {
            this.textureData = textureData;
            this.size = new Vector2i(textureData.GetLength(1), textureData.GetLength(0));
            this.tileSize = tileSize;
            this.patternOffset = patternOffset;

            // first characterize the base tiles
            foreach (var baseTilePos in baseTiles) {
                foundColors.Add(CharacterizeColor(baseTilePos * tileSize, new Vector2i(1, 1), tileSize));
            }

            tilesetSize = (this.size - patternOffset) / tileSize;
            for (int Y = 0; Y < tilesetSize.Y; ++Y) {
                for (int X = 0; X < tilesetSize.X; ++X) {
                    Vector2i XY = patternOffset + new Vector2i(X, Y);
                    Vector2i P = XY * tileSize;
                    var cornerRadius = new Vector2i(7, 7);
                    int matchColorRadius = 21;
                    // test the four interior corners
                    var C0 = CharacterizeColor(P + new Vector2i(0, 0), new Vector2i(1, 1), cornerRadius);
                    var C1 = CharacterizeColor(P + new Vector2i(tileSize.X - 1, 0), new Vector2i(-1, 1), cornerRadius);
                    var C2 = CharacterizeColor(P + new Vector2i(0, tileSize.Y - 1), new Vector2i(1, -1), cornerRadius);
                    var C3 = CharacterizeColor(P + new Vector2i(tileSize.X - 1, tileSize.Y - 1), new Vector2i(-1, -1), cornerRadius);
                    var C0c = MatchColor(C0);
                    var C1c = MatchColor(C1);
                    var C2c = MatchColor(C2);
                    var C3c = MatchColor(C3);
                    SelectorCode code = SelectorCode.None;
                    code += (uint)C0c * 0x01_00_00_00u;
                    code += (uint)C1c * 0x00_01_00_00u;
                    code += (uint)C2c * 0x00_00_01_00u;
                    code += (uint)C3c * 0x00_00_00_01u;
                    if (!Mappings.ContainsKey(code)) {
                        Mappings[code] = new List<Vector2i>();
                    }
                    Mappings[code].Add(XY * tileSize);
                }
            }
        }
        //////////////////////////////////////////////////////////////////
        struct Character {
            internal Vector4 mean;
            internal Vector4 sd;

            float GaussianOverlap(Character other, float soften = 1.0e-38f) {
                Vector4 delta = (mean - other.mean).Abs();
                var a = Math.Exp(-0.5f * (delta / (sd + 1e-8f)).LengthSquared);
                var b = Math.Exp(-0.5f * (delta / (other.sd + 1e-8f)).LengthSquared);
                return (float)(a * b);
            }
            public int MatchIndex(List<Character> foundColors, float soften = 0.0f) {
                int bestMatch = -1;
                float bestOverlap = 0;
                for (int i = 0; i < foundColors.Count; ++i) {
                    float overlap = GaussianOverlap(foundColors[i], soften);
                    if (overlap > bestOverlap) {
                        bestOverlap = overlap;
                        bestMatch = i;
                    }
                }
                return bestMatch;
            }
        }
        List<Character> foundColors = new();
        Rgba32[,] textureData;
        public readonly Vector2i size;
        public readonly Vector2i tileSize;
        public readonly Vector2i patternOffset;
        public readonly Vector2i tilesetSize;
        Character CharacterizeColor(Vector2i P, Vector2i D, Vector2i radius) {
            if (P.X < 0 || P.X >= size.X || P.Y < 0 || P.Y >= size.Y) {
                return new Character {
                    mean = Vector4.Zero, sd = Vector4.Zero
                };
            }
            Vector4 mean = Vector4.Zero;
            Vector4 variance = Vector4.Zero;

            int count = 0;
            for (int IY = 0; IY < radius.Y; ++IY) {
                for (int IX = 0; IX < radius.X; ++IX) {
                    var colorRaw = textureData[P.Y + IY * D.Y, P.X + IX * D.X].ToTkVector4();
                    mean += colorRaw;
                    ++count;
                }
            }
            mean /= count;
            for (int IY = 0; IY < radius.Y; ++IY) {
                for (int IX = 0; IX < radius.X; ++IX) {
                    var colorRaw = textureData[P.Y + IY * D.Y, P.X + IX * D.X].ToTkVector4();
                    Vector4 delta = colorRaw - mean;
                    variance += delta * delta;
                }
            }
            variance /= count; // divide by count instead of (count - 1) since we have a fixed population

            Vector4 sd = variance.Sqrt();

            return new Character{ mean = mean, sd = sd };
        }

        byte MatchColor(Character color) {
            return (byte)color.MatchIndex(foundColors);
        }

    }
    public enum LayerGeometry {
        Square,
        DualSquare,
        HexAcross,
        HexDown,
        Triangle,
    }
    public class Layer<T>(LayerGeometry geometry, Vector2i size, T defaultCell = default) {
        public LayerGeometry geometry = geometry;
        public Vector2i size = size;
        public T defaultCell = defaultCell;
        public T[,] cells = Alloc(size);
        public Span<T> Map => MemoryMarshal.CreateSpan(ref cells[0, 0], size.X * size.Y);
        public ref T this[Vector2i index] => ref cells[index.Y, index.X];
        public ref T this[int Y, int X] => ref cells[Y, X];
        public bool IsValid(int X, int Y) {
            return X >= 0 && Y >= 0 && X < size.X && Y < size.Y;
        }
        public bool IsValid(Vector2i index) {
            return index.X >= 0 && index.Y >= 0 && index.X < size.X && index.Y < size.Y;
        }
        public bool IsValid(Vector2 index) {
            return index.X >= 0 && index.Y >= 0 && index.X < size.X && index.Y < size.Y;
        }
        static T[,] Alloc(Vector2i size) {
            return new T[size.Y, size.X];
        }
        public T At(Vector2i position) {
            if (IsValid(position)) {
                return this[position.Y, position.X];
            }
            return defaultCell;
        }
        public T At(int X, int Y) {
            if (IsValid(new Vector2i(X, Y))) {
                return this[Y, X];
            }
            return defaultCell;
        }
        public T AnyOf(Vector2i pos, params T[] options) {
            if (pos.Y >= 0 && pos.X >= 0 && pos.Y < size.Y && pos.X < size.X) {
                if (options.Length == 0 || options.Contains(this[pos])) {
                    return this[pos];
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
        public static Vector2 Gradient(int axis, Vector2 a, Vector2 b) {
            Vector2 slope = Vector2.Zero;
            float dX = b[axis] - a[axis];
            slope[axis] = Math.Sign(dX);
            slope[1 - axis] = (b[1 - axis] - a[1 - axis]) / dX;
            return slope;
        }

        public IEnumerable<Vector2> DDA(Vector2 from, Vector2 to, int axis, float center = 0.5f, bool includeEndpoint = false) {
            float FloorFrac(float x) {
                return x - (float)Math.Floor(x);
            }
            float CeilFrac(float x) {
                return (float)Math.Ceiling(x) - x;
            }

            float dX = to[axis] - from[axis];

            if (dX == 0) {
                yield break;
            }

            Vector2 slope = Gradient(axis, from, to);

            var last = from;

            float T;
            if (dX > 0) {
                T = CeilFrac(from[axis] - center);
            } else {
                T = FloorFrac(from[axis] - center);
            }

            Vector2 curr() => from + slope * T;

            // Step through the line, yielding crossing points
            //while ((dX > 0 && next[axis] < to[axis]) || (dX < 0 && next[axis] > to[axis])) {
            while (dX > 0 ? last[axis] < to[axis] : last[axis] > to[axis]) {
                if (T > 0) {
                    yield return last;
                }
                last = curr();
                T += 1;
            }

            // Optionally include the endpoint
            if (includeEndpoint) {
                //yield return last;
            }
        }

        public void Line(T cell, Vector2i from, Vector2i to, bool includeEndpoint = true) {
            var PixelCenter = new Vector2(0.5f, 0.5f);;
            Line(cell, from.ToVector2() + PixelCenter, to.ToVector2() + PixelCenter, includeEndpoint);
        }
        public void Line(T cell, Vector2 from, Vector2 to, bool includeEndpoint = true) {
            float center = 1.0f;
            Console.WriteLine($"Line({from}, {to}) / center: {center}");
            Vector2 d = to - from;
            int axis = Math.Abs(d.X) > Math.Abs(d.Y) ? 0 : 1;
            int resultNo = 0;
            foreach (var point in DDA(from, to, axis, center, includeEndpoint: includeEndpoint)) {
                if (IsValid(point)) {
                    int px = (int)point.X;
                    int py = (int)point.Y;
                    this[py, px] = cell;
                    Console.WriteLine($"[{resultNo}]: {point} @ {px}, {py}");
                    ++resultNo;
                }
            }
        }

        public void Line(T cell, float radius, Vector2i from, Vector2i to, bool includeEndpoint = true) {
            _Line(cell, radius, from, to, includeEndpoint);
            //Circle(cell, radius, to);
        }

        void _Line(T cell, float radius, Vector2i from, Vector2i to, bool includeEndpoint = true) {
            Vector2i d = to - from;
            int axis = Math.Abs(d.X) > Math.Abs(d.Y) ? 0 : 1;
            var slope = Gradient(axis, from, to);
            Vector2 advance = slope * radius;
            float adjRadius = advance.Length;
            foreach (var point in DDA(
                         from.ToVector2() - advance,
                         to.ToVector2() + advance,
                         axis: axis,
                         includeEndpoint: includeEndpoint)) {
                float X = point[axis];
                if (X < 0 || X >= size[axis]) {
                    continue;
                }
                float Y = point[1 - axis];
                int Y0 = (int)(Y - adjRadius);
                int Y1 = (int)(Y + adjRadius);
                Y0 = Math.Max(0, Y0);
                Y1 = Math.Min(size[1 - axis], Y1);
                for (int J = Y0; J < Y1; ++J) {
                    Vector2 P = new();
                    P[axis] = X;
                    P[1 - axis] = J;
                    if (DistanceToLineSegmentSquared(P, from, to) < radius * radius) {
                        this[(int)P.Y, (int)P.X] = cell;
                    }
                }
            }
            //Circle(cell, radius, from);
        }

        float DistanceToLineSegmentSquared(Vector2 point, Vector2 a, Vector2 b) {
            Vector2 ab = b - a;
            Vector2 ap = point - a;
            float t = Vector2.Dot(ap, ab) / ab.LengthSquared;
            t = Math.Clamp(t, 0f, 1f);
            Vector2 closest = a + ab * t;
            float distSquared = (point - closest).LengthSquared;
            return distSquared;
        }


        public void Circle(T cell, float radius, Vector2 center) {
            var d = radius * 2;
            int minY = (int)(center.Y - radius);
            int maxY = (int)(center.Y + radius);
            int minX = (int)(center.X - radius);
            int maxX = (int)(center.X + radius);
            minY = Math.Max(0, minY);
            maxY = Math.Min(size.Y, maxY);
            minX = Math.Max(0, minX);
            maxX = Math.Min(size.X, maxX);
            for (int Y = minY; Y < maxY; ++Y) {
                for (int X = minX; X < maxX; ++X) {
                    var dx = X - center.X;
                    var dy = Y - center.Y;
                    if (dx * dx + dy * dy <= d * d) {
                        this[Y, X] = cell;
                    }
                }
            }
        }

        public void Polyline(T cell, params Vector2i[] points) {
            if (points.Length < 2) {
                return;
            }
            for (int i = 0; i < points.Length - 1; ++i) {
                Line(cell, points[i], points[i + 1]);
            }
        }
        void _Polyline(T cell, float radius, bool wrap, params Vector2i[] points) {
            if (points.Length < 2) {
                return;
            }
            for (int i = 0; i < points.Length - 1; ++i) {
                _Line(cell, radius, points[i], points[i + 1]);
            }
            if (wrap) {
                Line(cell, radius, points[^1], points[0]);
            }
        }
        public void Polyline(T cell, float radius, params Vector2i[] points) {
            _Polyline(cell, radius, false, points);
        }
        public void Polygon(T cell, params Vector2i[] points) {
            Polyline(cell, points);
            Line(cell, points[^1], points[0]);
        }
        public void Polygon(T cell, float radius, params Vector2i[] points) {
            _Polyline(cell, radius, true, points);
        }

        public void LineBresenham(T cell, Vector2i from, Vector2i to) {
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
        // 0 1
        //  x   -> 0x00_01_02_03
        // 2 3
        public uint CalcDualWangCornerSelector(Vector2i position, params byte[] selectors) {
            uint result = 0;
            byte sel0 = layer.AnyOf(position, selectors);
            byte sel1 = layer.AnyOf(position + new Vector2i(1, 0), selectors);
            byte sel2 = layer.AnyOf(position + new Vector2i(0, 1), selectors);
            byte sel3 = layer.AnyOf(position + new Vector2i(1, 1), selectors);

            result |= (uint)sel0 * 0x01_00_00_00;
            result |= (uint)sel1 * 0x00_01_00_00;
            result |= (uint)sel2 * 0x00_00_01_00;
            result |= (uint)sel3 * 0x00_00_00_01;
            return result;
        }

        public Layer<byte> layer = Layer;

        public Cell[,] ApplyDualWangCorner(Dictionary<uint, List<Cell>> mapping, params byte[] selectors) {
            Cell[,] result = new Cell[layer.size.Y - 1, layer.size.X - 1];

            for (int Y = 0; Y < layer.size.Y; ++Y) {
                for (int X = 0; X < layer.size.X; ++X) {
                    uint code = CalcDualWangCornerSelector(new Vector2i(X, Y), selectors);
                    if (code > 0) {
                        if (mapping.TryGetValue(code, out var cells)) {
                            result[Y, X] = cells[0];
                        } else {
                            result[Y, X] = default;
                        }
                    }
                }
            }
            return result;
        }
    }
}
