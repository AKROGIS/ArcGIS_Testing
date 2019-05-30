/*
   Copyright 2016 Esri
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
       http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using ESRI.ArcGIS;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using System;
using System.Collections.Generic;

// ReSharper disable SuspiciousTypeConversion.Global

/*
    This sample opens a Mosaic Dataset and goes through each row in the attribute table.
    It checks whether the dataset in the row has any band information (band properties) 
    associated with it. 
    If the item has no band information, it inserts band properties for the first 3 bands 
    in the item (if the item has 3 or more bands).
    Finally the mosaic dataset product definition is changed to Natural Color RGB so that 
    ArcGIS can use the band names of the mosaic dataset.
    The sample also shows how to set a key property on the mosaic dataset.
    The sample has functions to get/set any key property for a dataset.
    The sample has functions to get/set any band property for a dataset.
    The sample has functions to get all the properties and all the band properties 
    for a dataset.
    Key Properties:
    Key Properties of type 'double':
    CloudCover
    SunElevation
    SunAzimuth
    SensorElevation
    SensorAzimuth
    OffNadir
    VerticalAccuracy
    HorizontalAccuracy
    LowCellSize
    HighCellSize
    MinCellSize
    MaxCellSize
    Key Properties of type 'date':
    AcquisitionDate
    Key Properties of type 'string':
    SensorName
    ParentRasterType
    DataType
    ProductName
    DatasetTag
*/

