using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace ViewSync
{
    public partial class ViewSyncApplication : IExternalApplication
    {
        //application objects

        /// <summary>
        /// Application Startup
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public Result OnStartup(UIControlledApplication application)
        {
            string assemblyName = Assembly.GetExecutingAssembly().Location;

            //create ribbon panel
            RibbonPanel RevitAddInPanel = application.CreateRibbonPanel("RevitAddInApplication");

            //create split button for add and remove commands
            SplitButtonData splitButtonData = new SplitButtonData("Name", "Text");
            SplitButton splitButton = RevitAddInPanel.AddItem(splitButtonData) as SplitButton;

            //add buttons to the split button
            splitButton.AddPushButton(Command1.GetButtonData(assemblyName));
            splitButton.AddPushButton(Command2.GetButtonData(assemblyName));
            
            //document open event
            application.ControlledApplication.DocumentOpened += 
                new EventHandler<Autodesk.Revit.DB.Events.DocumentOpenedEventArgs>(RegisterUpdaterDocumentOpened);

            return Result.Succeeded;
        }
        
        /// <summary>
        /// Application Shutdown
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public Result OnShutdown(UIControlledApplication application)
        {
            //clean up
            application.ControlledApplication.DocumentOpened -= RegisterUpdaterDocumentOpened;
            return Result.Succeeded;
        }


    }
}
