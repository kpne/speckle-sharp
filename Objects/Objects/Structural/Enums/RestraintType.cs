﻿using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;

namespace Objects.Structural.Geometry
{
    public enum RestraintType
    {
        Free, //Release
        Pinned,
        Fixed,
        //Spring //flexible
        //rigid, free, flexible, comp only, tens only, flex comp only, flex tens only, non lin <-- SAF
        //free, fixed, fixed negative, fixed positive, spring, spring negative, spring positive, spring relative, spring relative neg, spring relative pos, non lin, friction, damped, gap <-- BHoM
    }

    public enum RestraintDescription
    {
        none,
        all,
        x, 
        y,
        z, 
        xy,
        xz,
        yz
    }
}

