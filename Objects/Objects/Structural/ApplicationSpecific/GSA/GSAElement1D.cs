﻿using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;

namespace Objects.Structural.GSA.Geometry
{
    public class GSAElement1D : Element1D
    {
        public int nativeId { get; set; } //equiv to num record of gwa keyword
        public int group { get; set; }
        public string colour { get; set; }
        public string action { get; set; }
        public bool isDummy { get; set; }        
        public GSAElement1D() { }

        [SchemaInfo("GSAElement1D (from local axis)", "Creates a Speckle structural 1D element for GSA (from local axis)", "GSA", "Geometry")]
        public GSAElement1D(int nativeId, ICurve baseLine, Property1D property, ElementType1D type, 
            [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")] Restraint end1Releases = null, 
            [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")] Restraint end2Releases = null, 
            [SchemaParamInfo("If null, defaults to no offsets")] Vector end1Offset = null, 
            [SchemaParamInfo("If null, defaults to no offsets")] Vector end2Offset = null, Plane localAxis = null)
        {
            this.nativeId = nativeId;
            this.baseLine = baseLine;
            this.property = property;
            this.type = type;
            this.end1Releases = end1Releases == null ? new Restraint("FFFFFF") : end1Releases;
            this.end2Releases = end2Releases == null ? new Restraint("FFFFFF") : end2Releases;
            this.end1Offset = end1Offset == null ? new Vector(0, 0, 0) : end1Offset;
            this.end2Offset = end2Offset == null ? new Vector(0, 0, 0) : end2Offset;
            this.localAxis = localAxis;
        }

        [SchemaInfo("GSAElement1D (from orientation node and angle)", "Creates a Speckle structural 1D element for GSA (from orientation node and angle)", "GSA", "Geometry")]
        public GSAElement1D(int nativeId, ICurve baseLine, Property1D property, ElementType1D type,
            [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")] Restraint end1Releases = null,
            [SchemaParamInfo("If null, restraint condition defaults to unreleased (fully fixed translations and rotations)")] Restraint end2Releases = null,
            [SchemaParamInfo("If null, defaults to no offsets")] Vector end1Offset = null,
            [SchemaParamInfo("If null, defaults to no offsets")] Vector end2Offset = null,
            Node orientationNode = null, double orientationAngle = 0)
        {
            this.nativeId = nativeId;
            this.baseLine = baseLine;
            this.property = property;
            this.type = type;
            this.end1Releases = end1Releases == null ? new Restraint("FFFFFF") : end1Releases;
            this.end2Releases = end2Releases == null ? new Restraint("FFFFFF") : end2Releases;
            this.end1Offset = end1Offset == null ? new Vector(0, 0, 0) : end1Offset;
            this.end2Offset = end2Offset == null ? new Vector(0, 0, 0) : end2Offset;
            this.orientationNode = orientationNode;
            this.orientationAngle = orientationAngle;
        }
    }
}
