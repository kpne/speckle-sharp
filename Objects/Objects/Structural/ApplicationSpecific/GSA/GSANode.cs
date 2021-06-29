﻿using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;

namespace Objects.Structural.GSA.Geometry
{
    public class GSANode : Node
    {
        public int nativeId { get; set; }
        public int group { get; set; }
        public string springPropertyRef { get; set; }
        public string massPropertyRef { get; set; }
        public string damperPropertyRef { get; set; }
        public double localElementSize { get; set; }
        public string colour { get; set; }
        public GSANode() { }

        /// <summary>
        /// SchemaBuilder constructor for a GSA node
        /// </summary>
        /// <param name="basePoint"></param>
        /// <param name="restraint"></param>
        /// <param name="constraintAxis"></param>
        /// <param name="springPropertyRef"></param>
        /// <param name="massPropertyRef"></param>
        /// <param name="damperPropertyRef"></param>
        /// <param name="localElementSize"></param>
        [SchemaInfo("GSANode", "Creates a Speckle structural node for GSA")]
        public GSANode(int nativeId, Point basePoint, Restraint restraint, Plane constraintAxis = null, int group = 0, string springPropertyRef = null, string massPropertyRef = null, string damperPropertyRef = null, double localElementSize = 0, string colour = "NO_RGB")
        {
            this.nativeId = nativeId;
            this.basePoint = basePoint;
            this.restraint = restraint;
            this.constraintAxis = constraintAxis == null ? new Plane(new Point(0, 0, 0), new Vector(0, 0, 1), new Vector(1, 0, 0), new Vector(0, 1, 0)) : constraintAxis;
            this.group = group;
            this.springPropertyRef = springPropertyRef;
            this.massPropertyRef = massPropertyRef;
            this.damperPropertyRef = damperPropertyRef;
            this.localElementSize = localElementSize;
            this.colour = colour;
        }
    }
}