namespace GetMosaicProperties
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
                // Get name of Mosaic Dataset

                //string workspaceFolder = @"X:\Mosaics\Statewide\DEMs\Best_Available_Elevation.gdb";
                //string name = @"DTM";
                string workspaceFolder = @"X:\Mosaics\Statewide\Imagery\LANDSAT8_2014.gdb";
                string name = @"PanSharp2014";
                if (args.Length > 1)
                {
                    workspaceFolder = args[0];
                    name = args[1];
                }

                // Open the Mosaic Dataset

                Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
                IWorkspaceFactory mdWorkspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
                IWorkspace mdWorkspace = mdWorkspaceFactory.OpenFromFile(workspaceFolder, 0);
                IRasterWorkspaceEx workspaceEx = (IRasterWorkspaceEx)(mdWorkspace);
                IMosaicDataset mosaicDataset = (IMosaicDataset)workspaceEx.OpenRasterDataset(name);

                // Read the Mosaic Dataset

                Console.WriteLine($"All Properties for Raster Mosaic Dataset {workspaceFolder}/{name}");
                Dictionary<string, object> ap = GetAllProperties((IDataset)mosaicDataset);
                if (ap != null)
                {
                    foreach (var kvp in ap)
                    {
                        Console.WriteLine($"  {kvp.Key} => {kvp.Value}");
                        if (kvp.Value is IRasterInfo)
                        {

                        }
                    }
                }

                int band = 0;
                Console.WriteLine($"All Properties for Band {band}");
                Dictionary<string, object> bp = GetAllBandProperties((IDataset)mosaicDataset, band);
                if (bp != null)
                {
                    foreach (var kvp in bp)
                    {
                        Console.WriteLine($"  {kvp.Key} => {kvp.Value}");
                    }
                }

                Console.WriteLine("Getting individual properties directly");
                string key = "DataType";
                object prop = GetRasterKeyProperty((IDataset)mosaicDataset, key);
                Console.WriteLine($"  {key} => {prop}");
                string bandKey = "BandName";
                prop = GetRasterBandProperty((IDataset)mosaicDataset, band, bandKey);
                Console.WriteLine($"  Band {band}: {bandKey} => {prop}");

                // Update the Mosaic Dataset
                /*
                // Set Mosaic Dataset item information.
                SetMosaicDatasetItemInformation(mosaicDataset);
                // Set Key Property 'DataType' on the Mosaic Dataset to value 'Processed'
                // The change will be reflected on the 'General' page of the mosaic dataset
                // properties under the 'Source Type' property.
                SetKeyProperty((IDataset)mosaicDataset, "DataType", "Processed");

                // Set the Product Definition on the Mosaic Dataset to 'NATURAL_COLOR_RGB'
                // First set the 'BandDefinitionKeyword' key property to natural color RGB.
                SetKeyProperty((IDataset)mosaicDataset, "BandDefinitionKeyword", "NATURAL_COLOR_RGB");
                // Then set band names and wavelengths on the mosaic dataset.
                SetBandProperties((IDataset)mosaicDataset);
                // Last and most important, refresh the mosaic dataset so the changes are saved.
                ((IRasterDataset3)mosaicDataset).Refresh();
                */

                Shutdown(license, "Successful Completion");

            }
            catch (Exception exc)
            {
                Shutdown(license, $"Exception caught while reading raster mosaic dataset. {exc.Message}");
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

        /// <summary>
        /// Get all the properties associated with the dataset.
        /// </summary>
        /// <returns>A dictionary of string keys and object properties</returns>
        /// <param name="dataset">An IDataset to get the property from. Must implement IRasterKeyProperties.</param>
        private static Dictionary<string, object> GetAllProperties(IDataset dataset)
        {
            IRasterKeyProperties rasterKeyProps = dataset as IRasterKeyProperties;
            if (rasterKeyProps == null) return null;
            IStringArray allKeys;
            IVariantArray allProperties;
            rasterKeyProps.GetAllProperties(out allKeys, out allProperties);
            var result = new Dictionary<string, object>();
            for (int i = 0; i < allKeys.Count; i++)
            {
                result[allKeys.Element[i]] = allProperties.Element[i];
            }
            return result;
        }

        /// <summary>
        /// Get all the properties associated with a particular band of the dataset.
        /// </summary>
        /// <returns>A dictionary of string keys and object properties</returns>
        /// <param name="dataset">An IDataset to get the properties from. Must implement IRasterKeyProperties.</param>
        /// <param name="bandIndex">band index for which to get all properties. Zero based index.</param>
        private static Dictionary<string, object> GetAllBandProperties(IDataset dataset, int bandIndex)
        {
            IRasterKeyProperties rasterKeyProps = dataset as IRasterKeyProperties;
            if (rasterKeyProps == null) return null;
            IStringArray bandKeys;
            IVariantArray bandProperties;
            rasterKeyProps.GetAllBandProperties(bandIndex, out bandKeys, out bandProperties);
            var result = new Dictionary<string, object>();
            for (int i = 0; i < bandKeys.Count; i++)
            {
                result[bandKeys.Element[i]] = bandProperties.Element[i];
            }
            return result;
        }

        /// <summary>
        /// Get the Key Property corresponding to the key 'key' from the raster dataset.
        /// </summary>
        /// <param name="dataset">an IDataset to get the property from. Must implement IRasterKeyProperties.</param>
        /// <param name="key">The key for which to get the value.</param>
        /// <returns>Property corresponding to the key or null if it doesn't exist or other errors occur.</returns>
        private static object GetRasterKeyProperty(IDataset dataset, string key)
        {
            try
            {
                return ((IRasterKeyProperties)dataset).GetProperty(key);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get the Key Property corresponding to the bandIndex and 'key' from the raster dataset.
        /// </summary>
        /// <param name="dataset">an IDataset to get the property from. Must implement IRasterKeyProperties.</param>
        /// <param name="key">The key for which to get the value.</param>
        /// <param name="bandIndex">Band for which to get the property. Zero based index.</param>
        /// <returns>Property corresponding to the key or null if none found or other errors occur.</returns>
        private static object GetRasterBandProperty(IDataset dataset, int bandIndex, string key)
        {
            try
            {
                return ((IRasterKeyProperties)dataset).GetBandProperty(key, bandIndex);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Sets band properties on a given dataset
        /// </summary>
        /// <param name="dataset">The target IDataset</param>
        private static void SetBandProperties(IDataset dataset)
        {
            try
            {
                // Set properties for Band 1.
                SetBandProperty(dataset, "BandName", 0, "Red");
                SetBandProperty(dataset, "WavelengthMin", 0, 630);
                SetBandProperty(dataset, "WavelengthMax", 0, 690);

                // Set properties for Band 2.
                SetBandProperty(dataset, "BandName", 1, "Green");
                SetBandProperty(dataset, "WavelengthMin", 1, 530);
                SetBandProperty(dataset, "WavelengthMax", 1, 570);

                // Set properties for Band 3.
                SetBandProperty(dataset, "BandName", 2, "Blue");
                SetBandProperty(dataset, "WavelengthMin", 2, 440);
                SetBandProperty(dataset, "WavelengthMax", 2, 480);
            }
            catch (Exception)
            {
                // ignore any errors
            }
        }

        /// <summary>
        /// Set the KeyProperty corresponding to the bandIndex and 'key' from the dataset.
        /// </summary>
        /// <param name="dataset">Dataset to set the property on.</param>
        /// <param name="key">The key on which to set the property.</param>
        /// <param name="bandIndex">Band from which to get the property.</param>
        /// <param name="value">The value to set.</param>
        private static void SetBandProperty(IDataset dataset, string key, int bandIndex, object value)
        {
            IRasterKeyProperties rasterKeyProps = (IRasterKeyProperties)dataset;
            rasterKeyProps.SetBandProperty(key, bandIndex, value);
        }

        /// <summary>
        /// Set the Key Property 'value' corresponding to the key 'key' on the dataset.
        /// </summary>
        /// <param name="dataset">Dataset to set the property on.</param>
        /// <param name="key">The key on which to set the property.</param>
        /// <param name="value">The value to set.</param>
        private static void SetKeyProperty(IDataset dataset, string key, object value)
        {
            IRasterKeyProperties rasterKeyProps = (IRasterKeyProperties)dataset;
            rasterKeyProps.SetProperty(key, value);
        }

        /// <summary>
        /// Sets band information on items in a mosaic dataset
        /// </summary>
        /// <param name="md">The mosaic dataset with the items</param>
        private static void SetMosaicDatasetItemInformation(IMosaicDataset md)
        {
            // Get the Attribute table from the Mosaic Dataset.
            IFeatureClass featureClass = md.Catalog;
            ISchemaLock schemaLock = (ISchemaLock)featureClass;
            try
            {
                // A try block is necessary, as an exclusive lock might not be available.
                schemaLock.ChangeSchemaLock(esriSchemaLock.esriExclusiveSchemaLock);

                // Get an update cursor going through all the rows in the Mosaic Dataset.
                IFeatureCursor fcCursor = featureClass.Update(null, false);
                // Alternatively, a read cursor can be used if the item does not need to be changed.
                // featureClass.Search(null, false);

                // For each row,
                IRasterCatalogItem rasCatItem = (IRasterCatalogItem)fcCursor.NextFeature();
                while (rasCatItem != null)
                {
                    // get the functionFasterDataset from the Raster field.
                    IFunctionRasterDataset funcDs = (IFunctionRasterDataset)rasCatItem.RasterDataset;
                    if (funcDs != null)
                    {
                        // Check if the 'BandName' property exists in the dataset.
                        bool propertyExists = false;
                        for (int bandId = 0; bandId < funcDs.RasterInfo.BandCount; ++bandId)
                        {
                            var bandNameProperty = GetRasterBandProperty((IDataset)funcDs, bandId, "BandName");
                            if (bandNameProperty != null)
                                propertyExists = true;
                        }

                        if (propertyExists == false && funcDs.RasterInfo.BandCount > 2)
                        {
                            // If the property does not exist and the dataset has at least 3 bands,
                            // set Band Definition Properties for first 3 bands of the dataset.
                            SetBandProperties((IDataset)funcDs);
                            funcDs.AlterDefinition();
                            var rasDs = (IRasterDataset3)funcDs;
                            // Refresh the dataset.
                            rasDs.Refresh();
                        }
                    }

                    fcCursor.UpdateFeature((IFeature)rasCatItem);
                    rasCatItem = (IRasterCatalogItem)fcCursor.NextFeature();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Exception caught in SetMosaicDatasetItemInformation: " + exc.Message);
            }
            finally
            {
                // Set the lock to shared, whether or not an error occurred.
                schemaLock.ChangeSchemaLock(esriSchemaLock.esriSharedSchemaLock);
            }
        }

    }
}
