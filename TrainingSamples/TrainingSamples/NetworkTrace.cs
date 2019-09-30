using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace TrainingSamples
{
    internal class NetworkTrace : Button
    {
        protected override async void OnClick()
        {
            string unLayerName = "UtilityNetwork Utility Network";
            Layer unLayer = await GetLayerByName(MapView.Active.Map, unLayerName);
            UtilityNetwork utilityNetwork = await GetUNByLayer(unLayer);
            await QueuedTask.Run(() =>
            {
                UtilityNetworkDefinition utilityNetworkDefinition = utilityNetwork.GetDefinition();
                DomainNetwork domainNetwork = utilityNetworkDefinition.GetDomainNetwork("ElectricTransmission");
                Tier tier = domainNetwork.GetTier("AC High Voltage");
                using (SubnetworkManager subnetworkManager = utilityNetwork.GetSubnetworkManager())
                {
                    using (TraceManager traceManager = utilityNetwork.GetTraceManager())
                    {
                        ConnectedTracer downstreamTracer = traceManager.GetTracer<ConnectedTracer>();
                        IReadOnlyList<Subnetwork> subnetworks = subnetworkManager.GetSubnetworks(tier, SubnetworkStates.Dirty);

                        SubnetworkController subnetworkController = subnetworks.First().GetControllers().First();
                        Element subnetworkElement = subnetworkController.Element;

                        TraceArgument traceArgument = new TraceArgument(new List<Element> { subnetworkElement });
                        TraceConfiguration traceConfiguration = new TraceConfiguration();

                        traceConfiguration.DomainNetwork = domainNetwork;

                        NetworkAttribute currentPhase = utilityNetworkDefinition.GetNetworkAttribute("Transmission Phases Current");
                        NetworkAttributeComparison phaseComparison = new NetworkAttributeComparison(currentPhase, Operator.IncludesTheValues, 3);
                        traceConfiguration.Traversability.Barriers = phaseComparison;

                        traceConfiguration.OutputCondition = new CategoryComparison(CategoryOperator.IsEqual, "Subnetwork Controller");

                        NetworkAttribute tierRank = utilityNetworkDefinition.GetNetworkAttribute("Tier rank");
                        Function maxTierRank = new Max(tierRank);
                        traceConfiguration.Functions = new List<Function> { maxTierRank };

                        traceArgument.Configuration = traceConfiguration;
                        IReadOnlyList<Result> traceResults = downstreamTracer.Trace(traceArgument);

                        FunctionOutputResult functionOutputResult = traceResults.OfType<FunctionOutputResult>().First();
                        FunctionOutput functionOutput = functionOutputResult.FunctionOutputs.First();
                        string maxTierOutput = functionOutput.Value.ToString();

                        ElementResult elementResult = traceResults.OfType<ElementResult>().First();
                        MessageBox.Show($"Total connected devices: {elementResult.Elements.Count}{Environment.NewLine}" 
                            + $"Max tier rank: {maxTierOutput}");
                    }
                }
            });
        }

        public Task<Layer> GetLayerByName(Map map, string name)
        {
            return QueuedTask.Run(() =>
            {
                var mapLayers = map.GetLayersAsFlattenedList();
                foreach (var layer in mapLayers)
                {
                    if (layer.Name == name)
                        return layer;
                }
                return null;
            });
        }

        public Task<UtilityNetwork> GetUNByLayer(Layer layer)
        {
            return QueuedTask.Run(() =>
            {
                if (layer is FeatureLayer)
                {
                    FeatureLayer featureLayer = layer as FeatureLayer;
                    using (FeatureClass featureClass = featureLayer.GetFeatureClass())
                    {
                        if (featureClass.IsControllerDatasetSupported())
                        {
                            IReadOnlyList<Dataset> datasets = featureClass.GetControllerDatasets();
                            foreach (var dataset in datasets)
                            {
                                if (dataset is UtilityNetwork)
                                {
                                    return dataset as UtilityNetwork;
                                }
                                else
                                {
                                    dataset.Dispose();
                                }
                            }
                        }
                    }
                }
                else if (layer is UtilityNetworkLayer)
                {
                    UtilityNetworkLayer utilityNetworkLayer = layer as UtilityNetworkLayer;
                    return utilityNetworkLayer.GetUtilityNetwork();
                }
                else if (layer is SubtypeGroupLayer)
                {
                    CompositeLayer compositeLayer = layer as CompositeLayer;
                    foreach (var subLayer in compositeLayer.Layers)
                    {
                        if (subLayer is UtilityNetworkLayer)
                        {
                            UtilityNetworkLayer unLayer = subLayer as UtilityNetworkLayer;
                            return unLayer.GetUtilityNetwork();
                        }
                    }
                }

                return null;
            });
        }
    }
}
