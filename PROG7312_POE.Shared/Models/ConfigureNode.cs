using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.Models
{
    public class ConfigureNode
    {
        public string Name { get; }
        public List<ConfigureNode> Children { get; } = new();

        public ConfigureNode(string name)
        {
            Name = name;
        }

        public ConfigureNode AddChild(string name)
        {
            var child = new ConfigureNode(name);
            Children.Add(child);
            return child;
        }
    }
}
