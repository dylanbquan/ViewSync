using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;

namespace ViewSync
{
    public partial class ViewSyncApplication : IExternalApplication
    {
        /// <summary>
        /// Document open handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RegisterUpdaterDocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
        {
            //register updater
        }
    }

    /// <summary>
    /// Updater
    /// </summary>
    class RevitAddInUpdater : IUpdater
    {
        public void Execute(UpdaterData data)
        {
            //do update
        }

        public string GetAdditionalInformation()
        {
            return "Additional Info";
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.Views;
        }

        public UpdaterId GetUpdaterId()
        {
            throw new NotImplementedException();
        }

        public string GetUpdaterName()
        {
            return "Revit Add In Updater";
        }
    }

}
