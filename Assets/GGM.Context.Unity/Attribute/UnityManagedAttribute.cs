using GGM.Context.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GGM.Context.Unity.Attribute
{
    public class UnityManagedAttribute : ManagedAttribute
    {
        public UnityManagedAttribute(ManagedType managedType, string resourcePath = null) : base(managedType)
        {
            ResourcePath = resourcePath;
        }

        public string ResourcePath { get; }
    }
}
