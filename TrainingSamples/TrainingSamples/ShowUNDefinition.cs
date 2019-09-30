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
    internal class ShowUNDefinition : Button
    {
        protected override async void OnClick()
        {
            string unLayerName = "UtilityNetwork Utility Network";
            Layer unLayer = await GetLayerByName(MapView.Active.Map, unLayerName);
            UtilityNetwork utilityNetwork = await GetUNByLayer(unLayer);
            string unDefInfo = await QueuedTask.Run(() =>
            {
                string result = $"Domain Networks: {Environment.NewLine}";
                UtilityNetworkDefinition utilityNetworkDefinition = utilityNetwork.GetDefinition();

                var attrs = utilityNetworkDefinition.GetNetworkAttributes();
                var msg = string.Empty;
                foreach (var attr in attrs)
                {
                    msg += attr.Name + Environment.NewLine;
                }
                MessageBox.Show(msg);

                var categories = utilityNetworkDefinition.GetAvailableCategories();
                var cats = string.Empty;
                foreach (var category in categories)
                {
                    cats += category + Environment.NewLine;
                }
                MessageBox.Show(cats);

                IReadOnlyList<DomainNetwork> domainNetworks = utilityNetworkDefinition.GetDomainNetworks();
                if (domainNetworks != null)
                {
                    foreach (DomainNetwork domainNetwork in domainNetworks)
                    {
                        result += $"{domainNetwork.Name}{Environment.NewLine}";
                    }
                }
                else
                {
                    result += "No domain networks found";
                }

                return result;
            });

            MessageBox.Show(unDefInfo);
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
