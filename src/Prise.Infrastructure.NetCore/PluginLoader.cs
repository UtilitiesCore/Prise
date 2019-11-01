using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Prise.Infrastructure;

namespace Prise.Infrastructure.NetCore
{
    public abstract class PluginLoader
    {

        protected T[] LoadPluginsOfType<T>(IPluginLoadOptions<T> pluginLoadOptions)
        {
            var assemblyName = GetAssemblyName(pluginLoadOptions);
            var assembly = pluginLoadOptions.AssemblyLoader.Load(assemblyName);
            return CreatePluginInstances(pluginLoadOptions, assembly);
        }

        protected async Task<T[]> LoadPluginsOfTypeAsync<T>(IPluginLoadOptions<T> pluginLoadOptions)
        {
            var assemblyName = GetAssemblyName(pluginLoadOptions);
            var assembly = await pluginLoadOptions.AssemblyLoader.LoadAsync(assemblyName);
            return CreatePluginInstances(pluginLoadOptions, assembly);
        }

        protected void Unload<T>(IPluginLoadOptions<T> pluginLoadOptions)
        {
            pluginLoadOptions.AssemblyLoader.Unload();
        }

        protected async Task UnloadAsync<T>(IPluginLoadOptions<T> pluginLoadOptions)
        {
            await pluginLoadOptions.AssemblyLoader.UnloadAsync();
        }

        protected string GetAssemblyName<T>(IPluginLoadOptions<T> pluginLoadOptions)
        {
            var assemblyName = pluginLoadOptions.PluginAssemblyNameProvider.GetAssemblyName();
            if (String.IsNullOrEmpty(assemblyName))
                throw new NotSupportedException($"IPluginAssemblyNameProvider returned an empty assembly name for plugin type {typeof(T).Name}");

            return assemblyName;
        }

        protected T[] CreatePluginInstances<T>(IPluginLoadOptions<T> pluginLoadOptions, Assembly pluginAssembly)
        {
            var pluginInstances = new List<T>();
            try
            {
                var pluginTypes = pluginAssembly
                                .GetTypes()
                                .Where(t => t.CustomAttributes
                                    .Any(c => c.AttributeType.Name == typeof(Prise.Infrastructure.PluginAttribute).Name
                                    && (c.NamedArguments.First(a => a.MemberName == "PluginType").TypedValue.Value as Type).Name == typeof(T).Name))
                                .OrderBy(t => t.Name);

                if (pluginTypes == null || !pluginTypes.Any())
                    throw new FileNotFoundException($@"No plugin was found in assembly {pluginAssembly.FullName}. Requested plugin type: {typeof(T).Name}. Please add the {nameof(PluginAttribute)} to your plugin class and specify the PluginType: [Plugin(PluginType = typeof({typeof(T).Name}))]");

                foreach (var pluginType in pluginTypes)
                {
                    var bootstrapperType = GetPluginBootstrapper(pluginAssembly, pluginType);
                    var pluginFactoryMethod = GetPluginFactoryMethod(pluginType);

                    IPluginBootstrapper bootstrapper = null;
                    if (bootstrapperType != null)
                    {
                        var remoteBootstrapperInstance = pluginLoadOptions.Activator.CreateRemoteBootstrapper(bootstrapperType, pluginAssembly);
                        var remoteBootstrapperProxy = pluginLoadOptions.ProxyCreator.CreateBootstrapperProxy(remoteBootstrapperInstance);
                        bootstrapper = remoteBootstrapperProxy;
                        // if (remoteBootstrapperInstance as IPluginBootstrapper == null)
                        //     throw new NotSupportedException("Version type mismatch");
                        // bootstrapper = remoteBootstrapperInstance as IPluginBootstrapper;
                    }

                    var remoteObject = pluginLoadOptions.Activator.CreateRemoteInstance(pluginType, bootstrapper, pluginFactoryMethod, pluginAssembly);
                    var remoteProxy = pluginLoadOptions.ProxyCreator.CreatePluginProxy(remoteObject, pluginLoadOptions);
                    pluginInstances.Add(remoteProxy);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception exSub in ex.LoaderExceptions)
                {
                    sb.AppendLine(exSub.Message);
                    FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb.AppendLine("Fusion Log:");
                            sb.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb.AppendLine();
                }
                string errorMessage = sb.ToString();
                throw new Exception(errorMessage);
                //Display or log the error based on your application.
            }

            return pluginInstances.ToArray();
        }

        protected Type GetPluginBootstrapper(Assembly pluginAssembly, Type pluginType)
        {
            return pluginAssembly
                    .GetTypes()
                    .Where(t => t.CustomAttributes
                        .Any(c => c.AttributeType.Name == typeof(Prise.Infrastructure.PluginBootstrapperAttribute).Name &&
                        (c.NamedArguments.First(a => a.MemberName == "PluginType").TypedValue.Value as Type).Name == pluginType.Name)).FirstOrDefault();
        }

        protected MethodInfo GetPluginFactoryMethod(Type pluginType)
        {
            return pluginType.GetMethods()
                    .Where(m => m.CustomAttributes
                        .Any(c => c.AttributeType.Name == typeof(Prise.Infrastructure.PluginFactoryAttribute).Name)).FirstOrDefault();
        }
    }
}