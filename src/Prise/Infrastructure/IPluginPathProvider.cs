﻿using System;

namespace Prise.Infrastructure
{
    public interface IPluginPathProvider<T> : IDisposable
    {
        string GetPluginPath();
    }

    public class DefaultPluginPathProvider<T> : IPluginPathProvider<T>
    {
        private readonly string pluginPath;
        private bool disposed = false;

        public DefaultPluginPathProvider(string pluginPath)
        {
            this.pluginPath = pluginPath;
        }

        public string GetPluginPath() => this.pluginPath;

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
