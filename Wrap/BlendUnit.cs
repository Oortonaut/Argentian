using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Argentian.Wrap {
    public class BlendUnit {
        // TODO: logic ops? It's one channel, color only I think.
        public Channel color = new();
        public Channel alpha = new();
        public Color4<Rgba>? clear = null;

        public BlendUnit(Channel both_, Color4<Rgba>? clear_ = null) {
            color = both_;
            alpha = both_;
            clear = clear_;
        }
        public BlendUnit(Channel color_, Channel alpha_, Color4<Rgba>? clear_ = null) {
            color = color_;
            alpha = alpha_;
            clear = clear_;
        }

        public record Channel(
            BlendingFactor src = BlendingFactor.One,
            BlendEquationModeEXT mode = BlendEquationModeEXT.FuncAdd,
            BlendingFactor dst = BlendingFactor.Zero
        ) {
            public static Channel Add(BlendingFactor src_, BlendingFactor dst_) =>
                new Channel(src_, BlendEquationModeEXT.FuncAdd, dst_);
        }
        public static Channel Write = new Channel { 
            src = BlendingFactor.One, 
            mode = BlendEquationModeEXT.FuncAdd, 
            dst = BlendingFactor.Zero 
        };
        public static Channel Keep = new Channel {
            src = BlendingFactor.Zero,
            mode = BlendEquationModeEXT.FuncAdd,
            dst = BlendingFactor.One
        };
        public static Channel Blend = new Channel {
            src = BlendingFactor.SrcAlpha,
            mode = BlendEquationModeEXT.FuncAdd,
            dst = BlendingFactor.OneMinusSrcAlpha
        };
        public static Channel Premult = new Channel {
            src = BlendingFactor.One,
            mode = BlendEquationModeEXT.FuncAdd,
            dst = BlendingFactor.OneMinusSrcAlpha
        };
        public static Channel Zero = new Channel {
            src = BlendingFactor.Zero,
            mode = BlendEquationModeEXT.FuncAdd,
            dst = BlendingFactor.Zero
        };
        public void Bind(uint drawBufferIndex) {
            GL.BlendEquationSeparatei(drawBufferIndex,
                color.mode,
                alpha.mode);
            GL.BlendFuncSeparatei(drawBufferIndex,
                color.src, color.dst,
                alpha.src, alpha.dst);
        }
    }
}
