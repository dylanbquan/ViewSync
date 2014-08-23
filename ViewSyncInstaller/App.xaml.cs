using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Autodesk.RevitAddIns;

namespace ViewSyncInstaller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        InstallWindow mainWindow;
        Thread installThread;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            mainWindow = new InstallWindow(InstallDataVS.CommandName + " Installer", 
                "Installing " + InstallDataVS.CommandName + " ...");
            this.MainWindow = mainWindow;
            mainWindow.Show();
            while (!mainWindow.IsInitialized) ;
            
            installThread = new Thread(new ThreadStart(Install));
            installThread.Start();
        }

        public void Install()
        {
            Thread.Sleep(1000); //initial stall

            string[] versions = InstallDataVS.Versions.Split(',', ' ');
            
            List<RevitProduct> products = RevitProductUtility.GetAllInstalledRevitProducts();
            //TODO: group products by year, write dll once per year group,
            // verify year group uses same manifest location, use one install item per year group

            foreach(RevitProduct product in products)
            {
                if (product.Version == RevitVersion.Unknown) continue;

                string versionYear = product.Version.ToString().Substring("Revit".Length); //enum Revit####
                if(!versions.Contains<string>(versionYear)) continue;

                InstallItem install = new InstallItem(versionYear);
                mainWindow.AddInstallItem(install);

                //make progress visible
                for (int fakeProgress = 0; fakeProgress < 10; fakeProgress++)
                {
                    double level = (fakeProgress / 10.0);
                    install.Level = level;
                    install.Message = string.Format("Installing for {0}... {1:#%}", product.Name, level);
                    Thread.Sleep(100);
                }

                string dllPath = WriteProgramFiles(versionYear);
                if (dllPath == null || !WriteAddInManifest(dllPath, product))
                {
                    //fail installation for this product
                    install.Message = string.Format("Installation for {0} failed.", product.Name);
                    install.Level = 0.0;
                    continue;
                }

                install.Level = 1.0;
                install.Message = string.Format("Installed for {0} {1:#%}", product.Name, 1.0);
            }

            mainWindow.Message = "Installation Complete!";
            mainWindow.Complete();
        }

        string WriteProgramFiles(string version)
        {
            string dllPath = null;
            try
            {
                //copy dll to programfiles

                //TODO: get dll stream from resource file not link
                Stream libraryStream = 
                    typeof(App).Assembly.GetManifestResourceStream(
                    typeof(App).Namespace + '.' + InstallDataVS.Namespace + version + ".dll");
                if(libraryStream == null) return null;

                string deploymentLocation = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\" + 
                    InstallDataVS.InstallFolder + "\\" + 
                    InstallDataVS.CommandName + "\\";
                if(!Directory.Exists(deploymentLocation)) Directory.CreateDirectory(deploymentLocation);
                
                dllPath = deploymentLocation + InstallDataVS.Namespace + version + ".dll";
                libraryStream.CopyTo(new FileStream(dllPath, FileMode.Create));

            } catch(Exception) {
                return null;
            }
            return dllPath;
        }

        bool WriteAddInManifest(string dllPath, RevitProduct product)
        {
            try
            {
                //create application and/or command entries
                RevitAddInCommand addInComm = new RevitAddInCommand(
                    dllPath,
                    new Guid(InstallDataVS.CommandGuid),
                    InstallDataVS.ClassFullName,
                    InstallDataVS.VendorName);

                addInComm.VendorDescription = InstallDataVS.VendorDescription;
                addInComm.Text = InstallDataVS.CommandName;
                addInComm.VisibilityMode = VisibilityMode.NotVisibleWhenNoActiveDocument;

                //create manifest and add apps/comms
                RevitAddInManifest manifest = new RevitAddInManifest();
                manifest.AddInCommands.Add(addInComm);

                //save manifest to file
                if (!Directory.Exists(product.CurrentUserAddInFolder))
                    Directory.CreateDirectory(product.CurrentUserAddInFolder);

                manifest.SaveAs(product.CurrentUserAddInFolder + "\\" + InstallDataVS.Namespace + ".addin");
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
