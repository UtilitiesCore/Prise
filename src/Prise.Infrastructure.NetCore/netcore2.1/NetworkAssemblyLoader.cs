using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using Prise.Infrastructure.NetCore.Contracts;

namespace Prise.Infrastructure.NetCore
{
    public class NetworkAssemblyLoadContext : AssemblyLoadContext
    {
        protected readonly HttpClient httpClient;
        protected readonly AssemblyName pluginInfrastructureAssemblyName;
        protected string baseUrl;
        protected DependencyLoadPreference dependencyLoadPreference;
        protected bool isConfigured;

        public NetworkAssemblyLoadContext(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            this.pluginInfrastructureAssemblyName = typeof(Prise.Infrastructure.PluginAttribute).Assembly.GetName();
        }

        internal void Configure(string baseUrl, DependencyLoadPreference dependencyLoadPreference)
        {
            if (this.isConfigured)
                return;

            this.baseUrl = baseUrl;
            this.dependencyLoadPreference = dependencyLoadPreference;

            this.isConfigured = true;
        }

        private Assembly LoadFromRemote(AssemblyName assemblyName)
        {
            var networkAssembly = LoadDependencyFromNetwork(assemblyName);
            if (networkAssembly != null)
                return networkAssembly;

            return null;
        }

        private Assembly LoadFromDependencyContext(AssemblyName assemblyName)
        {
            var defaultDependencies = DependencyContext.Default;
            var candidateAssembly = defaultDependencies.CompileLibraries.FirstOrDefault(d => String.Compare(d.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase) == 0);
            if (candidateAssembly != null)
            {
                return Assembly.Load(new AssemblyName(candidateAssembly.Name));
            }
            return null;
        }

        private Assembly LoadFromAppDomain(AssemblyName assemblyName)
        {
            return Assembly.Load(assemblyName);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (assemblyName.FullName == this.pluginInfrastructureAssemblyName.FullName)
                return null;

            return AssemblyLoadStrategyFactory
                .GetAssemblyLoadStrategy(this.dependencyLoadPreference).LoadAssembly(
                    assemblyName,
                    LoadFromDependencyContext, 
                    LoadFromRemote, 
                    LoadFromAppDomain
                );
        }

        protected virtual Assembly LoadDependencyFromNetwork(AssemblyName assemblyName)
        {
            var name = $"{assemblyName.Name}.dll";
            var dependency = DownloadDependency(name);

            if (dependency == null) return null;

            return Assembly.Load(dependency);
        }

        protected byte[] DownloadDependency(string pluginAssemblyName)
        {
            var response = this.httpClient.GetAsync($"{baseUrl}/{pluginAssemblyName}").Result;
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            return response.Content.ReadAsByteArrayAsync().Result;
        }
    }

    public class NetworkAssemblyLoader<T> : DisposableAssemblyUnLoader, IPluginAssemblyLoader<T>
    {
        protected readonly INetworkAssemblyLoaderOptions options;
        protected readonly HttpClient httpClient;
        protected NetworkAssemblyLoadContext context;

        /// To be used by Dependency Injection
        public NetworkAssemblyLoader(
            INetworkAssemblyLoaderOptions options,
            IHttpClientFactory httpClientFactory) : this(options, httpClientFactory.CreateClient()) { }

        internal NetworkAssemblyLoader(
            INetworkAssemblyLoaderOptions options,
            HttpClient httpClient)
        {
            this.options = options;
            this.httpClient = httpClient;
            this.context = new NetworkAssemblyLoadContext(httpClient);
        }

        public virtual Assembly Load(string pluginAssemblyName)
        {
            var pluginStream = LoadPluginFromNetwork(this.options.BaseUrl, pluginAssemblyName);
            this.context.Configure(this.options.BaseUrl, this.options.DependencyLoadPreference);
            return this.context.LoadFromStream(pluginStream);
        }

        public async virtual Task<Assembly> LoadAsync(string pluginAssemblyName)
        {
            var pluginStream = await LoadPluginFromNetworkAsync(this.options.BaseUrl, pluginAssemblyName);
            this.context.Configure(this.options.BaseUrl, this.options.DependencyLoadPreference);
            return this.context.LoadFromStream(pluginStream);
        }

        protected virtual Stream LoadPluginFromNetwork(string baseUrl, string pluginAssemblyName)
        {
            var response = this.httpClient.GetAsync($"{baseUrl}/{pluginAssemblyName}").Result;
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new FileNotFoundException($"Remote assembly {pluginAssemblyName} not found at {baseUrl}");

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Error loading plugin {pluginAssemblyName} at {baseUrl}");

            return response.Content.ReadAsStreamAsync().Result;
        }

        protected async virtual Task<Stream> LoadPluginFromNetworkAsync(string baseUrl, string pluginAssemblyName)
        {
            var response = await this.httpClient.GetAsync($"{baseUrl}/{pluginAssemblyName}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new FileNotFoundException($"Remote assembly {pluginAssemblyName} not found at {baseUrl}");

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Error loading plugin {pluginAssemblyName} at {baseUrl}");

            return await response.Content.ReadAsStreamAsync();
        }
    }
}