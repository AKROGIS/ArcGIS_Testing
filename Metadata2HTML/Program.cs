using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ESRI.ArcGIS;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesGDB;
using System.Xml;
using System.Xml.Xsl;

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
                // Get Feature Class

                // X:\AKR\Statewide\cultural\AKNetworks.gdb\ARCN                  // internal ArcGIS format
                // X:\AKR\Statewide\cultural\adminbnd.gdb\AdministrativeBoundary  // internal FGDC format

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

                // Console.WriteLine(xmlDoc);

                // XML input
                XmlReader xmlReader = XmlReader.Create(new StringReader(xmlDoc));


                // Style Sheet
                var xlstFilePath = @"C:\Program Files (x86)\ArcGIS\Desktop10.5\Metadata\Stylesheets\ArcGIS_ItemDescription.xsl";
                //var xlstFilePath = @"C:\Program Files (x86)\ArcGIS\Desktop10.5\Metadata\Stylesheets\ArcGIS.xsl";

                //XmlReader xlstReader = XmlReader.Create(xlstFilePath, null, new XmlParserContext(baseURI:@"C:\Program Files (x86)\ArcGIS\Desktop10.5\Metadata\Stylesheets"));

                // XLST Transform - see https://docs.microsoft.com/en-us/dotnet/standard/data/xml/inputs-to-the-xslcompiledtransform-class
                // parameter is for debugging
                XslCompiledTransform transform = new XslCompiledTransform(true);
                //XsltSettings settings = new XsltSettings();
                //settings.EnableScript = true;
                var myResolver = new MyXmlUrlResolver();
                XsltArgumentList xslArg = new XsltArgumentList();
                //Console.WriteLine($"idTags = {ESRI.ArcGIS.Metadata.Editor.XsltExtensionFunctions.GetResString("idTags")}");
                var esri = new ESRI.ArcGIS.Metadata.Editor.XsltExtensionFunctions();
                xslArg.AddExtensionObject("http://www.esri.com/metadata/", esri);
                //xslArg.AddExtensionObject("http://www.esri.com/metadata/res", esri);
                Console.WriteLine("Loading/compiling the XLST code");
                //transform.Load(xlstFilePath, XsltSettings.TrustedXslt, myResolver);
                //transform.Load(xlstFilePath, settings, myResolver);
                transform.Load(xlstFilePath, null, myResolver);
                Console.WriteLine("Transforming the XML to html");
                // HTML output
                using (TextWriter writer = new Utf8StringWriter())
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(writer))
                    {
                        transform.Transform(xmlReader, xslArg, xmlWriter);
                    }
                    // Replace the localizable elements <res:xxx /> with the localized text
                    var pattern = @"<res:(\w+)\s*/>";
                    MatchEvaluator evaluator = EsriLocalizer;
                    var htmlText = Regex.Replace(writer.ToString(), pattern, evaluator);
                    System.IO.File.WriteAllText(@"C:\tmp\metadata.html", htmlText);
                }

                Shutdown(license, "Successful Completion");

            }
            catch (Exception exc)
            {
                Shutdown(license, $"Exception caught while converting metadata to html. {exc.Message}");
            }
        }

        private static string EsriLocalizer(Match match)
        {
            return ESRI.ArcGIS.Metadata.Editor.XsltExtensionFunctions.GetResString(match.Groups[1].Value);
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

    class MyXmlUrlResolver : XmlUrlResolver
    {
        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            if (baseUri != null)
                return base.ResolveUri(baseUri, relativeUri);
            else
                return base.ResolveUri(new Uri(@"file://C:\Program Files(x86)\ArcGIS\Desktop10.5\Metadata\Stylesheets"), relativeUri);
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}