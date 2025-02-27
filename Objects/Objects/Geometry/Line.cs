﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry
{
  public class Line : Base, ICurve, IHasBoundingBox
  {
    /// <summary>
    /// OBSOLETE - This is just here for backwards compatibility.
    /// You should not use this for anything. Access coordinates using start and end point.
    /// </summary>

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<double> value
    {
      get
      {
        return null;
      }
      set
      {
        if (value == null)
          return;
        start = new Point(value[0], value[1], value[2]);
        end = new Point(value[3], value[4], value[5]);
      }
    }

    public Interval domain { get; set; }

    public Box bbox { get; set; }

    public double area { get; set; }
    public double length { get; set; }

    public string units { get; set; }

    public Point start { get; set; }
    public Point end { get; set; }
    public Line() { }

    public Line(double x, double y, double z = 0, string units = Units.Meters, string applicationId = null)
    {
      this.start = new Point(x, y, z);
      this.end = null;
      this.applicationId = applicationId;
      this.units = units;
    }

    public Line(Point start, Point end, string units = Units.Meters, string applicationId = null)
    {
      this.start = start;
      this.end = end;
      this.applicationId = applicationId;
      this.units = units;
    }

    public Line(IEnumerable<double> coordinatesArray, string units = Units.Meters, string applicationId = null)
    {
      var enumerable = coordinatesArray.ToList();
      if (enumerable.Count < 6)
        throw new SpeckleException("Line from coordinate array requires 6 coordinates.");
      this.start = new Point(enumerable[0], enumerable[1], enumerable[2], units, applicationId);
      this.end = new Point(enumerable[3], enumerable[4], enumerable[5], units, applicationId);
      this.applicationId = applicationId;
      this.units = units;
    }

    public List<double> ToList()
    {
      var list = new List<double>();
      list.AddRange(start.ToList());
      list.AddRange(end.ToList());
      list.Add(domain.start ?? 0);
      list.Add(domain.end ?? 1);
      list.Add(Units.GetEncodingFromUnit(units));
      list.Insert(0, CurveTypeEncoding.Line);
      list.Insert(0, list.Count);
      return list;
    }

    public static Line FromList(List<double> list)
    {
      var units = Units.GetUnitFromEncoding(list[list.Count - 1]);
      var startPt = new Point(list[2], list[3], list[4], units);
      var endPt = new Point(list[5], list[6], list[7], units);
      var line = new Line(startPt, endPt, units);
      line.domain = new Interval(list[8], list[9]);
      return line;
    }
  }
}
