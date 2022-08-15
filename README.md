# Argentian
Argentian is an OpenGL C# 3D Engine written by me, Ace Stapp, a 25 year rendering veteran. It supports almost all GL features including VSOs, FBOs, etc. 

It is mostly harmless but there is little documentation. The demo, simple as it is, ought to be enough to get you going. This is trimmed down and polished from a (somewhat) more capable app so don't let the simplicity fool you.

Versatile texture binding supports shader side layout (set by name on C# side), code side, or automatic texture unit selection. It separates Samplers and Textures. 

No shader #include but there are hax for some shader modularity. Create a .h.glsl with declarations and add it to the headers in the ShaderProgram.Def. Put function definition in a .glsl and include it with the vertex or pixel shaders.

Some of the odd design choices are to support future Vulkan, DX12, and other backends.

The component namespaces are:
  * Core, which handles command line processing and resource loading.
  * Wrap, which concerns the native GL types. The naming here is very close to the GL naming. Each of these wrap objects
    owns a single GL handle and a Def object which is used to configure the object on creation. This namespace also
    handles lifetime management with the Disposable abstract class.
  * Render, which adds a higher-level Pass and Frame structure on top of the
    lower level Wrapped GL calls. Naming departs a bit in this area. This is awaiting an actor / appearance? / primtiive
    system to insert into the pass.
  * Engine, which provides a caching system for GL objects and Yaml serialization and deserialization. The caching system is almost essential for good
    shader performance and can really simplify your code when loading GL object Defs from yaml files. You don't have to use it though;
    the demo shows a few different ways to use or not use the caching system for creating samplers.

The demo doesn't show rendering to a target yet, but it's as easy as populating and using your own Framebuffer when creating a Pass. 

Still trying to get HDR working too.

All dependencies are NuGet - Glob (really kind of overkill), OpenTK (for the graphics and windowing stuff), SixLabors.ImageSharp (Loading), and YamlDotNet (Serialization).