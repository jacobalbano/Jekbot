using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Utility
{
    public class ResourceEnvironment
    {
        public ResourceEnvironment(string? rootDir = null)
        {
            root = rootDir ?? "prepared";
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);
        }

        public string GetPath(string path)
        {
            return Path.Combine(root, path);
        }

        private readonly string root;
    }
}
