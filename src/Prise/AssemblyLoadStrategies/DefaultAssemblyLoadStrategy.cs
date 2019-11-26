﻿using System;
using System.Linq;
using System.Reflection;
using Prise.Infrastructure;

namespace Prise
{
    /// <summary>
    /// 
    /// </summary>
    public class DefaultAssemblyLoadStrategy : IAssemblyLoadStrategy
    {
        protected IPluginLoadContext pluginLoadContext;
        protected IPluginDependencyContext pluginDependencyContext;

        public DefaultAssemblyLoadStrategy(IPluginLoadContext pluginLoadContext, IPluginDependencyContext pluginDependencyContext)
        {
            this.pluginLoadContext = pluginLoadContext;
            this.pluginDependencyContext = pluginDependencyContext;
        }

        public virtual Assembly LoadAssembly(AssemblyName assemblyName,
            Func<IPluginLoadContext, AssemblyName, ValueOrProceed<Assembly>> loadFromDependencyContext,
            Func<IPluginLoadContext, AssemblyName, ValueOrProceed<Assembly>> loadFromRemote,
            Func<IPluginLoadContext, AssemblyName, ValueOrProceed<Assembly>> loadFromAppDomain)
        {
            if (assemblyName.Name == null)
                return null;

            ValueOrProceed<Assembly> valueOrProceed = ValueOrProceed<Assembly>.FromValue(null, true);

            if (IsHostAssembly(assemblyName) && !IsRemoteAssembly(assemblyName)) // Load from Default App Domain (host)
            {
                valueOrProceed = loadFromAppDomain(this.pluginLoadContext, assemblyName);
                if (valueOrProceed.Value != null)
                    return null; // fallback to default loading mechanism
            }

            if (valueOrProceed.CanProceed)
                valueOrProceed = loadFromDependencyContext(this.pluginLoadContext, assemblyName);

            if (valueOrProceed.CanProceed)
                valueOrProceed = loadFromRemote(this.pluginLoadContext, assemblyName);

            return valueOrProceed.Value;
        }

        public virtual NativeAssembly LoadUnmanagedDll(string unmanagedDllName,
            Func<IPluginLoadContext, string, ValueOrProceed<string>> loadFromDependencyContext,
            Func<IPluginLoadContext, string, ValueOrProceed<string>> loadFromRemote,
            Func<IPluginLoadContext, string, ValueOrProceed<IntPtr>> loadFromAppDomain)
        {
            ValueOrProceed<string> valueOrProceed = ValueOrProceed<string>.FromValue(String.Empty, true);
            ValueOrProceed<IntPtr> ptrValueOrProceed = ValueOrProceed<IntPtr>.FromValue(IntPtr.Zero, true);

            valueOrProceed = loadFromDependencyContext(this.pluginLoadContext, unmanagedDllName);

            if (valueOrProceed.CanProceed)
                ptrValueOrProceed = loadFromAppDomain(this.pluginLoadContext, unmanagedDllName);

            if (valueOrProceed.CanProceed && ptrValueOrProceed.CanProceed)
                valueOrProceed = loadFromRemote(this.pluginLoadContext, unmanagedDllName);

            return NativeAssembly.Create(valueOrProceed.Value, ptrValueOrProceed.Value);
        }

        protected virtual bool IsHostAssembly(AssemblyName assemblyName) => this.pluginDependencyContext.HostDependencies.Any(h => h.DependencyName.Name == assemblyName.Name);
        protected virtual bool IsRemoteAssembly(AssemblyName assemblyName) => this.pluginDependencyContext.RemoteDependencies.Any(r => r.DependencyName.Name == assemblyName.Name);
    }
}
