﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Objects;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops
{
  public class ReceiveLocalComponent : SelectKitAsyncComponentBase
  {
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

    public ReceiveLocalComponent() : base("Local Receive", "LR",
      "Receives data locally, without the need of a Speckle Server. NOTE: updates will not be automatically received.",
      ComponentCategories.SECONDARY_RIBBON, ComponentCategories.SEND_RECEIVE)
    {
      BaseWorker = new ReceiveLocalWorker(this);
    }

    protected override Bitmap Icon => Properties.Resources.LocalReceiver;

    public override Guid ComponentGuid => new Guid("43E22B36-891B-4478-8A4E-2338272EA3B3");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("localDataId", "id", "ID of the local data sent.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "Data to send.", GH_ParamAccess.tree);
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:");
      var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino6);

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true,
          kit.Name == Kit.Name);
      }

      base.AppendAdditionalComponentMenuItems(menu);
    }
    
    public override void AddedToDocument(GH_Document document)
    {
      SetDefaultKitAndConverter();
      base.AddedToDocument(document);
    }
    
    public void SetConverterFromKit(string kitName)
    {
      if (kitName == Kit.Name)return;

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Applications.Rhino6);

      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    public bool foundKit;
    private void SetDefaultKitAndConverter()
    {
      try
      { 
        Kit = KitManager.GetDefaultKit();
        Converter = Kit.LoadConverter(Applications.Rhino6);
        Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
        foundKit = true;
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
        foundKit = false;
      }
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (!foundKit)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No kit found on this machine.");
        return;
      }
      base.SolveInstance(DA);
    }

    protected override void BeforeSolveInstance()
    {
      Tracker.TrackPageview(Tracker.RECEIVE_LOCAL);
      base.BeforeSolveInstance();
    }
  }
  public class ReceiveLocalWorker : WorkerInstance
  {
    private GH_Structure<IGH_Goo> data;
    private string localDataId;
    public ReceiveLocalWorker(GH_Component _parent) : base(_parent) { }

    public override WorkerInstance Duplicate() => new ReceiveLocalWorker(Parent);

    public override void DoWork(Action<string, double> ReportProgress, Action Done)
    {
      try
      {
        Parent.Message = "Receiving...";
        var Converter = (Parent as ReceiveLocalComponent).Converter;

        Base @base = null;

        try
        {
          @base = Operations.Receive(localDataId, disposeTransports: true).Result;
        }
        catch (Exception e)
        {
          RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Failed to receive local data."));
          Done();
          return;
        }

        data = Utilities.ConvertToTree(Converter, @base);
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Log.CaptureException(e);
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message));
        Parent.Message = "Error";
      }
      Done();
    }

    List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new List<(GH_RuntimeMessageLevel, string)>();

    public override void SetData(IGH_DataAccess DA)
    {
      if(data != null) DA.SetDataTree(0, data);
      
      foreach (var (level, message) in RuntimeMessages)
      {
        Parent.AddRuntimeMessage(level, message);
      }

      Parent.Message = "Done";
    }

    public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
    {
      DA.GetData(0, ref localDataId);
      data = null;
    }
  }
}
