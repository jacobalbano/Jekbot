using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Utility
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class AutoDiscoverImplementationsAttribute : Attribute
    {
    }

    public static class AutoDiscoverImplementationsAttributeExtensions
    {
        public static ServiceCollection DiscoverTaggedInterfaces(this ServiceCollection services)
        {
            foreach (var t in typeof(AutoDiscoverSingletonServiceAttribute)
                .Assembly.GetExportedTypes()
                .Where(x => x.IsClass && !x.IsAbstract && x.GetCustomAttribute<AutoDiscoverImplementationsAttribute>(true) != null))
            {
                foreach (var iface in t.GetInterfaces()
                    .Where(x => x.GetCustomAttribute<AutoDiscoverImplementationsAttribute>() != null))
                    services.AddTransient(iface, t);
            }

            return services;
        }
    }
}
