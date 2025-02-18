using BlazorReflectionSample.Components;

using ControlSampleContract;

using McMaster.NETCore.Plugins;

using System.Reflection;

namespace BlazorReflectionSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var pluginsDir = builder.Configuration.GetValue<string>("PluginsDir");

            var loaders = new List<PluginLoader>();

            // create plugin loaders
            //var pluginsDir = Path.Combine(AppContext.BaseDirectory, "plugins");
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
                    var methodName = "AddTextBoxControl";

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

                    var services = builder.Services;

                    // Invoke the method
                    method.Invoke(null, new object[] {  services });
                }
            }


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
