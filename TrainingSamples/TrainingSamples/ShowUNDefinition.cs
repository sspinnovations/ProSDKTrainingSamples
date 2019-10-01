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
            string unLayerName = "Electric Utility Network";
            Layer unLayer = await GetLayerByName(MapView.Active.Map, unLayerName);
            UtilityNetwork utilityNetwork = await GetUNByLayer(unLayer);
            string unDefInfo = await QueuedTask.Run(() =>
            {
                UtilityNetworkDefinition utilityNetworkDefinition = utilityNetwork.GetDefinition();

                /* Uncomment to print out the network attributes in the utlity network
                IReadOnlyList<NetworkAttribute> networkAttributes = utilityNetworkDefinition.GetNetworkAttributes();
                string attributesMessage = "Network attributes: " + Environment.NewLine;
                foreach (var networkAttribute in networkAttributes)
                {
                    attributesMessage += networkAttribute.Name + Environment.NewLine;
                }
                MessageBox.Show(attributesMessage);
                */

                /* Uncomment to print out the categories in the utlity network
                IReadOnlyList<string> categories = utilityNetworkDefinition.GetAvailableCategories();
                string categoriesMsg = "Categories: " + Environment.NewLine;
                foreach (var category in categories)
                {
                    categoriesMsg += category + Environment.NewLine;
                }
                MessageBox.Show(categoriesMsg);
                */

                string result = $"Domain Networks: {Environment.NewLine}";
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
