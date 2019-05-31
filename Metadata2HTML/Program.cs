using System;
using ESRI.ArcGIS;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;

/*
 * Opens a *.lyr file from a console app and displays various properties.
 * The layer file can be modified and then saved.  See
 * https://desktop.arcgis.com/en/arcobjects/latest/net/webframe.htm#ILayerFile.htm
 * for methods on the ILayerFile.
 */

namespace Metadata2HTML
{
    static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            AoInitialize license = GetLicense(ProductCode.Desktop, esriLicenseProductCode.esriLicenseProductCodeAdvanced);
            if (license == null) return;

            try
            {
                // Get Feature Class; Assume it is 

                string workspacePath = @"X:\AKR\Statewide\cultural\AKNetworks.gdb";
                string featureName = "ARCN";
                if (args.Length > 1)
                {
                    workspacePath = args[0];
                    featureName = args[1];
                }

                // Open the Feature Class
                IWorkspaceFactory workspaceFactory = new FileGDBWorkspaceFactory();
                var workSpace = (IDataset) workspaceFactory.OpenFromFile(workspacePath, 0);
                // Or .Open() a new WorkspaceName after setting the ProgID and Path
                var featureClassName = (IDatasetName)new FeatureClassName();
                featureClassName.Name = featureName;
                featureClassName.WorkspaceName = (IWorkspaceName)workSpace.FullName;
                ((IName)featureClassName).Open();
                IMetadata metadata = (IMetadata)featureClassName;
                IXmlPropertySet2 xmlPropertySet2 = (IXmlPropertySet2)metadata.Metadata;
                String xmlDoc = xmlPropertySet2.GetXml("");

                Console.WriteLine(xmlDoc);

                Shutdown(license, "Successful Completion");

            }
            catch (Exception exc)
            {
                Shutdown(license, $"Exception caught while converting metadata to html. {exc.Message}");
            }
        }

        private static AoInitialize GetLicense(ProductCode product, esriLicenseProductCode level)
        {
            AoInitialize aoInit = null;
            try
            {
                Console.WriteLine($"Obtaining {product}-{level} license");
                RuntimeManager.Bind(product);
                aoInit = new AoInitialize();
                esriLicenseStatus licStatus = aoInit.Initialize(level);
                Console.WriteLine($"Ready with license.  Status: {licStatus}");
            }
            catch (Exception exc)
            {
                Shutdown(aoInit, $"Fatal Error: {exc.Message}");
                return null;
            }

            return aoInit;
        }

        private static void Shutdown(AoInitialize license, string msg)
        {
            if (!String.IsNullOrWhiteSpace(msg))
            {
                Console.WriteLine(msg);
                Console.WriteLine("Press any key...");
                Console.ReadKey();
            }

            license.Shutdown();
        }
    }
}