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
            mainWindow = new InstallWindow(ViewSyncInstaller.Properties.Resources.AppName + " Installer", 
                "Installing " + ViewSyncInstaller.Properties.Resources.AppName + " ...");
            this.MainWindow = mainWindow;
            mainWindow.Show();
            while (!mainWindow.IsInitialized) ;
            
            installThread = new Thread(new ThreadStart(Install));
            installThread.Start();
        }

        public void Install()
        {
            Thread.Sleep(1000); //initial stall

            Guid guid = new Guid(ViewSyncInstaller.Properties.Resources.AppGuid);
            string[] versions = ViewSyncInstaller.Properties.Resources.Versions.Split(',', ' ');
            
            List<RevitProduct> products = RevitProductUtility.GetAllInstalledRevitProducts();

            foreach(RevitProduct product in products)
            {
                string versionYear = product.Version.ToString().Substring("Revit".Length);
                if(!versions.Contains<string>(versionYear)) continue;

                InstallItem install = new InstallItem(versionYear);
                mainWindow.AddInstallItem(install);

                string dllPath = WriteProgramFiles(versionYear);

                if(dllPath != null) WriteAddInManifest(dllPath, guid, product);

                //make progress visible
                for(int fakeProgress = 0; fakeProgress < 10; fakeProgress++)
                {
                    double level = (fakeProgress / 10.0);
                    install.Level = level;
                    install.Message = string.Format("Installing for {0}... {1:#%}", product.Name, level);
                    Thread.Sleep(100);
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
                    typeof(App).Namespace + '.' + ViewSyncInstaller.Properties.Resources.Namespace + version + ".dll");
                if(libraryStream == null) return null;

                string deploymentLocation = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\" + 
                    ViewSyncInstaller.Properties.Resources.InstallFolder + "\\" + 
                    ViewSyncInstaller.Properties.Resources.AppName + "\\";
                if(!Directory.Exists(deploymentLocation)) Directory.CreateDirectory(deploymentLocation);
                
                //TODO: verify file is not locked if exists

                dllPath = deploymentLocation + ViewSyncInstaller.Properties.Resources.Namespace + version + ".dll";
                libraryStream.CopyTo(new FileStream(dllPath, FileMode.Create));

            } catch(Exception) {
                return null;
            }
            return dllPath;
        }

        bool WriteAddInManifest(string dllPath, Guid guid, RevitProduct product)
        {
            //create application and/or command entries
            RevitAddInCommand addInComm = new RevitAddInCommand(
                dllPath,
                guid,
                ViewSyncInstaller.Properties.Resources.ClassFullName,
                ViewSyncInstaller.Properties.Resources.VendorName);

            addInComm.VendorDescription = ViewSyncInstaller.Properties.Resources.VendorDescription;
            addInComm.Text = ViewSyncInstaller.Properties.Resources.AppName;
            addInComm.VisibilityMode = VisibilityMode.NotVisibleWhenNoActiveDocument;

            //create manifest and add apps/comms
            RevitAddInManifest manifest = new RevitAddInManifest();
            manifest.AddInCommands.Add(addInComm);

            //save manifest to file
            if (!Directory.Exists(product.CurrentUserAddInFolder)) 
                Directory.CreateDirectory(product.CurrentUserAddInFolder);

            manifest.SaveAs(product.CurrentUserAddInFolder + "\\" + ViewSyncInstaller.Properties.Resources.Namespace + ".addin");

            return true;
        }
    }
}
