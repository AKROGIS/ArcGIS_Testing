# ArcObjects Testing

This repo is a C# Visual Studio solution with a number of
projects (console apps) for testing and documenting the
usage of various functionality of esri's ArcObjects SDK for .Net.

The snippets probably do not represent best practices (I
don't grok COM).  They are handy as a launch pad
for testing out the many holes in the SDK documentation.

## Projects

### Get Mosaic Properties

This snippet was borrowed from esri and includes code
for reading and setting various properties on a raster
mosaic dataset.  See comments in the code for more details.
### Metadata2HTML

 Opens an FGDB feature class and reads its metadata and
 then transforms it into an html file using the esri Stylesheets.
### Read Layer File

 Opens a *.lyr file from a console app and displays various properties.
 The layer file can be modified and then saved. Explore methods on
[ILayerFile](https://desktop.arcgis.com/en/arcobjects/latest/net/webframe.htm#ILayerFile.htm).
### Read Mxd

 Opens a *.mxd file from a console app and displays various properties.
 The map document can be modified and then saved.  Explore methods on
[IMapDocument](https://desktop.arcgis.com/en/arcobjects/latest/net/webframe.htm#IMapDocument.htm).

## Build

Install the ArcObjects SDK (comes with ArcGIS Desktop 10.x)
Open the solution in the version Visual Studio supported by
your version of ArcGIS.  Select `Build Solution` from the
VS menu.

## Deploy

These snippets are not intended to be deployed.

## Using

Read the code and comments for tips on how to use the various
classes in a real application.

Set a break point at the end of the `Main` method in the
`Program` class and run the code in the VS debugger
(select the `debug` as the build configuration and click
the `Start` button).

Add additional code and/or set additional break points to
explore nuances of the ArcObjects SDK.

