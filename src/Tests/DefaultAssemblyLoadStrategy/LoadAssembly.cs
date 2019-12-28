﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Prise.Infrastructure;
using Xunit;

namespace Prise.Tests.DefaultAssemblyLoadStrategy
{
    public class LoadAssembly : TestBase
    {
        [Fact]
        public void Returns_Null_For_Empty_AssemblyName()
        {
            // Arrange
            var pluginLoadContext = this.Mock<IPluginLoadContext>();
            var pluginDependencyContext = this.Mock<IPluginDependencyContext>();
            var sut = new Prise.DefaultAssemblyLoadStrategy(pluginLoadContext, pluginDependencyContext);
            var emptyAssemblyname = new AssemblyName();

            // Act, Assert
            Assert.Null(sut.LoadAssembly(emptyAssemblyname, null, null, null));
        }

        [Fact]
        public void Returns_Null_When_AssemblyName_NotFound()
        {
            // Arrange
            var pluginLoadContext = this.Mock<IPluginLoadContext>();
            var pluginDependencyContext = this.Mock<IPluginDependencyContext>();
            var sut = new Prise.DefaultAssemblyLoadStrategy(pluginLoadContext, pluginDependencyContext);
            var assemblyname = new AssemblyName(this.CreateFixture<string>());
            var loadFromDependencyContext = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());
            var loadFromRemote = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());
            var loadFromAppDomain = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());
            this.Arrange<IPluginDependencyContext>()
                .Setup(p => p.HostDependencies).Returns(Enumerable.Empty<HostDependency>());

            // Act, Assert
            Assert.Null(sut.LoadAssembly(assemblyname, loadFromDependencyContext, loadFromRemote, loadFromAppDomain));
        }

        [Fact]
        public void Returns_Null_From_AppDomain_When_IsHostAssembly_And_Not_RemoteAssembly()
        {
            // Arrange
            var someAssembly = GetRealAssembly();
            var someAssemblyName = someAssembly.GetName();
            var pluginLoadContext = this.Mock<IPluginLoadContext>();
            var pluginDependencyContext = this.Mock<IPluginDependencyContext>();
            var sut = new Prise.DefaultAssemblyLoadStrategy(pluginLoadContext, pluginDependencyContext);
            var loadFromDependencyContext = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());
            var loadFromRemote = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());
            var loadFromAppDomain = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.FromValue(someAssembly, false));

            // Mock the fact that it was setup as a host assembly that should be loaded from the host.
            this.Arrange<IPluginDependencyContext>()
                .Setup(p => p.HostDependencies).Returns(new List<HostDependency> { new HostDependency { DependencyName = someAssemblyName } });

            this.Arrange<IPluginDependencyContext>()
                .Setup(p => p.RemoteDependencies).Returns(Enumerable.Empty<RemoteDependency>());

            // Act, Assert
            Assert.Null(sut.LoadAssembly(someAssemblyName, loadFromDependencyContext, loadFromRemote, loadFromAppDomain));
        }

        [Fact]
        public void Returns_Null_From_AppDomain_When_NOT_IsHostAssembly_And_RemoteAssembly()
        {
            // Arrange
            var someAssembly = GetRealAssembly();
            var someAssemblyName = someAssembly.GetName();
            var pluginLoadContext = this.Mock<IPluginLoadContext>();
            var pluginDependencyContext = this.Mock<IPluginDependencyContext>();
            var sut = new Prise.DefaultAssemblyLoadStrategy(pluginLoadContext, pluginDependencyContext);
            var loadFromDependencyContext = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());
            var loadFromRemote = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());
            var loadFromAppDomain = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());

            // Mock the fact that it was setup as a host assembly that should be loaded from the host.
            this.Arrange<IPluginDependencyContext>()
                .Setup(p => p.HostDependencies).Returns(new List<HostDependency> { new HostDependency { DependencyName = someAssemblyName } });

            // Mock the fact that it was ALSO setup as a remote assembly that should be loaded from the plugin.
            this.Arrange<IPluginDependencyContext>()
                .Setup(p => p.RemoteDependencies).Returns(new List<RemoteDependency> { new RemoteDependency { DependencyName = someAssemblyName } });

            // Act, Assert
            Assert.Null(sut.LoadAssembly(someAssemblyName, loadFromDependencyContext, loadFromRemote, loadFromAppDomain));
        }

        [Fact]
        public void Returns_Assembly_From_DependencyContext()
        {
            // Arrange
            var someAssembly = GetRealAssembly();
            var someAssemblyName = someAssembly.GetName();
            var pluginLoadContext = this.Mock<IPluginLoadContext>();
            var pluginDependencyContext = this.Mock<IPluginDependencyContext>();
            var sut = new Prise.DefaultAssemblyLoadStrategy(pluginLoadContext, pluginDependencyContext);
            var loadFromDependencyContext = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.FromValue(someAssembly, false));
            var loadFromRemote = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());
            var loadFromAppDomain = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());

            this.Arrange<IPluginDependencyContext>()
                .Setup(p => p.HostDependencies).Returns(Enumerable.Empty<HostDependency>());

            // Act, Assert
            Assert.Equal(someAssembly, sut.LoadAssembly(someAssemblyName, loadFromDependencyContext, loadFromRemote, loadFromAppDomain));
        }

        [Fact]
        public void Returns_Assembly_From_Remote()
        {
            // Arrange
            var someAssembly = GetRealAssembly();
            var someAssemblyName = someAssembly.GetName();
            var pluginLoadContext = this.Mock<IPluginLoadContext>();
            var pluginDependencyContext = this.Mock<IPluginDependencyContext>();
            var sut = new Prise.DefaultAssemblyLoadStrategy(pluginLoadContext, pluginDependencyContext);
            var loadFromDependencyContext = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());
            var loadFromRemote = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.FromValue(someAssembly, false));
            var loadFromAppDomain = CreateLookupFunction((c, a) => ValueOrProceed<Assembly>.Proceed());

            this.Arrange<IPluginDependencyContext>()
                .Setup(p => p.HostDependencies).Returns(Enumerable.Empty<HostDependency>());

            // Act, Assert
            Assert.Equal(someAssembly, sut.LoadAssembly(someAssemblyName, loadFromDependencyContext, loadFromRemote, loadFromAppDomain));
        }

        // You cannot create an instance of the Abstract Assembly class, instead of constructing a valid Assembly, return the entry assembly from the unit test
        private Assembly GetRealAssembly() => System.Reflection.Assembly.GetEntryAssembly();

        private Func<IPluginLoadContext, AssemblyName, ValueOrProceed<Assembly>> CreateLookupFunction(Func<IPluginLoadContext, AssemblyName, ValueOrProceed<Assembly>> func) => func;
    }
}
