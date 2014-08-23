using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ViewSync
{
    /// <summary>
    /// View Sync Command
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class SyncViews : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            View currentView = uiDoc.ActiveView;
            ViewBox currentBox = new ViewBox();

            try {

                if (!currentBox.Set(currentView)) return Result.Cancelled;
                ViewBox otherBox = new ViewBox();

                //Sync other graphical views
                foreach(UIView uiv in uiDoc.GetOpenUIViews().Reverse<UIView>()) //reverse mainatains window order in 2013
                {
                    //exclude current view
                    if(uiv.ViewId == currentView.Id) continue;

                    View view = doc.GetElement(uiv.ViewId) as View;

                    if (!ViewBox.CanZoom(view)) continue;
                    if (otherBox.Set(view) && otherBox.IsAlmostEqualTo(currentBox)) continue;

#if RVT2013
                    uiDoc.ActiveView = view;
#endif
                    currentBox.Zoom(view);
                }
#if RVT2013
                uiDoc.ActiveView = currentView;
#endif

            } catch (Exception e) {
                if(e is Autodesk.Revit.Exceptions.OperationCanceledException) return Result.Cancelled;

                message = e.Message;
                return Result.Failed;
            }
            
            return Result.Succeeded;
        }

    }

}
