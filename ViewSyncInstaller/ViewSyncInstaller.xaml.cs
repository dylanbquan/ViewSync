﻿using System;
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
            Thread.Sleep(500); //initial stall

            versionsVS = new List<string>(InstallDataVS.Versions.Split(',', ' '));
            versionsVA = new List<string>(InstallDataVA.Versions.Split(',', ' '));
            versionsDLL = versionsVS.Union<string>(versionsVA).ToList<string>();
            
            List<RevitProduct> allProducts = RevitProductUtility.GetAllInstalledRevitProducts();
            
            foreach(string version in versionsDLL)
            {
                List<RevitProduct> versionProducts = allProducts.
                    Where<RevitProduct>(p => p.Version.ToString().Contains(version)).
                    ToList<RevitProduct>();

                if(versionProducts.Count < 1) continue;

                string installName = "Revit " + version;
                InstallItem install = new InstallItem(installName);
                mainWindow.AddInstallItem(install);
                //pause here before setting success to true
                install.Message = string.Format("Installing for {0}...", installName);
                install.Success = null;

                Thread.Sleep(750);

                install.Success = true;

                bool success = true;

                //write files
                string dllPath = WriteProgramFiles(version);
                if (dllPath == null) success = false;

                //write manifests
                if (success) {
                    List<string> manifestPaths = new List<string>();
                    foreach (RevitProduct product in versionProducts) {
                        string manifestPath = product.CurrentUserAddInFolder;
                        if (manifestPaths.Contains(manifestPath)) continue;

                        bool manifestSuccess = WriteAddInManifest(version, dllPath, manifestPath);
                        if (manifestSuccess) manifestPaths.Add(manifestPath);

                        success &= manifestSuccess;
                    }
                }

                Thread.Sleep(750);
                
                if (!success) {
                    //fail installation for this product (let's try to centralize this fail so we can reverse progress animation)
                    install.Message = string.Format("Installation for {0} failed.", installName);
                    install.Success = false;
                    Thread.Sleep(500);
                    continue;
                }

                //install.Level = 1.0;
                install.Message = string.Format("Installed for {0} {1:#%}", installName, 1.0);
            }

            LogUserInstall();

            Thread.Sleep(500);
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

        string logFileName = "install.log";
        void LogUserInstall()
        {
            List<string> line = new List<string>();
            try {
                if (!File.Exists(logFileName)) {
                    line.Add("user name");
                    line.Add("local time");
                    line.Add("universal time");

                    File.AppendAllText(
                    logFileName,
                    string.Join(",", line)
                    );

                    line.Clear();
                }

                line.Add(Environment.UserName);
                line.Add(DateTime.Now.ToString());
                line.Add(DateTime.Now.ToUniversalTime().ToString());

                File.AppendAllText(
                    logFileName,
                    Environment.NewLine + string.Join(",", line)
                    );
            } catch (Exception) { }
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
