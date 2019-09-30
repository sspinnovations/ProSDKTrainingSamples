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
            // Step 1 - Get reference to utility network in the active map
            // string unLayerName = "UtilityNetwork Utility Network";
            // Layer unLayer = await GetLayerByName(MapView.Active.Map, unLayerName);
            // UtilityNetwork utilityNetwork = await GetUNByLayer(unLayer);
            

            // Step 2 - Create a queued task to run the trace on the MCT thread
            // await QueuedTask.Run(() =>
            // {
                // Step 3 - Use the utility network definition to find a domain network and a corresponding Tier
                // UtilityNetworkDefinition utilityNetworkDefinition = utilityNetwork.GetDefinition();
                // DomainNetwork domainNetwork = utilityNetworkDefinition.GetDomainNetwork("ElectricTransmission");
                // Tier tier = domainNetwork.GetTier("AC High Voltage");

                // Step 4 - Create a subnetwork manager which will be used to find a subnetwork controller
                // using (SubnetworkManager subnetworkManager = utilityNetwork.GetSubnetworkManager())
                // {
                    // Step 5 - Create a trace manager to create a tracer objects
                    // using (TraceManager traceManager = utilityNetwork.GetTraceManager())
                    // {
                        // Step 6 - Create a connected tracer object. The connected trace will trace all elements connected
                        // to the starting point list
                        // ConnectedTracer connectedTracer = traceManager.GetTracer<ConnectedTracer>();

                        // Step 7 - Get subnetworks for the previously defined tier and find the first dirty subnetwork controller
                        // IReadOnlyList<Subnetwork> subnetworks = subnetworkManager.GetSubnetworks(tier, SubnetworkStates.Dirty);
                        // SubnetworkController subnetworkController = subnetworks.First().GetControllers().First();
                        // Element subnetworkElement = subnetworkController.Element;

                        // Step 8 - Create a new trace argument with the subnetwork controller as the starting point
                        // and initialize the configuration object we will modify in the following steps
                        // TraceArgument traceArgument = new TraceArgument(new List<Element> { subnetworkElement });
                        // TraceConfiguration traceConfiguration = new TraceConfiguration();

                        // Step 9 - Set the domain network being traced in the configuration
                        // traceConfiguration.DomainNetwork = domainNetwork;

                        // Step 10 - Use the UN definition to get the network attribute we will use to add barriers to the trace's traversability.
                        // Then create a new network attribute comparision to set the traversability.
                        // NetworkAttribute currentPhase = utilityNetworkDefinition.GetNetworkAttribute("Transmission Phases Current");
                        // NetworkAttributeComparison phaseComparison = new NetworkAttributeComparison(currentPhase, Operator.IncludesTheValues, 3);
                        // traceConfiguration.Traversability.Barriers = phaseComparison;

                        // Step 11 - Set the output condition of the trace to only find elements with the category defined in the following line
                        // traceConfiguration.OutputCondition = new CategoryComparison(CategoryOperator.IsEqual, "Subnetwork Controller");

                        // Step 12 - Find another network attribute and create a function to calculate the max value during the trace. 
                        // Set the functions property on the trace configuration object in order to configure the function.
                        // NetworkAttribute tierRank = utilityNetworkDefinition.GetNetworkAttribute("Tier rank");
                        // Function maxTierRank = new Max(tierRank);
                        // traceConfiguration.Functions = new List<Function> { maxTierRank };

                        // Step 13 - Set the configuration for the trace and run the trace using the tracer objects
                        // traceArgument.Configuration = traceConfiguration;
                        // IReadOnlyList<Result> traceResults = connectedTracer.Trace(traceArgument);

                        // Step 14 - Get the function output result from the trace results
                        // FunctionOutputResult functionOutputResult = traceResults.OfType<FunctionOutputResult>().First();
                        // FunctionOutput functionOutput = functionOutputResult.FunctionOutputs.First();
                        // string maxTierOutput = functionOutput.Value.ToString();

                        // Step 15 - Get the result element from the trace
                        // ElementResult elementResult = traceResults.OfType<ElementResult>().First();

                        // Step 16 - Print the number of connected elements and the output of the trace function
                        // MessageBox.Show($"Total connected devices: {elementResult.Elements.Count}{Environment.NewLine}"
                        //     + $"Max tier rank: {maxTierOutput}");
            //         }
            //     }
            // });

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
