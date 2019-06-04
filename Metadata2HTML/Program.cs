using ESRI.ArcGIS;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;

/*
 * Opens a Feature Class in a FGDB and reads the metadata
 * then transforms it using the Esri Stylesheets to an html file
 */

namespace Metadata2HTML
{
    static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            AoInitialize license = GetLicense(ProductCode.Desktop, esriLicenseProductCode.esriLicenseProductCodeAdvanced);
            if (license == null) return;

            sw.Stop();
            Console.WriteLine("Time to get license = {0}", sw.Elapsed);

            try
            {
                // Get Feature Class

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
                //var xlstFilePath = @"C:\Program Files (x86)\ArcGIS\Desktop10.5\Metadata\Stylesheets\ArcGIS_Imports\XML.xslt";
                //var xlstFilePath = @"C:\Program Files (x86)\ArcGIS\Desktop10.5\Metadata\Stylesheets\ArcGIS_Imports\FGDC.xslt";  //No good - only works as an included stylesheet

                // Load the stylesheet
                // Loading also compiles the stylesheet, so there is a large startup cost
                // If possible, load a stylesheet once if it may be used for multiple transformations.
                // See https://docs.microsoft.com/en-us/dotnet/standard/data/xml/inputs-to-the-xslcompiledtransform-class

                // Debugging:
                // ** Debugging really slows down the compilation and transformation process, so only use it in testing.**
                // If you wish to debug the compilation of the XLST file, you need to add true as the first/only
                // parameter to the XslCompiledTransform constructor.
                // You will also need to add the following to the app.config to avoid 'Stylesheet is too complex warnings'
                /*
                 <configSections>
                   <sectionGroup name="system.xml">
                     <section name="xslt" type="System.Xml.XmlConfiguration.XsltConfigSection, System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" requirePermission="false"/>
                   </sectionGroup>
                 </configSections>
                 <system.xml>
                   <xslt limitXPathComplexity="false"/>
                 </system.xml>  
                 */
                sw.Restart();
                Console.WriteLine("Loading/compiling the XLST code");
                var transform = new XslCompiledTransform();
                transform.Load(xlstFilePath, null, new XmlUrlResolver());
                sw.Stop();
                Console.WriteLine("Loaded/compiled in {0}", sw.Elapsed);

                // Custom Esri extensions are needed to process the Esri Stylesheets
                // This is based on examination of the Stylesheets and the Esri Libraries
                // I could not find this documented by Esri, so it may be subject to change without notice
                XsltArgumentList xslArg = new XsltArgumentList();
                var esri = new ESRI.ArcGIS.Metadata.Editor.XsltExtensionFunctions();
                xslArg.AddExtensionObject("http://www.esri.com/metadata/", esri);
                xslArg.AddExtensionObject("http://www.esri.com/metadata/res/", esri);

                // HTML output
                Console.WriteLine("Transforming the XML to html");
                using (TextWriter writer = new Utf8StringWriter())
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(writer))
                    {
                        sw.Restart();
                        transform.Transform(xmlReader, xslArg, xmlWriter);
                        sw.Stop();
                        Console.WriteLine("Transformed in {0}", sw.Elapsed);
                    }
                    //File.WriteAllText(@"C:\tmp\metadata_in.html", writer.ToString());

                    // Replace the localizable elements <res:xxx /> with the localized text
                    var pattern = @"<res:(\w+)(?:\s?|\s\S*\s)/>";
                    //var pattern = @"<res:(\w+)\s?/>";
                    MatchEvaluator evaluator = EsriLocalize;
                    sw.Restart();
                    var htmlText = Regex.Replace(writer.ToString(), pattern, evaluator, RegexOptions.None, TimeSpan.FromSeconds(0.25));
                    sw.Stop();
                    Console.WriteLine("Localized HTML in {0}", sw.Elapsed);
                    //Add DOCTYPE
                    htmlText = htmlText.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", 
                        "<?xml version=\"1.0\" encoding=\"utf-8\"?><!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
                    //Fix Thumbnail centering
                    htmlText = htmlText.Replace(".noThumbnail {",
                        ".noThumbnail {display:inline-block;");
                    File.WriteAllText(@"C:\tmp\metadata.html", htmlText);
                }

                Shutdown(license, "Successful Completion");

            }
            catch (Exception exc)
            {
                Shutdown(license, $"Exception caught while converting metadata to html. {exc.Message}");
            }
        }

        private static string EsriLocalize(Match match)
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

    // The default string writer is encodes UTF16 which is not compatible with my UTF8 XML
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}