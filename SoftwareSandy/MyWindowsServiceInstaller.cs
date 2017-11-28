using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration.Install;
using System.ServiceProcess;
using System.ComponentModel;

namespace SoftwareSandy
{
    [RunInstaller(true)]
    public class MyWindowsServiceInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public MyWindowsServiceInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.DisplayName = "SoftwareSandy Claims Scrubber";
            serviceInstaller.StartType = ServiceStartMode.Manual;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = "SoftwareSandy Claims Scrubber";

            this.Installers.Add(processInstaller);
            this.Installers.Add(serviceInstaller);
        }

    }
}
