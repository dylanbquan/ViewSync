using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace ViewSync
{
    /// <summary>
    /// View Sync Command
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class SyncViews : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            View currentView = uiDoc.ActiveView;
            List<XYZ> corners = new List<XYZ>();

            try {
                if(currentView == null) return Result.Cancelled;
                if (!(currentView is ViewPlan || currentView is ViewSection)) return Result.Cancelled;
                
                UIView uiView = uiDoc.GetOpenUIViews().
                        Where<UIView>(uiv => uiv.ViewId == currentView.Id).First<UIView>();
                corners = uiView.GetZoomCorners().ToList<XYZ>();

                if(currentView is ViewPlan)
                {
                    ViewPlan plan = currentView as ViewPlan;
                    PlanViewRange range = plan.GetViewRange();

                    double bottom = 0;
                    Level bottomlevel = doc.GetElement(range.GetLevelId(PlanViewPlane.BottomClipPlane)) as Level;
                    if(bottomlevel != null) bottom = bottomlevel.Elevation + range.GetOffset(PlanViewPlane.BottomClipPlane);

                    double top = bottom;
                    Level toplevel = doc.GetElement(range.GetLevelId(PlanViewPlane.TopClipPlane)) as Level;
                    if(toplevel != null) top = toplevel.Elevation + range.GetOffset(PlanViewPlane.TopClipPlane);

                    //replace Z values from range
                    corners[0] = new XYZ(corners[0].X, corners[0].Y, bottom);
                    corners[1] = new XYZ(corners[1].X, corners[1].Y, top);
                }
                else if(currentView is ViewSection)
                {
                    BoundingBoxXYZ box = currentView.CropBox;
                    //replace Y values from view range
                    corners[0] = new XYZ(corners[0].X, box.Transform.OfPoint(box.Min).Y, corners[0].Z);
                    corners[1] = new XYZ(corners[1].X, box.Transform.OfPoint(box.Max).Y, corners[1].Z);
                }
                else return Result.Cancelled;

                foreach(UIView uiv in uiDoc.GetOpenUIViews())
                {
                    //exclude current view
                    if(uiv.ViewId == currentView.Id) continue;

                    //exclude minimized views
                    Rectangle rectangle = uiv.GetWindowRectangle();
                    if (rectangle.Top == rectangle.Bottom || rectangle.Left == rectangle.Right) continue;

                    View view = doc.GetElement(uiv.ViewId) as View;

                    if(view is ViewPlan || view is View3D || view is ViewSection)
                    {
#if RVT2013
                        uiDoc.ActiveView = view;
#endif
                        uiv.ZoomAndCenterRectangle(corners[0], corners[1]);
                    }
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
