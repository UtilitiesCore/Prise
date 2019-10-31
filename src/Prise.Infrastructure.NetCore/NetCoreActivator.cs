using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Prise.Infrastructure.NetCore
{
    public class NetCoreActivator : IRemotePluginActivator
    {
        private readonly ISharedServicesProvider sharedServicesProvider;
        private bool disposed = false;

        public NetCoreActivator(ISharedServicesProvider sharedServicesProvider)
        {
            this.sharedServicesProvider = sharedServicesProvider;
        }

        public object CreateRemoteBootstrapper(Type bootstrapperType, Assembly assembly)
        {
            var contructors = bootstrapperType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var firstCtor = contructors.First();

            if (!contructors.Any())
                throw new NotSupportedException($"No public constructors found for remote bootstrapper {bootstrapperType.Name}");
            if (firstCtor.GetParameters().Any())
                throw new NotSupportedException($"Bootstrapper {bootstrapperType.Name} must contain a public parameterless constructor");
            return Activator.CreateInstance(bootstrapperType);
            // return assembly.CreateInstance(bootstrapperType.FullName);
        }

        public object CreateRemoteInstance(Type pluginType, IPluginBootstrapper bootstrapper, MethodInfo factoryMethod, Assembly assembly)
        {
            var contructors = pluginType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            if (contructors.Count() > 1)
                throw new NotSupportedException($"Multiple public constructors found for remote plugin {pluginType.Name}");

            var firstCtor = contructors.FirstOrDefault();
            if (firstCtor != null && !firstCtor.GetParameters().Any())
                return assembly.CreateInstance(pluginType.FullName);
            // return Activator.CreateInstance(pluginType);

            if (factoryMethod == null)
                throw new NotSupportedException($@"Plugins must either provide a default parameterless constructor or implement a static factory method.
                    Like; 'public static {pluginType.Name} CreatePlugin(IServiceProvider serviceProvider)");

            if (bootstrapper == null)
                throw new NotSupportedException($"The type requires dependencies, please provide a {nameof(IPluginBootstrapper)} for plugin {pluginType.Name}");

            var serviceProvider = CreateServiceProviderForType(bootstrapper);
            return factoryMethod.Invoke(null, new[] { serviceProvider });
        }

        private IServiceProvider CreateServiceProviderForType(IPluginBootstrapper bootstrapper)
        {
            var sharedServices = this.sharedServicesProvider.ProvideSharedServices();
            return bootstrapper.Bootstrap(sharedServices).BuildServiceProvider();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing)
            {
                // Nothing to do here
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}