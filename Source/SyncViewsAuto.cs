using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace ViewSync
{
    /// <summary>
    /// Active View Sync Command
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    class SyncViewsAuto : IExternalCommand
    {
        private static UIApplication application;
        private static bool syncAuto = false;
        private static ViewBox prevBox;
        private static List<ElementId> syncedViewIds;
        private static EventHandler<IdlingEventArgs> syncHandler;
        private static int viewIndex;

        /// <summary>
        /// continuous sync
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (application == null) application = commandData.Application;
            if (syncHandler == null) syncHandler = new EventHandler<IdlingEventArgs>(SyncOneView);

            syncAuto = !syncAuto;
            if (syncAuto)
            {
                commandData.Application.Idling += syncHandler;
                prevBox = new ViewBox();
                syncedViewIds = new List<ElementId>();
                viewIndex = 0;
            }
            else commandData.Application.Idling -= syncHandler;

            return Result.Succeeded;
        }

        /// <summary>
        /// Sync one view per idle event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SyncOneView(object sender, IdlingEventArgs e)
        {
            ViewBox viewBox = new ViewBox();
            UIDocument activeUiDoc = application.ActiveUIDocument;

            View activeView = activeUiDoc.ActiveView;
            if (!viewBox.Set(activeView)) return;

            //CATCH CHANGE
            if (!viewBox.IsAlmostEqualTo(prevBox))
            {
                syncedViewIds.Clear();
                syncedViewIds.Add(activeView.Id);
                prevBox.Set(viewBox);
            }

            //UPDATE VIEWS
            List<ElementId> uiViews = activeUiDoc.GetOpenUIViews().Select <UIView, ElementId>(uiv => uiv.ViewId).ToList<ElementId>();

            ElementId uiViewNext; //iterator
            while (syncedViewIds.Count < uiViews.Count)
            {
                viewIndex %= uiViews.Count;
                uiViewNext = uiViews[viewIndex++];

                if(syncedViewIds.Contains(uiViewNext)) continue;
                syncedViewIds.Add(uiViewNext);

                if(prevBox.Zoom(activeUiDoc.Document.GetElement(uiViewNext) as View)) break;
            }

        }

    }

}
