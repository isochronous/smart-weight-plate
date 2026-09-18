using HarmonyLib;
using PeterHan.PLib.UI;

namespace SmartWeightPlate
{
	public static class Patches
	{
		// Build menu: Automation > Sensors, right after the vanilla Weight Plate.
		[HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
		public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
		{
			public static void Prefix()
			{
				ModUtil.AddBuildingToPlanScreen("Automation", SmartWeightPlateConfig.ID, "sensors",
					FloorSwitchConfig.ID, ModUtil.BuildingOrdering.After);
			}
		}

		// Research: Advanced Automation (LogicCircuits), the tier directly after the
		// Weight Plate's Generic Sensors.
		[HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
		public static class Db_Initialize_Patch
		{
			public const string TechId = "LogicCircuits";

			public static void Postfix()
			{
				Tech tech = Db.Get().Techs.TryGet(TechId);
				if (tech == null)
				{
					Debug.LogWarning("[SmartWeightPlate] Tech '" + TechId + "' not found; building will be unlocked from the start");
					return;
				}
				if (!tech.unlockedItemIDs.Contains(SmartWeightPlateConfig.ID))
					tech.unlockedItemIDs.Add(SmartWeightPlateConfig.ID);
			}
		}

		[HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
		public static class DetailsScreen_OnPrefabInit_Patch
		{
			public static void Postfix()
			{
				PUIUtils.AddSideScreenContent<SmartWeightPlateSideScreen>();
			}
		}
	}
}
