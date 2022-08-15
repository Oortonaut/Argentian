using OpenTK;
using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;
using System.Drawing;
using OpenTK.Graphics;

namespace Argentian.Wrap {
    public class Texture: Disposable {
        public class Def {
            public TextureTarget target;
            public InternalFormat internalFormat = InternalFormat.Rgba8;
            // from user if no filename, otherwise from file
            public Size size = Size.Empty;
            public PixelFormat format = PixelFormat.Rgba;
            public PixelType type = PixelType.UnsignedByte;
        }

        public readonly Def def;
        public Texture(string name, Def def_) : base(name) {
            def = def_;
            handle = GL.CreateTexture(TextureTarget.Texture2d);
            GL.ObjectLabel(ObjectIdentifier.Texture, ( uint ) handle.Handle, Name.Length, Name);
            CreateStorage(def);
        }
        public TextureHandle handle;
        protected override void Delete() {
            GL.DeleteTexture(handle);
        }
        public override string ToString() => $"Texture {handle.Handle} '{Name}'{def.size}/{def.internalFormat}{DisposedString}";

        void CreateStorage(Def def) {
            switch(def.target) {
            case TextureTarget.Texture2d:
                GL.TextureStorage2D(
                    handle,
                    1,
                    ( SizedInternalFormat ) def.internalFormat,
                    def.size.Width,
                    def.size.Height);
                var err = GL.GetError();
                break;
            default:
                throw new InvalidDataException("Unsupported");
            }
        }
        public static Def LoadTextureDef(Stream stream) {
            stream.Position = 0;
            var (info, fmt) = SixLabors.ImageSharp.Image.IdentifyWithFormatAsync(stream).Result;
            stream.Position = 0;
            int channels;
            int bpc;
            if(info.Metadata.GetFormatMetadata(fmt as PngFormat) is PngMetadata pngInfo) {
                channels = pngInfo.ColorType switch {
                    PngColorType.Grayscale => 1,
                    PngColorType.GrayscaleWithAlpha => 2,
                    PngColorType.Rgb => 3,
                    PngColorType.RgbWithAlpha => 4,
                    PngColorType.Palette => pngInfo.HasTransparency ? 4 : 3,
                    _ => 4,
                };
                bpc = ( int ) (pngInfo.BitDepth ?? PngBitDepth.Bit8);
            } else {
                int bits = info.PixelType?.BitsPerPixel ?? 32;
                bpc = bits <= 32 ? 8 : 16;
                channels = bits / bpc;
            }
            return new Def {
                format = channels switch {
                    1 => PixelFormat.Luminance,
                    2 => PixelFormat.LuminanceAlpha,
                    3 => PixelFormat.Rgb,
                    _ => PixelFormat.Rgba,
                },
                internalFormat = bpc <= 8 ?
                channels switch {
                    1 => InternalFormat.R8,
                    2 => InternalFormat.Rg8,
                    3 => InternalFormat.Rgb8,
                    _ => InternalFormat.Rgba8,
                } : channels switch {
                    1 => InternalFormat.R16,
                    2 => InternalFormat.Rg16,
                    3 => InternalFormat.Rgb16,
                    _ => InternalFormat.Rgba16,
                },
                target = TextureTarget.Texture2d,
                type = bpc <= 8 ? PixelType.UnsignedByte : PixelType.UnsignedShort,
                size = new Size(info.Width, info.Height),
            };
        }
        public static TPixel[] LoadImage<TPixel>(Stream stream) where TPixel : unmanaged, IPixel<TPixel> {
            SixLabors.ImageSharp.Image<TPixel> fileImage = SixLabors.ImageSharp.Image.Load<TPixel>(stream);

            var size = new Size(fileImage.Width, fileImage.Height);

            var result = new TPixel[fileImage.Height * fileImage.Width] ?? throw new InsufficientMemoryException("Couldn't allocate temporary image buffer");
            fileImage.CopyPixelDataTo(result);
            return result;
        }
        public static Texture LoadFile(string path) {
            //Load the image
            var (stream, filePath) = Core.Config.ReadStream(Core.Config.texturePath, path);
            var def = LoadTextureDef(stream);

            var result = new Texture(path, def);
            
            bool s = def.internalFormat switch {
                InternalFormat.R8 => result.Set(def.size, def.format, def.type, LoadImage<L8>(stream)),
                InternalFormat.Rg8 => result.Set(def.size, def.format, def.type, LoadImage<La16>(stream)),
                InternalFormat.Rgb8 => result.Set(def.size, def.format, def.type, LoadImage<Rgb24>(stream)),
                InternalFormat.Rgba8 => result.Set(def.size, def.format, def.type, LoadImage<Rgba32>(stream)),
                InternalFormat.R16 => result.Set(def.size, def.format, def.type, LoadImage<L16>(stream)),
                InternalFormat.Rg16 => result.Set(def.size, def.format, def.type, LoadImage<La32>(stream)),
                InternalFormat.Rgb16 => result.Set(def.size, def.format, def.type, LoadImage<Rgb48>(stream)),
                InternalFormat.Rgba16 => result.Set(def.size, def.format, def.type, LoadImage<Rgba64>(stream)),
                _ => throw new InvalidDataException($"Confused about internal format {def.internalFormat}"),
            };
            return result;
        }
        public bool Set<T>(Size size, PixelFormat fmt, PixelType type, T[] pixels, int mip = -1) where T : unmanaged {
            bool automip = false;
            if(mip == -1) {
                mip = 0;
                automip = true;
            }
            switch(def.target) {
            case TextureTarget.Texture2d:
                GL.TextureSubImage2D(
                    handle,
                    mip,
                    0,
                    0,
                    def.size.Width,
                    def.size.Height,
                    def.format,
                    def.type,
                    in pixels[0]);
                break;
            default:
                throw new InvalidDataException("Unsupported");
            }
            if(automip) {
                GL.GenerateTextureMipmap(handle);
            }
            // GL.TextureParameter(handle, TextureParameterName.TextureMinLod, 0);
            // GL.TextureParameter(handle, TextureParameterName.TextureMaxLod, 0);
            return true;
        }
    }
}
