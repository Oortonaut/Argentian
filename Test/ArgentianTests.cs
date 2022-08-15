using Microsoft.VisualStudio.TestTools.UnitTesting;
using Argentian.Wrap;
using Argentian.Engine;
using Argentian.Render;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;
using System.Collections.Generic;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.ClassLevel)]

// namespace Test {
//     [TestClass]
//     public class ArgentianTests {
//         [TestMethod]
//         public void Init() {
//         }
//         void Run(IEnumerable<Pass> passes) {
//             if(renderer == null || window == null) return;
//             var passList = passes.ToArray();
//             void UpdateFrame(object? sender, EventArgs e) {
//             }
// 
//             void RenderFrame(object? sender, EventArgs e) {
//                 var win = sender as GameWindow;
//                 if(win == null || win != window) return;
//                 foreach (var pass in passList) {
//                     renderer.Queue(pass);
//                 }
//                 renderer.Draw(win.Context, win.ClientSize);
//                 win.Context.SwapBuffers();
//                 GlDisposable.ProcessDeleteQueue();
//             }
// 
//             void KeyPress(object? sender, KeyPressEventArgs e) {
//                 (sender as GameWindow)?.Close();
//             }
// 
//             window.UpdateFrame += UpdateFrame;
//             window.RenderFrame += RenderFrame;
//             window.KeyPress += KeyPress;
// 
//             window.Run();
// 
//             GC.Collect();
//             GC.WaitForPendingFinalizers();
//             GlDisposable.ProcessDeleteQueue();
// 
// 
//         }
// 
//         GameWindow ? window;
//         Renderer? renderer;
//     }
// }

#if false
// See https://aka.ms/new-console-template for more information

Console.WriteLine("Starting Jetson.Test");
Config.Initialize(args);

var width = 800;
var height = 600;

using var window = new GameWindow(
        width, height, GraphicsMode.Default, "Jetson Test", GameWindowFlags.Default,
        DisplayDevice.Default, 4, 6, GraphicsContextFlags.ForwardCompatible, null, false
        );
using var renderer = new Renderer();
//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
// Create Tilemap material from .mat yaml file.
Caches.ShaderProgramDefs.Get("tilemap.mat");

//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
// The first pass uses this "stretch" material that just copies its source
// texture.
Caches.ShaderProgramDefs.Insert("stretch.mat", new ShaderProgram.Def {
    vertex = { "stretch.vert.gl" },
    fragment = { "stretch.frag.gl" },
});
using var stretchProgram = Caches.NewShaderProgram("stretch.mat");

//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
// The compose material corrects for OpenGLs inverted screen Y as well as color
// correction (planned)
Caches.ShaderProgramDefs.Insert("compose.mat", new ShaderProgram.Def {
    headers = { "util.h.gl" },
    vertex = { "compose.vert.gl" },
    fragment = { "compose.frag.gl", "util.lib.gl" },
});
using var composeProgram = Caches.NewShaderProgram("compose.mat");
// Samplers
using var samplerNearest = new Sampler("nearest", new Sampler.Def {
    magFilter = TextureMagFilter.Nearest,
    minFilter = TextureMinFilter.Nearest,
});
using var samplerBilinear = new Sampler("bilinear", new Sampler.Def {
    magFilter = TextureMagFilter.Linear,
    minFilter = TextureMinFilter.LinearMipmapLinear,
});

// Textures
using var alphaImage = Caches.Textures.Get("cp437_ibm_pc.png");
using var capybarasImage = Caches.Textures.Get("capybaras.png");

// first pass target

using var renderTargetImage = new Texture(":renderTargetImage", new Texture.Def { 
    format = PixelFormat.Rgba,
    internalFormat = PixelInternalFormat.Rgba8,
    type = PixelType.UnsignedByte,
    target = TextureTarget.Texture2D,
    size = new Size(384, 256),
});

// first pass quad

var stretchPrim = renderer.Quad("stretch.prim", stretchProgram);
stretchPrim.Material.SetTexture("tex", alphaImage, samplerNearest);

// first pass pass

var stretchPass = new Pass {
    name = "stretch",
    def = {
        order = 0,
        settings = {
            blend = {
                new BlendUnit(BlendUnit.Blend, Color4<Rgba>.Aqua)
            },
            depth = new DepthUnit { clear = 1.0f },
            stencil = new StencilUnit { clear = uint.MaxValue },
        },
        framebuffer = new Framebuffer("stretch FB", new Framebuffer.Attachments { 
            color = { new Attachment.Texture(renderTargetImage) } 
        }),
    },
    prims = { stretchPrim },
};

var composePrim = renderer.Quad("compose.prim", composeProgram);
composePrim.Material.SetTexture("tex", renderTargetImage, samplerBilinear);
composePrim.Material.SetTexture("bg", capybarasImage, samplerBilinear);


var composePass = new Pass {
    name = "compose",
    def = {
        order = 100,
        settings = {
            blend = {
                new BlendUnit(BlendUnit.Write, Color4<Rgba>.Aqua)
            },
        },
    },
    prims = { composePrim },
};


void UpdateFrame(object? sender, EventArgs e) {
}

void RenderFrame(object? sender, EventArgs e) {
    var win = sender as GameWindow;
    if (win == null) return;
    renderer.Queue(stretchPass);
    renderer.Queue(composePass);
    renderer.Draw(win.Context, win.ClientSize);
    win.Context.SwapBuffers();
    GlDisposable.ProcessDeleteQueue();
}

void KeyPress(object? sender, KeyPressEventArgs e) {
    (sender as GameWindow)?.Close();
}

window.UpdateFrame += UpdateFrame;
window.RenderFrame += RenderFrame;
window.KeyPress += KeyPress;

window.Run();

GC.Collect();
GC.WaitForPendingFinalizers();
GlDisposable.ProcessDeleteQueue();

#endif