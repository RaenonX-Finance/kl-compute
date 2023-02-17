using KL.Common.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KL.Common.Utils;


public static class EnvironmentConfigHelper {
    public static readonly EnvironmentConfigModel Config = new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .Build()
        .Deserialize<EnvironmentConfigModel>(
            File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml"))
        );
}