using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Prise.Infrastructure.NetCore
{
    public abstract class PluginLoader
    {
        protected Assembly pluginAssembly;
        protected List<IDisposable> disposables;

        protected PluginLoader()
        {
            this.disposables = new List<IDisposable>();
        }

        protected T[] LoadPluginsOfType<T>(IPluginLoadOptions<T> pluginLoadOptions)
        {
            var assemblyName = GetAssemblyName(pluginLoadOptions);
            this.pluginAssembly = pluginLoadOptions.AssemblyLoader.Load(assemblyName);
            return CreatePluginInstances(pluginLoadOptions, ref this.pluginAssembly);
        }

        protected async Task<T[]> LoadPluginsOfTypeAsync<T>(IPluginLoadOptions<T> pluginLoadOptions)
        {
            var assemblyName = GetAssemblyName(pluginLoadOptions);
            this.pluginAssembly = await pluginLoadOptions.AssemblyLoader.LoadAsync(assemblyName);
            return CreatePluginInstances(pluginLoadOptions, ref this.pluginAssembly);
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
                throw new PrisePluginException($"IPluginAssemblyNameProvider returned an empty assembly name for plugin type {typeof(T).Name}");

            return assemblyName;
        }

        protected T[] CreatePluginInstances<T>(IPluginLoadOptions<T> pluginLoadOptions, ref Assembly pluginAssembly)
        {
            var pluginInstances = new List<T>();
            var pluginTypes = pluginAssembly
                            .GetTypes()
                            .Where(t => t.CustomAttributes
                                .Any(c => c.AttributeType.Name == typeof(Prise.Infrastructure.PluginAttribute).Name
                                && (c.NamedArguments.First(a => a.MemberName == "PluginType").TypedValue.Value as Type).Name == typeof(T).Name))
                            .OrderBy(t => t.Name)
                            .AsEnumerable();

            if (pluginTypes == null || !pluginTypes.Any())
                throw new PrisePluginException($@"No plugin was found in assembly {pluginAssembly.FullName}. Requested plugin type: {typeof(T).Name}. Please add the {nameof(PluginAttribute)} to your plugin class and specify the PluginType: [Plugin(PluginType = typeof({typeof(T).Name}))]");

            pluginTypes = pluginLoadOptions.PluginSelector.SelectPlugins(pluginTypes);

            if (!pluginTypes.Any())
                throw new PrisePluginException($@"Selector returned no plugin for {pluginAssembly.FullName}. Requested plugin type: {typeof(T).Name}. Please add the {nameof(PluginAttribute)} to your plugin class and specify the PluginType: [Plugin(PluginType = typeof({typeof(T).Name}))]");

            foreach (var pluginType in pluginTypes)
            {
                var bootstrapperType = GetPluginBootstrapper(ref this.pluginAssembly, pluginType);
                var pluginFactoryMethod = GetPluginFactoryMethod(pluginType);

                IPluginBootstrapper bootstrapper = null;
                if (bootstrapperType != null)
                {
                    var remoteBootstrapperInstance = pluginLoadOptions.Activator.CreateRemoteBootstrapper(bootstrapperType, pluginAssembly);
                    var remoteBootstrapperProxy = pluginLoadOptions.ProxyCreator.CreateBootstrapperProxy(remoteBootstrapperInstance);
                    this.disposables.Add(remoteBootstrapperProxy as IDisposable);
                    bootstrapper = remoteBootstrapperProxy;
                }

                var remoteObject = pluginLoadOptions.Activator.CreateRemoteInstance(pluginType, bootstrapper, pluginFactoryMethod, pluginAssembly);
                var remoteProxy = pluginLoadOptions.ProxyCreator.CreatePluginProxy(remoteObject, pluginLoadOptions);
                this.disposables.Add(remoteProxy as IDisposable);
                pluginInstances.Add(remoteProxy);
            }

            return pluginInstances.ToArray();
        }

        protected Type GetPluginBootstrapper(ref Assembly pluginAssembly, Type pluginType)
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