using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Pr0Notify2
{
    class MyLabel : Label
    {
        public MessageManager.Message msg { get; set; }
    }
}
