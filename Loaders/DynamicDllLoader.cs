using ControlSampleContract;
using McMaster.NETCore.Plugins;
using System.Reflection;

namespace BlazorReflectionSample.Loaders
{
    public class DynamicDllLoader
    {
        public static void LoadExternalAssemblies(WebApplicationBuilder builder)
        {

            var pluginsDir = builder.Configuration.GetValue<string>("PluginsDir");

            string[] dllNames = Directory.GetDirectories(pluginsDir!)
            .Select(dir => Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar)))
            .ToArray();

            bool IsFound = false;

            foreach (string dllName in dllNames)
            {
                // Get all assemblies currently loaded in the application domain
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    string referencedDllName = assembly.FullName!.Substring(0, assembly.FullName.IndexOf(','));
                    // Check if the assembly is already loaded
                    if (dllName == referencedDllName)
                    {
                        Console.WriteLine($"Assembly already loaded: {assembly.FullName}");
                        IsFound = true;
                        break;
                    }
                }
                // If the assembly is not loaded, load it with reflection
                if (IsFound == false)
                {
                    LoadAndConfigureServices(builder.Services, pluginsDir!); // Cast builder.Services to IServiceCollection
                }
                // If the assembly is already loaded, restart IsFound = false to search a new
                // assembly from the list
                else
                {
                    IsFound = false;
                }
            }
        }

        public static void LoadAndConfigureServices(IServiceCollection services, string pluginsDir, string methodName = "AddControl")
        {
            var loaders = new List<PluginLoader>();

            // Create plugin loaders            
            foreach (var dir in Directory.GetDirectories(pluginsDir!))
            {
                var dirName = Path.GetFileName(dir);
                var pluginDll = Path.Combine(dir, dirName + ".dll");
                if (File.Exists(pluginDll))
                {
                    var loader = PluginLoader.CreateFromAssemblyFile(
                        pluginDll,
                        sharedTypes: new[] { typeof(IControlContract) });
                    loaders.Add(loader);
                }
            }

            // Create an instance of plugin types
            foreach (var loader in loaders)
            {
                foreach (var control in loader
                    .LoadDefaultAssembly()
                    .GetTypes()
                    .Where(t => typeof(IControlContract).IsAssignableFrom(t) && !t.IsAbstract))
                {
                    // Get all types in the assembly
                    var types = loader
                    .LoadDefaultAssembly().GetTypes();


                    // Find the type containing the extension method
                    var targetType = types.FirstOrDefault(t =>
                        t.GetMethods(BindingFlags.Static | BindingFlags.Public)
                         .Any(m => m.Name == methodName &&
                                   m.GetParameters().Length == 1 &&
                                   m.GetParameters()[0].ParameterType == typeof(IServiceCollection)));

                    if (targetType == null)
                    {
                        throw new InvalidOperationException($"No type with a method named '{methodName}' found in assembly");
                    }

                    // Find the method
                    var method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);

                    if (method == null)
                    {
                        throw new InvalidOperationException($"Method '{methodName}' not found in type '{targetType.FullName}'.");
                    }

                    // Invoke the method
                    method.Invoke(null, new object[] { services });
                }
            }
        }
    }
}
