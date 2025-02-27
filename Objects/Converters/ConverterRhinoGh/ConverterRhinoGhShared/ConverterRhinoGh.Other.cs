using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry.Collections;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using BlockDefinition = Objects.Other.BlockDefinition;
using BlockInstance = Objects.Other.BlockInstance;
using Hatch = Objects.Other.Hatch;
using Polyline = Objects.Geometry.Polyline;
using Text = Objects.Other.Text;
using RH = Rhino.DocObjects;
using Rhino;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    public Rhino.Geometry.Hatch HatchToNative(Hatch hatch)
    {
      var curves = hatch.curves.Select(o => CurveToNative(o));
      var pattern = Doc.HatchPatterns.FindName(hatch.pattern);
      int index;
      if (pattern == null)
      {
        // find default hatch pattern
        pattern = FindDefaultPattern(hatch.pattern);
        index = Doc.HatchPatterns.Add(pattern);
      }
      else
        index = pattern.Index;
      var hatches = Rhino.Geometry.Hatch.Create(curves, index, hatch.rotation, hatch.scale, 0.001);
      return hatches.First();
    }
    public Hatch HatchToSpeckle(Rhino.Geometry.Hatch hatch)
    {
      var _hatch = new Hatch();

      var curves = hatch.Get3dCurves(true).ToList();
      curves.AddRange(hatch.Get3dCurves(false));
      _hatch.curves = curves.Select(o => CurveToSpeckle(o)).ToList();
      _hatch.scale = hatch.PatternScale;
      _hatch.pattern = Doc.HatchPatterns.ElementAt(hatch.PatternIndex).Name;
      _hatch.rotation = hatch.PatternRotation;

      return _hatch;
    }
    private HatchPattern FindDefaultPattern(string patternName)
    {
      var defaultPattern = typeof(HatchPattern.Defaults).GetProperties().Where(o => o.Name.Equals(patternName, StringComparison.OrdinalIgnoreCase)).ToList()?.First();
      if (defaultPattern != null)
        return defaultPattern.GetValue(this, null) as HatchPattern;
      else
        return HatchPattern.Defaults.Solid;
    }

    public BlockDefinition BlockDefinitionToSpeckle(RH.InstanceDefinition definition)
    {
      var geometry = new List<Base>();
      foreach (var obj in definition.GetObjects())
      {
        if (CanConvertToSpeckle(obj))
        {
          Base converted = ConvertToSpeckle(obj);
          if (converted != null)
          {
            converted["Layer"] = Doc.Layers[obj.Attributes.LayerIndex].FullPath;
            geometry.Add(converted);
          }
        }
      }

      var _definition = new BlockDefinition()
      {
        name = definition.Name,
        basePoint = PointToSpeckle(Point3d.Origin), // rhino by default sets selected block def base pt at world origin
        geometry = geometry,
        units = ModelUnits
      };

      return _definition;
    }

    public InstanceDefinition BlockDefinitionToNative(BlockDefinition definition)
    {
      // get modified definition name with commit info
      var commitInfo = GetCommitInfo();
      var blockName = $"{commitInfo} - {definition.name}";

      // see if block name already exists and return if so
      if (Doc.InstanceDefinitions.Find(blockName) is InstanceDefinition def)
        return def;

      // base point
      Point3d basePoint = PointToNative(definition.basePoint).Location;

      // geometry and attributes
      var geometry = new List<GeometryBase>();
      var attributes = new List<ObjectAttributes>();
      foreach (var geo in definition.geometry)
      {
        if (CanConvertToNative(geo))
        {
          GeometryBase converted = null;
          switch (geo)
          {
            case BlockInstance _:
              var instance = (InstanceObject)ConvertToNative(geo);
              converted = instance.Geometry;
              Doc.Objects.Delete(instance);
              break;
            default:
              converted = (GeometryBase)ConvertToNative(geo);
              break;
          }
          if (converted == null)
            continue;
          var layerName = (geo["Layer"] != null) ? $"{commitInfo}{Layer.PathSeparator}{geo["Layer"] as string}" : $"{commitInfo}";
          int index = 1;
          if (layerName != null)
            GetLayer(Doc, layerName, out index, true);
          var attribute = new ObjectAttributes()
          {
            LayerIndex = index
          };
          geometry.Add(converted);
          attributes.Add(attribute);
        }
      }

      int definitionIndex = Doc.InstanceDefinitions.Add(blockName, string.Empty, basePoint, geometry, attributes);

      if (definitionIndex < 0)
        return null;

      var blockDefinition = Doc.InstanceDefinitions[definitionIndex];
      return blockDefinition;
    }

    // Rhino convention seems to order the origin of the vector space last instead of first
    // This results in a transposed transformation matrix - may need to be addressed later
    public BlockInstance BlockInstanceToSpeckle(RH.InstanceObject instance)
    {
      var t = instance.InstanceXform;
      var transformArray = new double[] {
        t.M00, t.M01, t.M02, t.M03,
        t.M10, t.M11, t.M12, t.M13,
        t.M20, t.M21, t.M22, t.M23,
        t.M30, t.M31, t.M32, t.M33 };

      var def = BlockDefinitionToSpeckle(instance.InstanceDefinition);

      var _instance = new BlockInstance()
      {
        insertionPoint = PointToSpeckle(instance.InsertionPoint),
        transform = transformArray,
        blockDefinition = def,
        units = ModelUnits
      };

      return _instance;
    }

    public InstanceObject BlockInstanceToNative(BlockInstance instance)
    {
      // get the block definition
      InstanceDefinition definition = BlockDefinitionToNative(instance.blockDefinition);

      // get the transform
      if (instance.transform.Length != 16)
        return null;
      Transform transform = new Transform();
      int count = 0;
      for (int i = 0; i < 4; i++)
      {
        for (int j = 0; j < 4; j++)
        {
          if (j == 3 && i != 3) // scale the delta values for translation transformations
            transform[i, j] = ScaleToNative(instance.transform[count], instance.units);
          else
            transform[i, j] = instance.transform[count];
          count++;
        }
      }

      // create the instance
      if (definition == null)
        return null;
      Guid instanceId = Doc.Objects.AddInstanceObject(definition.Index, transform);

      if (instanceId == Guid.Empty)
        return null;

      return Doc.Objects.FindId(instanceId) as InstanceObject;
    }

    // Text
    public Text TextToSpeckle(TextEntity text)
    {
      var _text = new Text();

      // display value as list of polylines
      var outlines = text.CreateCurves(text.DimensionStyle, false)?.ToList();
      if (outlines != null)
      {
        foreach (var outline in outlines)
        {
          Rhino.Geometry.Polyline poly = null;
          if (!outline.TryGetPolyline(out poly))
            outline.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out poly); // this is from nurbs, should probably be refined for text
          if (poly != null)
            _text.displayValue.Add(PolylineToSpeckle(poly) as Polyline);
        }
      }

      _text.plane = PlaneToSpeckle(text.Plane);
      _text.rotation = text.TextRotationRadians;
      _text.height = text.TextHeight * text.DimensionScale; // this needs to be multiplied by model space scale for true height
      _text.value = text.PlainText;
      _text.richText = text.RichText;
      _text.units = ModelUnits;

      // rhino specific props
      _text["horizontalAlignment"] = text.TextHorizontalAlignment.ToString();
      _text["verticalAlignment"] = text.TextVerticalAlignment.ToString();

      return _text;
    }
    public TextEntity TextToNative(Text text)
    {
      var _text = new TextEntity();
      _text.Plane = PlaneToNative(text.plane);
      if (!string.IsNullOrEmpty(text.richText))
        _text.RichText = text.richText;
      else
        _text.PlainText = text.value;
      _text.TextHeight = ScaleToNative(text.height, text.units);
      _text.TextRotationRadians = text.rotation;
      
      // rhino specific props
      if (text["horizontalAlignment"] != null)
        _text.TextHorizontalAlignment = Enum.TryParse(text["horizontalAlignment"] as string, out TextHorizontalAlignment horizontal) ? horizontal : TextHorizontalAlignment.Center;
      if (text["verticalAlignment"] != null)
        _text.TextVerticalAlignment = Enum.TryParse(text["verticalAlignment"] as string, out TextVerticalAlignment vertical) ? vertical : TextVerticalAlignment.Middle;

      return _text;
    }
  }
}