using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Carto;
using System.Runtime.Serialization.Json;
using System.IO;

namespace makiArcGISStyle
{
  
  class Program
  {
    private static LicenseInitializer m_AOLicenseInitializer = new makiArcGISStyle.LicenseInitializer();
    private static string baseGitPath = "C:\\Users\\<username>\\Documents\\GitHub\\maki\\"; //CORRECT THIS PATH
  
    [STAThread()]
    static void Main(string[] args)
    {
      //ESRI License Initializer generated code.
      if (!m_AOLicenseInitializer.InitializeApplication(new esriLicenseProductCode[] { esriLicenseProductCode.esriLicenseProductCodeBasic, esriLicenseProductCode.esriLicenseProductCodeStandard, esriLicenseProductCode.esriLicenseProductCodeAdvanced },
      new esriLicenseExtensionCode[] { }))
      {
        System.Console.WriteLine(m_AOLicenseInitializer.LicenseMessage());
        System.Console.WriteLine("This application could not initialize with the correct ArcGIS license and will shutdown.");
        m_AOLicenseInitializer.ShutdownApplication();
        return;
      }

      string jsonPath = baseGitPath + "_includes\\maki.json";
      string stylePath = baseGitPath + "ArcGIS\\maki.style";
      string serverStylePath = baseGitPath + "ArcGIS\\maki.ServerStyle";
      string renderPath = baseGitPath + "renders";
      string emfPath = baseGitPath + "emf"; //you'll need to create these locally from the SVG via the bat file
      
      //do the Desktop style
      ImportMaki(jsonPath, stylePath, renderPath, emfPath);
      ConvertVectorPicturesToRepresentationMarkers(stylePath);

      //do the Server style
      ImportMaki(jsonPath, serverStylePath, renderPath, emfPath);
      ConvertVectorPicturesToRepresentationMarkers(serverStylePath);


      //ESRI License Initializer generated code.
      //Do not make any call to ArcObjects after ShutDownApplication()
      m_AOLicenseInitializer.ShutdownApplication();

    }
    static void ImportMaki(string jsonPath, string stylePath, string renderPath, string emfPath)
    {
      IStyleGallery styleGallery = GetStyleGallery(stylePath);
      
      IStyleGalleryStorage styleGalleryStorage = styleGallery as IStyleGalleryStorage;
      File.Delete(stylePath); //delete the existing Style to start from scratch
      styleGalleryStorage.TargetFile = stylePath;

      Icon[] icons = Deserialize(jsonPath);
      System.Array.Sort(icons); //sort by name

      IStyleGalleryItem2 styleGalleryItem = null;
      IStyleGalleryItem2 styleGalleryItemVector = null;
      string tags = "";
      int[] sizes = {12,18,24};
      int[] displays = {1,2};

      //the order here is mainly to produce a pleasing experience in ArcMap.  Add 96 dpi images at each size first
      //then add retina, finally add vector

      foreach (int display in displays) // do regular first, the retina
      {
        foreach (Icon icon in icons)
        {
          if (icon.tags[0] == "deprecated") { continue; }
          foreach (int size in sizes)
          {
            //raster version
            IPictureMarkerSymbol rasterPictureMarkerSymbol = MakeMarkerSymbol(renderPath,icon.icon, size, display, false);
            styleGalleryItem = new StyleGalleryItemClass();
            styleGalleryItem.Item = rasterPictureMarkerSymbol;
            styleGalleryItem.Name = icon.name + " " + size + "px";

            string displayDescrip = (display == 2) ? "Retina" : "";
            styleGalleryItem.Category = "Picture " + displayDescrip;
            
            tags = string.Join(";", icon.tags); //make array into string

            styleGalleryItem.Tags = tags + ";png" + ";" + size;
            styleGallery.AddItem((IStyleGalleryItem)styleGalleryItem);

          }
        }
      }

      //now add vector versions to the end of the list
      //vector version
      foreach (Icon icon in icons)
      {
        if (icon.tags[0] == "deprecated"){continue;}
        foreach (int size in sizes)
        {
          IPictureMarkerSymbol vectorPictureMarkerSymbol = MakeMarkerSymbol(emfPath, icon.icon, size, 1, true);
          styleGalleryItemVector = new StyleGalleryItemClass();
          styleGalleryItemVector.Item = vectorPictureMarkerSymbol;
          styleGalleryItemVector.Name = icon.name + " " + size + "px";
          styleGalleryItemVector.Category = "Vector";
          tags = string.Join(";", icon.tags); //make array into string
          styleGalleryItemVector.Tags = tags + ";emf" + ";" + size;
          styleGallery.AddItem((IStyleGalleryItem)styleGalleryItemVector);
        }
      }

    }
    public static Icon[] Deserialize(string jsonPath)
    {
      System.IO.FileStream jsonStream = new System.IO.FileStream(jsonPath,FileMode.Open);

      DataContractJsonSerializer iconSerializer = new DataContractJsonSerializer(typeof(Icon[]));
      Icon[] icons;
      icons = (Icon[])iconSerializer.ReadObject(jsonStream);

      jsonStream.Close();
      return icons;
    }
    private static IPictureMarkerSymbol MakeMarkerSymbol(string renderPath, string icon, int size, int display, bool isVector)
    {
      esriIPictureType picType = (isVector) ? esriIPictureType.esriIPictureEMF : esriIPictureType.esriIPicturePNG;
      string extrafileName = (display == 2) ? "@2x" : "";
      string suffix = (picType == esriIPictureType.esriIPictureEMF) ? ".emf" : ".png";

      IPictureMarkerSymbol pictureMarkerSymbol = new PictureMarkerSymbolClass();
      pictureMarkerSymbol.CreateMarkerSymbolFromFile(picType, renderPath + "\\" + icon + "-" + size + extrafileName + suffix);
      pictureMarkerSymbol.Size = (((Double)size / 96.0) * 72.0); //so it is 1to1 at 96 dpi or twize that for retina
      return pictureMarkerSymbol;
    }
    private static void ConvertVectorPicturesToRepresentationMarkers(string stylePath) 
    {
      IStyleGallery styleGallery = GetStyleGallery(stylePath);
      IStyleGalleryStorage styleGalleryStorage = styleGallery as IStyleGalleryStorage;
      styleGalleryStorage.TargetFile = stylePath;
      IEnumStyleGalleryItem enumItems = styleGallery.get_Items("Marker Symbols", stylePath, "Vector");

      IStyleGalleryItem3 originalItem = null;
      IStyleGalleryItem3 newItem = null;
      enumItems.Reset();

      originalItem = enumItems.Next() as IStyleGalleryItem3;
      while (originalItem != null)
      {
        newItem = ConvertMarkerItemToRep(originalItem);
        styleGallery.AddItem(newItem);
        originalItem = enumItems.Next() as IStyleGalleryItem3;
      }
      

    }
    private static IStyleGalleryItem3 ConvertMarkerItemToRep(IStyleGalleryItem3 inputItem)
    {
      IMarkerSymbol markerSymbol = inputItem.Item as IMarkerSymbol;
      IRepresentationRule repRule = new RepresentationRuleClass();
      IRepresentationRuleInit repRuleInit = repRule as IRepresentationRuleInit;

      repRuleInit.InitWithSymbol((ISymbol)markerSymbol); //initialize the rep rule with the marker
      IRepresentationGraphics representationGraphics = new RepresentationMarkerClass();

      IGraphicAttributes graphicAttributes = null;
      IRepresentationGraphics tempMarkerGraphics = null;
      IGeometry tempGraphicGeometry = null;
      IRepresentationRule tempRule = null;

      //only pull the markers out.
      for (int i = 0; i < repRule.LayerCount; i++)
      {

        graphicAttributes = repRule.get_Layer(i) as IGraphicAttributes;
        tempMarkerGraphics = graphicAttributes.get_Value((int)esriGraphicAttribute.esriGAMarker) as IRepresentationGraphics;

        tempMarkerGraphics.Reset();
        tempMarkerGraphics.Next(out tempGraphicGeometry, out tempRule);

        while (tempRule != null && tempGraphicGeometry != null)
        {
          representationGraphics.Add(tempGraphicGeometry, tempRule);
          tempGraphicGeometry = null;
          tempRule = null;
          tempMarkerGraphics.Next(out tempGraphicGeometry, out tempRule);
        }
      }

      IStyleGalleryItem3 newMarkerStyleGalleryItem = new ServerStyleGalleryItemClass();
      newMarkerStyleGalleryItem.Item = representationGraphics;
      newMarkerStyleGalleryItem.Name = inputItem.Name;
      newMarkerStyleGalleryItem.Category = inputItem.Category;
      newMarkerStyleGalleryItem.Tags = inputItem.Tags.Replace(";emf", ""); //strip emf from the tags

      return newMarkerStyleGalleryItem;


    }
    static IStyleGallery GetStyleGallery(string stylePath)
    {
      IStyleGallery styleGallery = null;
      String tempPath = stylePath.ToLower();
      if (tempPath.Contains(".serverstyle"))
      {
        styleGallery = new ServerStyleGalleryClass();
      }
      else
      {
        styleGallery = new StyleGalleryClass();
      }
      return styleGallery;
    }
  }
}
