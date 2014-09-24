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
    public partial class ViewSyncInstallerApp : Application
    {
        InstallWindow mainWindow;
        Thread installThread;

        List<string> versionsDLL;
        List<string> versionsVS;
        List<string> versionsVA;

        /// <summary>
        /// Application_Startup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            mainWindow = new InstallWindow(InstallDataVS.CommandName + " Installer", 
                "Installing " + InstallDataVS.CommandName + " ...");
            this.MainWindow = mainWindow;
            mainWindow.Show();
            while (!mainWindow.IsInitialized) ;
            
            installThread = new Thread(new ThreadStart(Install));
            installThread.Start();
        }

        /// <summary>
        /// Install thread
        /// </summary>
        public void Install()
        {
            Thread.Sleep(1000); //initial stall

            versionsVS = new List<string>(InstallDataVS.Versions.Split(',', ' '));
            versionsVA = new List<string>(InstallDataVA.Versions.Split(',', ' '));
            versionsDLL = versionsVS.Union<string>(versionsVA).ToList<string>();
            
            List<RevitProduct> allProducts = RevitProductUtility.GetAllInstalledRevitProducts();
            //TODO: group products by year, write dll once per year group,
            // verify year group uses same manifest location, use one install item per year group

            foreach(string version in versionsDLL) //RevitProduct product in products)
            {
                List<RevitProduct> versionProducts = allProducts.
                    Where<RevitProduct>(p => p.Version.ToString().Contains(version)).
                    ToList<RevitProduct>();

                string installName = "Revit " + version;
                InstallItem install = new InstallItem(installName);
                mainWindow.AddInstallItem(install);

                //make progress visible
                for (int fakeProgress = 0; fakeProgress < 10; fakeProgress++)
                {
                    double progressLevel = (fakeProgress / 10.0);
                    install.Level = progressLevel;
                    install.Message = string.Format("Installing for {0}... {1:#%}", installName, progressLevel);
                    Thread.Sleep(100);
                }

                //write files
                string dllPath = WriteProgramFiles(version);
                if (dllPath == null)
                {
                    //fail installation for this product
                    install.Message = string.Format("Installation for {0} failed.", installName);
                    install.Level = 0.0;
                    continue;
                }

                //write manifests
                bool manifestSuccess = true;
                List<string> manifestPaths = new List<string>();
                foreach(RevitProduct product in versionProducts)
                {
                    string manifestPath = product.CurrentUserAddInFolder;
                    if (manifestPaths.Contains(manifestPath)) continue;

                    bool success = WriteAddInManifest(version, dllPath, manifestPath);
                    if(success) manifestPaths.Add(manifestPath);

                    manifestSuccess &= success;
                }
                if (!manifestSuccess)
                {
                    //fail installation for this product
                    install.Message = string.Format("Installation for {0} failed.", installName);
                    install.Level = 0.0;
                    continue;
                }

                install.Level = 1.0;
                install.Message = string.Format("Installed for {0} {1:#%}", installName, 1.0);
            }

            mainWindow.Message = "Installation Complete!";
            mainWindow.Complete();
        }

        /// <summary>
        /// WriteProgramFiles
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        string WriteProgramFiles(string version)
        {
            string dllPath = null;
            try
            {
                //copy dll to programfiles

                //TODO: get dll stream from resource file not link
                Stream libraryStream = 
                    typeof(ViewSyncInstallerApp).Assembly.GetManifestResourceStream(
                    typeof(ViewSyncInstallerApp).Namespace + '.' + InstallDataVS.Namespace + version + ".dll");
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

        /// <summary>
        /// WriteAddInManifest
        /// </summary>
        /// <param name="dllPath"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        bool WriteAddInManifest(string version, string dllPath, string addInFolder)
        {
            try
            {
                //create manifest and add commands
                RevitAddInManifest manifest = new RevitAddInManifest();

                //View Sync Manual
                if (versionsVS.Contains(version))
                {
                    RevitAddInCommand addInCommVS = new RevitAddInCommand(
                        dllPath,
                        new Guid(InstallDataVS.CommandGuid),
                        InstallDataVS.ClassFullName,
                        InstallDataVS.VendorName);

                    addInCommVS.VendorDescription = InstallDataVS.VendorDescription;
                    addInCommVS.Text = InstallDataVS.CommandName;
                    addInCommVS.VisibilityMode = VisibilityMode.NotVisibleWhenNoActiveDocument;

                    manifest.AddInCommands.Add(addInCommVS);
                }

                //View Sync Auto
                if (versionsVA.Contains(version))
                {
                    RevitAddInCommand addInCommVA = new RevitAddInCommand(
                        dllPath,
                        new Guid(InstallDataVA.CommandGuid),
                        InstallDataVA.ClassFullName,
                        InstallDataVA.VendorName);

                    addInCommVA.VendorDescription = InstallDataVA.VendorDescription;
                    addInCommVA.Text = InstallDataVA.CommandName;
                    addInCommVA.VisibilityMode = VisibilityMode.NotVisibleWhenNoActiveDocument;

                    manifest.AddInCommands.Add(addInCommVA);
                }
                
                //save manifest to file
                if (!Directory.Exists(addInFolder)) Directory.CreateDirectory(addInFolder);
                string manifestFullPath = addInFolder + "\\" + InstallDataVS.Namespace + ".addin";
                if (File.Exists(manifestFullPath)) File.Delete(manifestFullPath); //delete old?
                manifest.SaveAs(manifestFullPath);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Embedded dll
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().
                GetManifestResourceStream("ViewSyncInstaller.RevitAddInUtility.dll"))
            {
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return Assembly.Load(bytes);
            }
        }
    }
}
