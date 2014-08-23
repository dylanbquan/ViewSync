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

        /// <summary>
        /// Check
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public static bool CanSet(View view)
        {
            if (!(view is ViewPlan || view is ViewSection)) return false;

            UIView uiView = GetUIView(view);
            if (uiView == null) return false; //view is not open

            int uiRectMin = GetRectMin(uiView);
            if (uiRectMin == 0) return false; //view is minimized

            return true;
        }

        /// <summary>
        /// Check for 
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public static bool CanZoom(View view)
        {
            if (!(view is ViewPlan || view is View3D || view is ViewSection)) return false;

            UIDocument uiDoc = new UIDocument(view.Document);
            UIView uiView = GetUIView(view);
            if (uiView == null) return false; //view is not open

            int uiRectMin = GetRectMin(uiView);
            if (uiRectMin == 0) return false; //view is minimized

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
            UIView uiView = GetUIView(view);
            if (uiView == null) return false; //view is not open

            int uiRectMin = GetRectMin(uiView);
            if (uiRectMin == 0) return false; //minimized

            List<XYZ> corners = (List<XYZ>)uiView.GetZoomCorners();

            //minimum rectilinear distance
            BoundingBoxXYZ box = view.CropBox;
            Transform transform = box.Transform;
            Transform inverse = transform.Inverse;
            XYZ min = inverse.OfPoint(corners[0]);
            XYZ max = inverse.OfPoint(corners[1]);
            
            //half of shortest view dimension divided by the shortest window dimension
            Double preScalar = Math.Min(max.X - min.X, max.Y - min.Y) / 2.0;
            Scalar = preScalar / (double)uiRectMin;

            //center of view plane
            Center = min.Add(max).Divide(2.0);
            XYZ offset = new XYZ();
            
            //type specific Z offset
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

                //offset center into view range
                offset = new XYZ(0.0, 0.0, (top - bottom) / 2.0);
            }

            else if (view is ViewSection)
            {
                double clipOffset = 0.0;

                //check for far clip off
                Parameter farClipping = view.get_Parameter(BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING);
                if (farClipping.AsInteger() != 0)
                {
                    Parameter farClipOffset = view.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
                    clipOffset = farClipOffset.AsDouble() / 2.0;

                    clipOffset = Math.Min(clipOffset, preScalar);
                }

                //offset center into section view depth, -basis Z?
                offset = new XYZ(0.0, 0.0, -clipOffset);
            }

            Center = transform.OfPoint(Center.Add(offset));

            return true;
        }
        
        /// <summary>
        /// Override
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Set(ViewBox other)
        {
            Center = new XYZ(other.Center.X, other.Center.Y, other.Center.Z);
            Scalar = other.Scalar;
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
            UIView uiView = GetUIView(view);
            if (uiView == null) return false; //view is not open

            //scale edge
            int uiRectMin = GetRectMin(uiView);
            if (uiRectMin == 0) return false; //window is minimized
            double offset = (Scalar * uiRectMin);

            Transform transform = new Transform(view.CropBox.Transform);
            transform.Origin = Center;

            //calculate corners
            List<XYZ> corners = new List<XYZ>();
            corners.Add(transform.OfPoint( new XYZ(-offset, -offset, -offset) ));
            corners.Add(transform.OfPoint( new XYZ(offset, offset, offset) ));

            uiView.ZoomAndCenterRectangle(corners[0], corners[1]);

            return true;
        }

        /// <summary>
        /// return the UIView of this View
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        private static UIView GetUIView(View view)
        {
            UIDocument uiDoc = new UIDocument(view.Document);
            return uiDoc.GetOpenUIViews().
                Where<UIView>(uiv => uiv.ViewId == view.Id).First<UIView>();
        }

        /// <summary>
        /// Return the minimum window Rectangle dimension of this UIView
        /// </summary>
        /// <param name="uiView"></param>
        /// <returns></returns>
        private static int GetRectMin(UIView uiView)
        {
            Rectangle uiRect = uiView.GetWindowRectangle();
            return Math.Min(uiRect.Right - uiRect.Left, uiRect.Bottom - uiRect.Top);
        }
    }

}
