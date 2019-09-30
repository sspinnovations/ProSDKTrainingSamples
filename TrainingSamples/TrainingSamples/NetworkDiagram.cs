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
    internal class NetworkDiagram : Button
    {
        protected override async void OnClick()
        {
            string unLayerName = "UtilityNetwork Utility Network";
            Map activeMap = MapView.Active.Map;
            Layer unLayer = await GetLayerByName(activeMap, unLayerName);
            UtilityNetwork utilityNetwork = await GetUNByLayer(unLayer);

            await QueuedTask.Run(() =>
            {
                using (DiagramManager diagramManager = utilityNetwork.GetDiagramManager())
                {
                    // Get selected global ids on the desired layer
                    string layerName = "TODO";
                    List<Guid> globalIds = GetSelectionGlobalIds(activeMap, layerName);

                    DiagramTemplate diagramTemplate = diagramManager.GetDiagramTemplate("TODO");
                    NetworkDiagram networkDiagram = diagramManager.CreateNetworkDiagram(diagramTemplate, globalIds);
                }
            });
        }

        private static List<Guid> GetSelectionGlobalIds(Map activeMap, string layerName)
        {
            List<Guid> globalIds = new List<Guid>();

            Dictionary<MapMember, List<long>> mapSelection = activeMap.GetSelection();
            MapMember desiredMapMember = mapSelection.Keys.Where(mm => mm.Name == layerName).First();
            List<long> objectIds = mapSelection[desiredMapMember];
            FeatureLayer featureLayer = desiredMapMember as FeatureLayer;

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
