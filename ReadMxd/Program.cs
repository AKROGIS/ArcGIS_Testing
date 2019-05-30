using ESRI.ArcGIS;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using System;
using System.Runtime.InteropServices;

/*
 * Opens a *.mxd file from a console app and displays various properties.
 * The map document can be modified and then saved.  See
 * https://desktop.arcgis.com/en/arcobjects/latest/net/webframe.htm#IMapDocument.htm
 * for methods on the IMapDocument.
 */

namespace ReadMxd
{
    static class Program
    {
        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        private static extern int GetDesktopWindow();

        [STAThread]
        private static void Main(string[] args)
        {
            AoInitialize license = GetLicense(ProductCode.Desktop, esriLicenseProductCode.esriLicenseProductCodeAdvanced);
            if (license == null) return;

            try
            {
                // Get MXD

                string mxdPath = @"C:\tmp\pds\mapFixer\test_data\mapFixer_testing_function.mxd";
                if (args.Length > 0)
                {
                    mxdPath = args[0];
                }

                // Open the MXD

                IMapDocument mapDoc = new MapDocument();
                if (mapDoc.IsPresent[mxdPath] && mapDoc.IsMapDocument[mxdPath] && !mapDoc.IsPasswordProtected[mxdPath])
                {
                    mapDoc.Open(mxdPath);
                    try
                    {
                        /*
                         * From the open() method docs:
                         * When opening or creating a map document with the IMapDocument Open() or New() methods, you should always
                         * make subsequent calls to IActiveView::Activate() in order to properly initialize the display of the PageLayout
                         * and Map objects.  Call Activate() once for the PageLayout and once for each Map you will be working with.
                         * If your application has a user interface, you should call Activate() with the hWnd of the application's client area.
                         * If your application runs in the background and has no windows, you can always get a valid hWnd from the GDI
                         * GetDesktopWindow() function, part of the Win32 API.
                         */

                        ((IActiveView)mapDoc.PageLayout).Activate(GetDesktopWindow());

                        // Read the Map Documents
                        Console.WriteLine($"Contents of Document: {mxdPath}");
                        for (int i = 0; i < mapDoc.MapCount; i++)
                        {
                            var map = mapDoc.Map[i];
                            //((IActiveView)map).Activate(GetDesktopWindow());
                            Console.WriteLine($"  Data Frame #{i}: {map.Name}");
                            for (int j = 0; j < map.LayerCount; j++)
                            {
                                var layer = map.Layer[j];
                                Console.WriteLine($"    Layer #{j}: {layer.Name}");
                            }
                        }
                    }
                    finally
                    {
                        mapDoc.Close();
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid MXD Path: {mxdPath}");
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