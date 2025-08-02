using System;
using System.IO;
using System.Drawing;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;

namespace Argentian.Wrap {
    public static partial class Extensions {
        public static Texture LoadTexture(string path) {
            //Load the image
            var (stream, filePath) = Core.Config.ReadStream(Core.Config.texturePath, path);
            var def = LoadTextureDef(stream);

            var result = new Texture(path, def);

            bool s = def.internalFormat switch {
                InternalFormat.R8 => result.Set(def.size, def.format, def.type, LoadImage<L8>(stream)),
                InternalFormat.Rg8 => result.Set(def.size, def.format, def.type, LoadImage<La16>(stream)),
                InternalFormat.Rgb8 => result.Set(def.size, def.format, def.type, LoadImage<Rgb24>(stream)),
                InternalFormat.Rgba8 => result.Set(def.size, def.format, def.type, LoadImage<Rgba32>(stream)),
                InternalFormat.Srgb8 => result.Set(def.size, def.format, def.type, LoadImage<Rgb24>(stream)),
                InternalFormat.Srgb8Alpha8 => result.Set(def.size, def.format, def.type, LoadImage<Rgba32>(stream)),
                InternalFormat.R16 => result.Set(def.size, def.format, def.type, LoadImage<L16>(stream)),
                InternalFormat.Rg16 => result.Set(def.size, def.format, def.type, LoadImage<La32>(stream)),
                InternalFormat.Rgb16 => result.Set(def.size, def.format, def.type, LoadImage<Rgb48>(stream)),
                InternalFormat.Rgba16 => result.Set(def.size, def.format, def.type, LoadImage<Rgba64>(stream)),
                _ => throw new InvalidDataException($"Confused about internal format {def.internalFormat}"),
            };
            return result;
        }
        public static Texture.Def LoadTextureDef(Stream stream) {
            stream.Position = 0;
            var info = SixLabors.ImageSharp.Image.IdentifyAsync(stream).Result;
            var fmt = info.Metadata.DecodedImageFormat;
            stream.Position = 0;
            int bits = info.PixelType?.BitsPerPixel ?? 32;
            int bpc = bits <= 32 ? 8 : 16;
            int channels = bits / bpc;

            PixelFormat format = channels switch {
                1 => PixelFormat.Red,
                2 => PixelFormat.Rg,
                3 => PixelFormat.Rgb,
                _ => PixelFormat.Rgba,
            };
            InternalFormat internalFormat = bpc <= 8
                ? channels switch {
                    1 => InternalFormat.R8,
                    2 => InternalFormat.Rg8,
                    3 => InternalFormat.Srgb8,
                    _ => InternalFormat.Srgb8Alpha8,
                }
                : channels switch {
                    1 => InternalFormat.R16,
                    2 => InternalFormat.Rg16,
                    3 => InternalFormat.Rgb16,
                    _ => InternalFormat.Rgba16,
                };
            PixelType pixelType = bpc <= 8 ? PixelType.UnsignedByte : PixelType.UnsignedShort;

            if (fmt is PngFormat pngFormat) {
                if (info.Metadata.GetFormatMetadata(pngFormat) is PngMetadata pngInfo) {
                    channels = pngInfo.ColorType switch {
                        PngColorType.Grayscale => 1,
                        PngColorType.GrayscaleWithAlpha => 2,
                        PngColorType.Rgb => 3,
                        PngColorType.RgbWithAlpha => 4,
                        PngColorType.Palette => pngInfo.TransparentColor != null ? 4 : 3,
                        _ => 4,
                    };
                    bpc = (int)(pngInfo.BitDepth ?? PngBitDepth.Bit8);
                }
            } else if (fmt is BmpFormat bmpFormat) {
                pixelType = PixelType.UnsignedByte;
                if (info.Metadata.GetFormatMetadata(bmpFormat) is BmpMetadata bmpInfo) {
                    switch (bmpInfo.BitsPerPixel) {
                    case BmpBitsPerPixel.Pixel1: // palettized bitmap
                        channels = 1;
                        bpc = 1;
                        format = PixelFormat.Red;
                        internalFormat = InternalFormat.CompressedRed;
                        break;
                    case BmpBitsPerPixel.Pixel4: // palette
                        channels = 3;
                        format = PixelFormat.Rgba;
                        internalFormat = InternalFormat.Rgba4;
                        pixelType = PixelType.UnsignedShort4444;
                        bpc = 4;
                        break;
                    case BmpBitsPerPixel.Pixel8: // palette
                        channels = 3;
                        format = PixelFormat.Rgb;
                        internalFormat = InternalFormat.Srgb8;
                        bpc = 8;
                        break;
                    case BmpBitsPerPixel.Pixel16:
                        channels = 4;
                        format = PixelFormat.Rgba;
                        internalFormat = InternalFormat.Rgba4;
                        pixelType = PixelType.UnsignedShort4444;
                        bpc = 8;
                        break;
                    case BmpBitsPerPixel.Pixel24:
                        channels = 3;
                        format = PixelFormat.Rgb;
                        internalFormat = InternalFormat.Srgb8;
                        bpc = 8;
                        break;
                    case BmpBitsPerPixel.Pixel32:
                        channels = 4;
                        format = PixelFormat.Rgba;
                        internalFormat = InternalFormat.Srgb8Alpha8;
                        bpc = 8;
                        break;
                    }
                }
            }
            return new Texture.Def {
                target = TextureTarget.Texture2d,
                type = pixelType,
                format = format,
                internalFormat = internalFormat,
                size = new Vector2i(info.Width, info.Height),
            };
        }
        public static TPixel[] LoadImage<TPixel>(Stream stream) where TPixel : unmanaged, IPixel<TPixel> {
            SixLabors.ImageSharp.Image<TPixel> fileImage = SixLabors.ImageSharp.Image.Load<TPixel>(stream);

            int W = fileImage.Width;
            int H = fileImage.Height;
            var result = new TPixel[fileImage.Height * fileImage.Width] ?? throw new InsufficientMemoryException("Couldn't allocate temporary image buffer");
            fileImage.CopyPixelDataTo(result);
            return result;
        }
        public static TPixel[,] LoadImage2D<TPixel>(Stream stream) where TPixel : unmanaged, IPixel<TPixel> {
            SixLabors.ImageSharp.Image<TPixel> fileImage = SixLabors.ImageSharp.Image.Load<TPixel>(stream);

            int W = fileImage.Width;
            int H = fileImage.Height;
            var result = new TPixel[fileImage.Height, fileImage.Width] ?? throw new InsufficientMemoryException("Couldn't allocate temporary image buffer");

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                result[y, x] = fileImage[x, y];
            return result;
        }
    }
    public class Texture: Disposable {
        public class Def {
            public TextureTarget target;


