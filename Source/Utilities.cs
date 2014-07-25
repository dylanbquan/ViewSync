using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;

using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;

namespace ViewSync
{
    static class ViewSyncUtilities
    {
        /// <summary>
        /// General Availability
        /// </summary>
        /// <param name="applicationData"></param>
        /// <returns></returns>
        public static bool IsAvailableGeneral(UIApplication applicationData)
        {
            if (applicationData.ActiveUIDocument == null) return false;
            if (applicationData.ActiveUIDocument.Document.IsFamilyDocument) return false;

            return true;
        }

        //Other utility methods

        /// <summary>
        /// Create Bitmap image from embedded resource file name
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns></returns>
        public static BitmapImage GetBitmapImageFromResource(string imageName)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource =
                Assembly.GetExecutingAssembly().GetManifestResourceStream(imageName);
            image.EndInit();
            return image;
        }
    }

    /// <summary>
    /// Circuited element selection filter
    /// </summary>
    public class CircuitSelector : ISelectionFilter
    {
        private ElectricalSystem allowedSystem;

        public CircuitSelector()
        {
            allowedSystem = null;
        }
        public CircuitSelector(ElectricalSystem system)
        {
            allowedSystem = system;
        }

        public bool AllowElement(Element elem)
        {
            FamilyInstance inst = elem as FamilyInstance;
            if (inst == null) return false;
            MEPModel model = inst.MEPModel;
            if (model == null) return false;
            if (model.ElectricalSystems.Size == 0) return false;

            //test for particular system values
            if(allowedSystem != null && allowedSystem.SystemType.ToString().Contains("Power"))
            {
                List<ElectricalSystem> systemList = 
                model.ElectricalSystems.Cast<ElectricalSystem>().
                Where<ElectricalSystem>(es => es.Voltage == allowedSystem.Voltage).
                ToList<ElectricalSystem>();

                if(systemList.Count == 0) return false;
            }
            
            return true;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
