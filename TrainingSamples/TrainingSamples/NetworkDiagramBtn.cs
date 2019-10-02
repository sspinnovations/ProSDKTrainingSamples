using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.NetworkDiagrams;
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
    internal class NetworkDiagramBtn : Button
    {
        protected override async void OnClick()
        {
            try
            {
                // Step 1 - Get a reference to the utility network in the current map
                // string unLayerName = "Electric Utility Network";
                // Map activeMap = MapView.Active.Map;
                // Layer unLayer = await GetLayerByName(activeMap, unLayerName);
                // UtilityNetwork utilityNetwork = await GetUNByLayer(unLayer);

                // Step 2 - Create a queued task to create the diagram on the MCT thread
                // QueuedTask.Run(() =>
                // {
                    // Step 3 - Get a reference to the diagram manager class in the utility network
                    // using (DiagramManager diagramManager = utilityNetwork.GetDiagramManager())
                    // {
                        // Step 4 - Create a list of layer names to include in the diagram
                        // List<string> layerNames = new List<string>
                        // {
                        //     "Transformer",
                        //     "Service Point",
                        //     "Low Voltage"
                        // };

                        // Step 5 - get a list of global ids from the selected features in the list of layers from the 
                        // previous step
                        // List<Guid> globalIds = GetSelectionGlobalIds(activeMap, layerNames);

                        // Step 6 - Get a core template and create a network diagram using the globabl ids of the features found in the selection
                        // DiagramTemplate diagramTemplate = diagramManager.GetDiagramTemplate("CollapseContainers");
                        // NetworkDiagram networkDiagram = diagramManager.CreateNetworkDiagram(diagramTemplate, globalIds);

                        // Step 7 - Apply a smart tree diagram layout to the network diagram
                        // SmartTreeDiagramLayoutParameters diagramLayoutParameters = new SmartTreeDiagramLayoutParameters();
                        // diagramLayoutParameters.Direction = SmartTreeDiagramLayoutParameters.TreeDirection.FromTopToBottom;
                        // networkDiagram.ApplyLayout(diagramLayoutParameters);

                        // Step 8 - Create the diagram map
                        // var newMap = MapFactory.Instance.CreateMap(networkDiagram.Name, MapType.NetworkDiagram, MapViewingMode.Map);
                        // if (newMap == null)
                        //     return null;

                        // Step 9 - Open the diagram map
                        // var mapPane = ArcGIS.Desktop.Core.ProApp.Panes.CreateMapPaneAsync(newMap, MapViewingMode.Map);
                        // if (mapPane == null)
                        //     return null;

                        // Step 10 - Add the diagram to the map
                        // DiagramLayer diagramLayer = newMap.AddDiagramLayer(networkDiagram);
                //     }
                // }).Wait();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An exception occurred: {ex.Message}");
            }
        }

        private static List<Guid> GetSelectionGlobalIds(Map activeMap, List<string> layerNames)
        {
            List<Guid> globalIds = new List<Guid>();

            Dictionary<MapMember, List<long>> mapSelection = activeMap.GetSelection();
            IEnumerable<MapMember> desiredMapMembers = mapSelection.Keys.Where(mm => layerNames.Contains(mm.Name));

            foreach (MapMember mapMember in desiredMapMembers)
            {
                List<long> objectIds = mapSelection[mapMember];
                FeatureLayer featureLayer = mapMember as FeatureLayer;

                QueryFilter queryFilter = new QueryFilter
                {
                    ObjectIDs = objectIds
                };

                using (RowCursor rowCursor = featureLayer.Search(queryFilter))
                {
                    while (rowCursor.MoveNext())
                    {
                        Row row = rowCursor.Current;
                        globalIds.Add(new Guid(row["GLOBALID"].ToString()));
                    }
                }
            }
            return globalIds;
        }

        public Task<Layer> GetLayerByName(Map map, string name)
        {
            return QueuedTask.Run(() =>
            {
                var mapLayers = map.GetLayersAsFlattenedList();
                foreach (Layer layer in mapLayers)
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
                            foreach (Dataset dataset in datasets)
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
