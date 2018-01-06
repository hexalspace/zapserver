using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zapserver
{
    interface IKeyValueStore
    {
        T getValue<T>(string key);
        void setValue(string key, object value);
        void remove(string key);
    }
}
