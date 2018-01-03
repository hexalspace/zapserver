using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zapserver
{
    interface IKeyValueStore
    {
        string getValue(string key);
        void setValue(string key, string value);
    }
}
