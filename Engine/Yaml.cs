using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using CommunityToolkit.Diagnostics;
using YamlDotNet.Core;
using System.Text.RegularExpressions;

namespace Argentian.Engine {
    public class JetsonTypeResolver: INodeTypeResolver {
        // TODO - build from assembly name
        public List<string> path = new() { "", "Argentian.", "Argentian.Core.", "Argentian.Engine.", "Argentian.Wrap." };
        public bool Resolve(NodeEvent? nodeEvent, ref Type currentType) {
            Console.WriteLine($"{nodeEvent?.Tag ?? "null"} : {currentType}");
            if (nodeEvent != null &&
                !nodeEvent.Tag.IsEmpty &&
                !nodeEvent.Tag.IsNonSpecific &&
                nodeEvent.Tag.Value.StartsWith("!")) {
                string s = nodeEvent.Tag.Value.Substring(1);
                s = s.Replace('/', '+');

                foreach (var p in path) {
                    var type = Type.GetType($"{p}{s}");
                    if (type != null) {
                        currentType = type;
                        return true;
                    }
                }
            }

            return false;
        }
    }
    // public class JetsonNodeDeserializer: INodeDeserializer {
    //     public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value) {
    //         return true;
    //     }
    // }
    public static class Yaml {
        public static IDeserializer Deserializer = new DeserializerBuilder().
            WithNodeTypeResolver(new JetsonTypeResolver()).
            // WithNamingConvention(NullNamingConvention.Instance).
            // WithNodeDeserializer(new JetsonNodeDeserializer()).
            Build();
        public static ISerializer Serializer = new SerializerBuilder().
            Build();

        public static T Deserialize<T>(List<string> path, string key) {
            string filename = $"{key}.yaml";
            try {
                var (text, pathFile) = Core.Config.ReadFile(path, filename);
                return Yaml.Deserializer.Deserialize<T>(text);
            } catch (YamlDotNet.Core.YamlException e) {
                return ThrowHelper.ThrowInvalidDataException<T>($"Couldn't load {typeof(T)} from {filename}", e);
            } catch (FileNotFoundException) {
                throw;
            }
        }
    }
}
