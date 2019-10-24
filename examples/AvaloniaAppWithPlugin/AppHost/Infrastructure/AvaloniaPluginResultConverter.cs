using System;
using Prise.Infrastructure.NetCore;

namespace AppHost.Infrastructure
{
    public class AvaloniaPluginResultConverter : ResultConverterBase
    {
        public override object Deserialize(Type localType, Type remoteType, object value)
        {
            // No conversion, no backwards compatibility
            // When the host upgrades any Avalonia dependency, it will break
            return value;
        }
    }
}