using System.Reflection;

namespace Test
{
    public static class EmbeddedResource
    {
        public static string Get(string embeddedResourceName)
        {
            if (embeddedResourceName is null)
            {
                throw new ArgumentNullException(nameof(embeddedResourceName));
            }

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly
                .GetManifestResourceNames()
                .SingleOrDefault(resource => resource.EndsWith(embeddedResourceName))
                ?? throw new InvalidOperationException($"Could not find resource in assembly with name {embeddedResourceName}");

            var resourceStream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException("Could not load manifest resource from assembly");

            using Stream stream = resourceStream;
            using StreamReader reader = new(stream);

            var json = reader.ReadToEnd();

            return json;
        }
    }
}
