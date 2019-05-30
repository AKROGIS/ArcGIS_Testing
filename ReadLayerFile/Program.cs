using ESRI.ArcGIS;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using System;

/*
 * Opens a *.lyr file from a console app and displays various properties.
 * The layer file can be modified and then saved.  See
 * https://desktop.arcgis.com/en/arcobjects/latest/net/webframe.htm#ILayerFile.htm
 * for methods on the ILayerFile.
 */

namespace ReadLayerFile
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
                // Get Layer File

                string lyrPath = @"X:\GIS\ThemeMgr\GLBA Themes\Basemap\GLBA Annotation 150K.lyr";
                if (args.Length > 0)
                {
                    lyrPath = args[0];
                }

                // Open the MXD

                ILayerFile layerFile = new LayerFile();
                if (layerFile.IsPresent[lyrPath] && layerFile.IsLayerFile[lyrPath])
                {
                    layerFile.Open(lyrPath);
                    try
                    {
                        // Read the Layer Contents
                        Console.WriteLine($"Contents of Document: {lyrPath}");
                        var layer = layerFile.Layer;
                        Console.WriteLine($"  Name: {layer.Name}");
                        Console.WriteLine($"  Is Valid: {layer.Valid}");
                        Console.WriteLine($"  Is Composite: {layer is ICompositeLayer}");
                    }
                    finally
                    {
                        layerFile.Close();
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid Layer File Path: {lyrPath}");
                }

                Shutdown(license, "Successful Completion");

            }
            catch (Exception exc)
            {
                Shutdown(license, $"Exception caught while reading MXD. {exc.Message}");
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