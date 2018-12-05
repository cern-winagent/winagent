using System;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Collections;
using System.ComponentModel;

namespace winagent
{
    class ServiceManager
    {
        // Get Installer instance
        public static AssemblyInstaller GetInstaller()
        {
            AssemblyInstaller installer = new AssemblyInstaller(typeof(Agent.Service).Assembly, null);
            installer.UseNewContext = true;
            return installer;
        }

        // Install the service if it's not installed
        public static void Install()
        {
            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        installer.Install(state);
                        installer.Commit(state);
                    }
                    catch
                    {
                        try
                        {
                            installer.Rollback(state);
                        }
                        catch(Exception e)
                        {
                            // Since AssemblyInstaller prints the error message, just exit
                            Console.Error.WriteLine();
                            Console.Write("Exiting...");
                            Console.Error.WriteLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);

                Console.Error.WriteLine(ex.Message);
            }
        }

        // Uninstall the service if it's installed
        public static void Uninstall()
        {
            try
            {
                using (AssemblyInstaller installer = GetInstaller())
                {
                    IDictionary state = new Hashtable();
                    try
                    {
                        installer.Uninstall(state);
                    }
                    catch (Exception e)
                    {
                        // Since AssemblyInstaller prints the error message, just exit
                        Console.Error.WriteLine();
                        Console.Write("Exiting...");
                        Console.Error.WriteLine();
                    }
                }
            }
            
            
            catch (Exception ex)
            {
                Console.Write(ex);

                Console.Error.WriteLine(ex.Message);
            }
        }

        public static void Start()
        {
            try
            {
                ServiceController controller = new ServiceController("Winagent");
                controller.Start();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Service already started or not installed");
                Console.WriteLine("Exiting...");
            }
            catch (Exception ex)
            {
                Console.Write(ex);

                Console.Error.WriteLine(ex.Message);
            }
        }

        public static void Stop()
        {
            try
            {
                ServiceController controller = new ServiceController("Winagent");
                controller.Stop();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Service already stopped or not installed");
                Console.WriteLine("Exiting...");
            }
            catch (Exception ex)
            {
                Console.Write(ex);

                Console.Error.WriteLine(ex.Message);
            }
        }
    }

    [RunInstaller(true)]
    public sealed class WinagentInstallerProcess : ServiceProcessInstaller
    {
        public WinagentInstallerProcess()
        {
            //this.Account = ServiceAccount.NetworkService;
            this.Account = ServiceAccount.LocalSystem;
        }
    }

    [RunInstaller(true)]
    public sealed class WinagentInstaller : ServiceInstaller
    {
        public WinagentInstaller()
        {
            this.Description = "Windows Agent";
            this.DisplayName = "Winagent";
            this.ServiceName = "Winagent";
            this.StartType = System.ServiceProcess.ServiceStartMode.Manual;
        }
    }

}