            // from user if no filename, otherwise from file
            public Vector2i size = Vector2i.Zero;
            public PixelFormat format = PixelFormat.Rgba;
            public InternalFormat internalFormat = InternalFormat.Rgba8;
            public PixelType type = PixelType.UnsignedByte;
        }

        public readonly Def def;
        public Texture(string name, Def def_): base(name) {
            def = def_;
            native = new TextureHandle(GL.CreateTexture(TextureTarget.Texture2d));
            GL.ObjectLabel(ObjectIdentifier.Texture, (uint)native.Handle, Name.Length, Name);
            CreateStorage(def);
        }
        public TextureHandle native;
        protected override void Delete() {
            GL.DeleteTexture(native.Handle);
        }
        public override string ToString() => $"Texture {native.Handle} '{Name}'{def.size}/{def.internalFormat}{DisposedString}";
        public Vector2i Size => def.size;

        void CreateStorage(Def def) {
            switch (def.target) {
            case TextureTarget.Texture2d:
                GL.TextureStorage2D(
                    native.Handle,
                    1,
                    (SizedInternalFormat)def.internalFormat,
                    def.size.X,
                    def.size.Y);
                var err = GL.GetError();
                break;
            default:
                throw new InvalidDataException("Unsupported");
            }
        }
        public bool Set<T>(Vector2i size, PixelFormat fmt, PixelType type, T[] pixels, int mip = -1) where T : unmanaged {
            bool automip = false;
            if (mip == -1) {
                mip = 0;
                automip = true;
            }
            switch (def.target) {
            case TextureTarget.Texture2d:
                GL.TextureSubImage2D(
                    native.Handle,
                    mip,
                    0,
                    0,
                    def.size.X,
                    def.size.Y,
                    def.format,
                    def.type,
                    in pixels[0]);
                break;
            default:
                throw new InvalidDataException("Unsupported");
            }
            if (automip) {
                GL.GenerateTextureMipmap(native.Handle);
            }
            // GL.TextureParameter(handle.Handle, TextureParameterName.TextureMinLod, 0);
            // GL.TextureParameter(handle.Handle, TextureParameterName.TextureMaxLod, 0);
            return true;
        }
    }
}
