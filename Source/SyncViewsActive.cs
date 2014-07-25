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
    /// Active View Sync Command
    /// Not available in 2013
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    class SyncViewsActive : IExternalCommand
    {
        static private bool active = false;

        static private UIApplication uiApp;
        static private List<XYZ> corners, prevCorners;
        static private List<ElementId> syncedViewIds;
        static public EventHandler<Autodesk.Revit.UI.Events.IdlingEventArgs> syncHandler;

        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            if(uiApp == null) uiApp = commandData.Application;
            if(syncHandler == null) syncHandler = new EventHandler<Autodesk.Revit.UI.Events.IdlingEventArgs>( SyncOneView );

            active = !active;
            if(active) {
                uiApp.Idling += syncHandler;
                prevCorners = new List<XYZ>();
                prevCorners.Add(new XYZ());
                prevCorners.Add(new XYZ());
                syncedViewIds = new List<ElementId>();
            } else uiApp.Idling -= syncHandler;

            return Result.Succeeded;
        }

        void SyncOneView(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            View activeView = uiDoc.ActiveView;
            if(!(activeView is ViewPlan || activeView is ViewSection)) return;

            UIView uiView = uiDoc.GetOpenUIViews().
                        Where<UIView>(uiv => uiv.ViewId == activeView.Id).First<UIView>();
            List<XYZ> activeCorners = uiView.GetZoomCorners().ToList<XYZ>();

            if(!prevCorners[0].IsAlmostEqualTo(activeCorners[0]) ||
               !prevCorners[1].IsAlmostEqualTo(activeCorners[1]))
            {
                syncedViewIds.Clear();
                syncedViewIds.Add(activeView.Id);
                prevCorners = new List<XYZ>(activeCorners);
                corners = new List<XYZ>(prevCorners);

                if(activeView is ViewPlan)
                {
                    Document doc = uiDoc.Document;
                    ViewPlan plan = activeView as ViewPlan;
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
                } else if(activeView is ViewSection)
                {
                    BoundingBoxXYZ box = activeView.CropBox;
                    //replace Y values from view range
                    corners[0] = new XYZ(corners[0].X, box.Transform.OfPoint(box.Min).Y, corners[0].Z);
                    corners[1] = new XYZ(corners[1].X, box.Transform.OfPoint(box.Max).Y, corners[1].Z);
                }
            }

            foreach(UIView uiViewNext in uiDoc.GetOpenUIViews())
            {
                if(syncedViewIds.Contains(uiViewNext.ViewId)) continue;
                syncedViewIds.Add(uiViewNext.ViewId);

                View view = uiDoc.Document.GetElement(uiViewNext.ViewId) as View;
                if(view is ViewPlan || view is View3D || view is ViewSection)
                {
                    uiViewNext.ZoomAndCenterRectangle(corners[0], corners[1]);
                    break; //do one and return
                }

            } 

        }

    }

}
