using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace labs_coordinate_pictures
{
    class ClassConfigs
    {
        HashSet<string> m_supportedKeys;
        ClassConfigs()
        {
            m_supportedKeys = new HashSet<string> { "EnablePersonalFeatures", "b" };
        }
        public bool EnablePersonalFeatures { get; set; }
    }
}
