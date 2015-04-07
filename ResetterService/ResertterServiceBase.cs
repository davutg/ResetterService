using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace ResetterService
{
    public partial class ResertterServiceBase:ServiceBase
    {
        public virtual void FireOnStart(string[] args)
        {
            base.OnStart(args);
        }

        public virtual void FireOnStop()
        {
            base.OnStop();
        }

        public virtual string DisplayName { get; set; }

    }
}
