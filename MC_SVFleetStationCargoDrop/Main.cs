using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MC_SVFleetStationCargoDrop
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.fleetstationcargodrop";
        public const string pluginName = "SV Fleet Station Cargo Drop";
        public const string pluginVersion = "1.0.0";

        private static List<int> droneEquipIDs;

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        [HarmonyPatch(typeof(FleetControl), nameof(FleetControl.UnloadFleetCargo))]
        [HarmonyPrefix]
        private static void UnloadFleetCargo_Pre(FleetControl __instance, Transform ___shipsTrans, CargoSystem ___playerCS)
        {
            if (!(__instance.pc != null && PChar.Char.GetFleetSize > 0))
            {
                return;
            }
            if (!Inventory.instance.inStation)
            {
                InfoPanelControl.inst.ShowWarning(Lang.Get(0, 385), 4, false);
                return;
            }
            Station currStation = Inventory.instance.currStation;
            int num = 0;
            for (int i = 0; i < ___shipsTrans.childCount; i++)
            {
                AIMercenaryCharacter aiMercChar = ___shipsTrans.GetChild(i).GetComponent<FleetMemberSlot>().aiMercChar;
                
                if (aiMercChar.hangarDocked || aiMercChar.dockedStationID == currStation.id)
                {
                    List<CargoItem> cargo = aiMercChar.shipData.cargo;
                    int num2 = 0;
                    for (int j = 0; j < cargo.Count; j++)
                    {
                        CargoItem cargoItem = cargo[j];
                        if (cargoItem.stockStationID == -1 && cargoItem.itemType < 4 &&
                            ((cargoItem.isDronePart && !HasDroneBay(aiMercChar.shipData.equipments)) ||
                              (cargoItem.isAmmo && !NeedsThisAmmo(cargoItem.itemID, aiMercChar.shipData.weapons))))
                        {
                            int stationID;
                            switch (cargoItem.itemType)
                            {
                                case 1:
                                    stationID = -3;
                                    break;
                                case 2:
                                    stationID = -4;
                                    break;
                                case 3:
                                    stationID = (ItemDB.GetItem(cargoItem.itemID).canBeStashed ? -2 : currStation.id);
                                    break;
                                default:
                                    stationID = currStation.id;
                                    break;
                            }
                            ___playerCS.StoreItem(cargoItem.itemType, cargoItem.itemID, cargoItem.rarity, cargoItem.qnt, cargoItem.pricePaid, cargoItem.sellerID, stationID, -1, cargoItem.extraData);
                            num2 += cargoItem.qnt;
                            cargo.RemoveAt(j);
                            j--;
                        }
                    }
                    if (num2 > 0)
                    {
                        SideInfo.AddMsg(Lang.Get(0, 386, aiMercChar.CommanderName(12), num2));
                    }
                    num += num2;
                }
            }
        }

		private static bool NeedsThisAmmo(int ammoID, List<EquipedWeapon> weapons)
        {
            foreach (EquipedWeapon weapon in weapons)
                if (ammoID == GameData.data.weaponList[weapon.weaponIndex].ammo.itemID)
                    return true;

			return false;
        }

        private static bool HasDroneBay(List<InstalledEquipment> installedEquipments)
        {
            if (droneEquipIDs == null)
            {
                droneEquipIDs = new List<int>();
                foreach (Equipment equip in AccessTools.StaticFieldRefAccess<List<Equipment>>(typeof(EquipmentDB), "equipments"))
                    if (equip.equipName.Contains("Drone Bay"))
                        droneEquipIDs.Add(equip.id);
            }

            if (droneEquipIDs.Count > 0)
                foreach (InstalledEquipment ie in installedEquipments)
                    if (droneEquipIDs.Contains(ie.equipmentID))
                        return true;

            return false;
        }
    }
}
