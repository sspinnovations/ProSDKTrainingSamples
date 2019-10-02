using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Extensions;
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
    internal class ValidateTopology : Button
    {
        protected override async void OnClick()
        {
            try
            {
                string unLayerName = "Electric Utility Network";
                string domainNetworkName = "ElectricTransmission";
                string tierName = "AC High Voltage";
                Layer unLayer = await GetLayerByName(MapView.Active.Map, unLayerName);
                UtilityNetwork utilityNetwork = await GetUNByLayer(unLayer);

                await QueuedTask.Run(() =>
                {
                    using (UtilityNetworkDefinition utilityNetworkDefinition = utilityNetwork.GetDefinition())
                    {
                        DomainNetwork domainNetwork = utilityNetworkDefinition.GetDomainNetwork(domainNetworkName);
                        Tier tier = domainNetwork.GetTier(tierName);

                        using (SubnetworkManager subnetworkManager = utilityNetwork.GetSubnetworkManager())
                        {
                            Subnetwork dirtySubnetwork = subnetworkManager.GetSubnetworks(tier, SubnetworkStates.Dirty).FirstOrDefault();
                            try
                            {
                                if (dirtySubnetwork != null)
                                {
                                    SubnetworkController subnetworkController = dirtySubnetwork.GetControllers().First();
                                    subnetworkManager.DisableControllerInEditOperation(subnetworkController.Element);
                                    utilityNetwork.ValidateNetworkTopologyInEditOperation();
                                    dirtySubnetwork.Update();

                                    // Redraw map and clear cache
                                    MapView.Active.Redraw(true);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }

                        }
                    }
                    // utilityNetwork.ValidateNetworkTopology();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An exception occurred: {ex.Message}");
            }
            
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

                return null;
            });
        }
    }
}
