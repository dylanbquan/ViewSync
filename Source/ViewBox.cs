using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ViewSync
{
    class ViewBox
    {
        private XYZ Center; //midpoint of view box volume
        private double Scalar; //scalable length from center to any face of view box
        private const double MIN_PREC = 1.0e-09;

        /// <summary>
        /// Constructor
        /// </summary>
        public ViewBox()
        {
            Center = new XYZ();
            Scalar = 0.0;
        }
        public ViewBox(View view)
        {
            Center = new XYZ();
            Scalar = 0.0;
            Set(view);
        }

        public bool Set(ViewBox other)
        {
            Center = new XYZ(other.Center.X, other.Center.Y, other.Center.Z);
            Scalar = other.Scalar;
            return true;
        }

        /// <summary>
        /// Comparison
        /// </summary>
        /// <param name="other"></param>
        /// <param name="prec"></param>
        /// <returns></returns>
        public bool IsAlmostEqualTo(ViewBox other, double prec)
        {
            return Math.Abs(Scalar - other.Scalar) <= prec && Center.IsAlmostEqualTo(other.Center, prec);
        }
        public bool IsAlmostEqualTo(ViewBox other)
        {
            return IsAlmostEqualTo(other, MIN_PREC);
        }

        /// <summary>
        /// Set box from view
        /// </summary>
        /// <param name="view"></param>
        public bool Set(View view)
        {
            if (!(view is ViewPlan || view is ViewSection)) return false;

            Document doc = view.Document;
            UIDocument uiDoc = new UIDocument(doc);
            UIView uiView = uiDoc.GetOpenUIViews().
                        Where<UIView>(uiv => uiv.ViewId == view.Id).First<UIView>();

            Rectangle uiRect = uiView.GetWindowRectangle();
            int uiRectMin = Math.Min(uiRect.Right - uiRect.Left, uiRect.Bottom - uiRect.Top);
            if (uiRectMin == 0) return false; //minimized

            List<XYZ> corners = (List<XYZ>)uiView.GetZoomCorners();

            //center of viewing plane
            Center = corners[0].Add(corners[1]).Divide(2.0);
            
            //minimum rectilinear distance
            BoundingBoxXYZ box = view.CropBox;
            Transform inverse = box.Transform.Inverse;
            XYZ min = inverse.OfPoint(corners[0]);
            XYZ max = inverse.OfPoint(corners[1]);
            
            //half of shortest view dimension divided by the shortest window dimension
            Scalar = (Math.Min(max.X - min.X, max.Y - min.Y)) / (double)uiRectMin / 2.0;

            //TODO: center should be shifted to either half view depth or prescalar distance (before dividing window size), whichever is least
            if (view is ViewPlan)
            {
                ViewPlan plan = view as ViewPlan;
                PlanViewRange range = plan.GetViewRange();

                double bottom = 0;
                Level bottomlevel = doc.GetElement(range.GetLevelId(PlanViewPlane.BottomClipPlane)) as Level;
                if (bottomlevel != null) bottom = bottomlevel.Elevation + range.GetOffset(PlanViewPlane.BottomClipPlane);

                double top = bottom;
                Level toplevel = doc.GetElement(range.GetLevelId(PlanViewPlane.TopClipPlane)) as Level;
                if (toplevel != null) top = toplevel.Elevation + range.GetOffset(PlanViewPlane.TopClipPlane);

                //offset center into view range, positive Z?
            }
            else if (view is ViewSection)
            {
                ViewSection section = view as ViewSection; //nothing unique here
                //check for far clip off
                //offset center into section view depth, -basis Z?
            }

            return true;
        }

        /// <summary>
        /// Zoom view to this box
        /// </summary>
        /// <param name="view"></param>
        public bool Zoom(View view)
        {
            if (Scalar <= double.MinValue) return false; //box is not initialized

            if (!(view is ViewPlan || view is View3D || view is ViewSection)) return false;

            UIDocument uiDoc = new UIDocument(view.Document);
            UIView uiView = uiDoc.GetOpenUIViews().
                        Where<UIView>(uiv => uiv.ViewId == view.Id).First<UIView>();
            if (uiView == null) return false; //view is not open

            Transform transform = new Transform(view.CropBox.Transform);
            transform.Origin = Center;

            //scale edge
            Rectangle uiRect = uiView.GetWindowRectangle();
            int uiRectMin = Math.Min(uiRect.Right - uiRect.Left, uiRect.Bottom - uiRect.Top);
            if (uiRectMin == 0) return false; //window is minimized
            double offset = (Scalar * uiRectMin);

            //calculate corners
            List<XYZ> corners = new List<XYZ>();
            corners.Add(transform.OfPoint( new XYZ(-offset, -offset, -offset) ));
            corners.Add(transform.OfPoint( new XYZ(offset, offset, offset)));

            uiView.ZoomAndCenterRectangle(corners[0], corners[1]);

            return true;
        }
    }

}
