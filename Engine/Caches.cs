using System;
using System.Collections.Generic;
using Argentian.Wrap;
using OpenTK.Graphics;

namespace Argentian.Engine {
    public class Cache<Key, T> where Key : notnull {
        Dictionary<Key, T> cache = new Dictionary<Key, T>();
        Func<Key, T> make;
        public Cache(Func<Key, T> make_) {
            make = make_;
        }
        public Cache<Key, T> ForEach(Action<T> action) {
            foreach(var v in cache.Values)
                action(v);
            return this;
        }
        // You can insert values from any source
        public T Insert(Key key, T value) {
            return cache[key] = value;
        }
        // But missing values will always be made using the key
        public T Get(Key key) {
            if(cache.ContainsKey(key)) {
                return cache[key];
            } else {
                return Insert(key, make(key));
            }
        }
        public T this[Key key] => Get(key);
        public Cache<Key, T> Clear() {
            cache.Clear();
            return this;
        }
    }
    public class YamlCache<T>: Cache<string, T> {
        public YamlCache(List<string> path) 
            : base(key => Yaml.Deserialize<T>(path, key)) {}
    }
    public static partial class Caches {
        //'## Shader Object Cache
        //'
        //'The cache holds compiled shader objects keyed to a source file
        //'compiled against a set of headers. This should also include
        //'defines, permutations, etc.
        record struct ShaderObjectKey(
            OpenTK.Graphics.OpenGL.ShaderType type, 
            string source, 
            List<string> headers);
        //'Wrap the cache.Get(key) with a function that builds the key for us.
        //'The cache factory is LoadShaderObject, which compiles the
        //'shader based on source files.
        public static ShaderHandle ShaderObjectGet(
            OpenTK.Graphics.OpenGL.ShaderType type, 
            string key, 
            List<string> headers
        ) => ShaderObjects[new ShaderObjectKey(type, key, headers)];
        static Cache<ShaderObjectKey, ShaderHandle> ShaderObjects =
            new(arg => ShaderProgram.LoadShaderObject(arg.type, arg.source, arg.headers));
        //'## Shader Program Definition Cache
        //'
        //'Caches ShaderProgram.Def based on filename, conventionally foo.mat
        //'The cache uses a Yaml factory which searches Config.shaderPath for foo.mat.
        //'If found, the yaml file is deserialized to a ShaderProgram.Def
        public static YamlCache<ShaderProgram.Def> ShaderProgramDefs = new(Core.Config.shaderPath);
        //'## Shader Program Cache
        //'
        //'These are compiled from a def file and will use cached shader objects via
        //'ShaderObjectGet if available.
        //' 1. Item A
        //' 1. Item B
        //'      * Item 3
        public static ShaderProgram NewShaderProgram(string defKey)
        => new ShaderProgram(defKey, ShaderProgramDefs[defKey], ShaderObjectGet);
        //'# Texture Cache
        //'Caches textures based on filename. 
        public static Cache<string, Texture> Textures = new(Extensions.LoadTexture);
        //'# Sampler Cache
        //'## Sampler Cache
        //'### Sampler Cache
        //'#### Sampler Cache
        //'##### Sampler Cache
        //'###### Sampler Cache
        //'Caches sampler based on yaml definition
        public static YamlCache<Sampler.Def> SamplerDefs = new(Core.Config.texturePath);
        public static Cache<string, Sampler> Samplers = new(arg => new(arg, SamplerDefs[arg]));
    }
}
