using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GlobExpressions;

namespace Argentian.Core {
    public static class Config {
        public static List<string> dataPath = new List<string>();
        public static List<string> shaderPath = new List<string>();
        public static List<string> texturePath = new List<string>();
        public static string[] args = new string[]{ };

        public static bool GetFlag(string tag, bool d = false) {
            foreach (var arg in args) {
                if (arg.StartsWith(tag)) {
                    var opt = arg.Substring(tag.Length);
                    if (opt == "-")
                        return false;
                    else if (opt == "+")
                        return true;
                    else if (opt == "")
                        return true;
                    else
                        throw new InvalidDataException($"Unrecognized flag {arg}");
                }
            }
            return d;
        }
        public static string GetString(string tag, string d = "") {
            int index = 0;
            foreach (var arg in args) {
                if (arg.StartsWith(tag)) {
                    var opt= arg.Substring(tag.Length);
                    if (opt.Length == 0) {
                        return args[index + 1];
                    } else {
                        return opt;
                    }
                }
                ++index;
            }
            return d;
        }
        public static string FindRoot() {
            var current = Fwd(Directory.GetCurrentDirectory());
            var parts = current.Split('/').ToList();
            parts.RemoveAt(parts.Count - 1);
            while(parts.Any()) {
                var path = Path.Combine(parts.ToArray());
                if(Directory.Exists($"{path}/shaders") &&
                    Directory.Exists($"{path}/textures")) {
                    return path;
                }
                parts.RemoveAt(parts.Count - 1);
            }
            return current;
        }
        public static void Initialize(string[] args_) {
            args = args_;
            string rootPath = GetString("--root", "./");

            foreach (var entry in rootPath.Split(new[] { ';' })) {
                var normalized = Path.GetFullPath(entry);
                normalized = Fwd(normalized);
                shaderPath.AddRange(Glob.Directories(normalized, "**/shaders").Select(x => normalized + Fwd(x)));
                texturePath.AddRange(Glob.Directories(normalized, "**/textures").Select(x => normalized + Fwd(x)));
            }
        }
        static string Fwd(string x, bool path = true) {
            x = x.Replace('\\', '/');
            if(path && !x.EndsWith('/'))
                x += '/';
            return x;
        }

        public static (string text, string pathFile) ReadFile(List<string> paths, string filename) {
            var (reader, pathFile) = ReadStreamReader(paths, filename);
            return (reader.ReadToEnd(), pathFile);
        }
        public static (StreamReader stream, string path) ReadStreamReader(List<string> paths, string filename) {
            var (stream, pathFile) = ReadStream(paths, filename);
            return (new StreamReader(stream), pathFile);
        }
        public static (FileStream stream, string path) ReadStream(List<string> paths, string filename) {
            foreach (var (stream, pathFile) in ReadStreams(paths, filename)) {
                // just return the first one
                return (stream, pathFile);
            }
            throw new FileNotFoundException($"Couldn't find {filename}");
        }
        public static IEnumerable<(FileStream stream, string path)> ReadStreams(List<string> paths, string filename) {
            foreach (var entry in paths) {
                FileStream? found = null;
                var pathFile = $"{entry}{filename}";
                try {
                    found = new FileStream(pathFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                } catch (Exception) {
                }
                if (found != null)
                    yield return (found, pathFile);
            }
        }
    }
}
