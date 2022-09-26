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
        public static string[] args = new string[] { };
        //     /====/ /======/ /==/   /===============/
        // --foo bar;baz    => bar;baz
        // --foo:bar        => bar (colon is ignored)
        // --foo+           => +
        // --foo/bar        => /bar
       public static string GetString(string tag, string d = "") {
            int index = 0;
            foreach (var arg in args) {
                if (arg == tag) {
                    if (index + 1 < args.Length) {
                        return args[index + 1];
                    }
                } else if (arg.StartsWith(tag)) {
                    if (tag[0] == ':') {
                        return arg.Substring(tag.Length + 1);
                    } else {
                        return arg.Substring(tag.Length);
                    }
                }
                ++index;
            }
            return d;
        }
        public static bool GetFlag(string tag, bool d = false) {
            var s = GetString(tag, d.ToString());
            return s.Length == 0 || !("NnFf-".Contains(s[0]));
        }
        public static int GetInt(string tag, int d = 0) {
            var s = GetString(tag, d.ToString());
            return Convert.ToInt32(s);
        }
        public static (int x, int y) GetInt2(string tag, int x = 0, int y = 0) {
            var s = GetString(tag, $"{x},{y}");
            var ss = s.Split(',');
            return (Convert.ToInt32(ss[0]), Convert.ToInt32(ss[1]));
        }
        public static string FindRoot() {
            var current = Fwd(Directory.GetCurrentDirectory());
            var parts = current.Split('/').ToList();
            parts.RemoveAt(parts.Count - 1);
            while(parts.Any()) {
                var path = Path.Combine(parts.ToArray());
                if(Directory.Exists($"{path}/shaders")) {
                    return path;
                }
                parts.RemoveAt(parts.Count - 1);
            }
            return current;
        }
        public static void Initialize(bool findRoot, string[] args_) {
            args = args_;
            string rootPath = findRoot ? FindRoot() : GetString("--root", "./");
            foreach (var entry in rootPath.Split(new[] { ';' })) {
                var normalized = Path.GetFullPath(entry);
                normalized = Fwd(normalized);
                shaderPath.AddRange(Glob.Directories(normalized, "**/shaders").Select(x => normalized + Fwd(x)));
                texturePath.AddRange(Glob.Directories(normalized, "**/textures").Select(x => normalized + Fwd(x)));
            }
        }
        public static void Initialize(string[] args_) {
            Initialize(true, args);
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
