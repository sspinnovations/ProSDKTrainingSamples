using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
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
    internal class FindUtilityNetwork : Button
    {
        protected override async void OnClick()
        {
            string unLayerName = "Electric Utility Network";
            Layer unLayer = await GetLayerByName(MapView.Active.Map, unLayerName);
            UtilityNetwork utilityNetwork = await GetUNByLayer(unLayer);
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
