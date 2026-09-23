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

		// The vanilla range screen sets its two text boxes' min/max only once, in OnSpawn,
		// from whichever target it showed first. Every vanilla target is 0-100 %, so the
		// boxes clamp typed values to 100 on a plate that goes to 2000 kg (and would accept
		// 2000 on a reservoir after showing a plate). Refresh the range on every target.
		[HarmonyPatch(typeof(ActiveRangeSideScreen), nameof(ActiveRangeSideScreen.SetTarget))]
		public static class ActiveRangeSideScreen_SetTarget_Patch
		{
			public static void Postfix(ActiveRangeSideScreen __instance, IActivationRangeTarget ___target,
				KNumberInputField ___activateValueLabel, KNumberInputField ___deactivateValueLabel)
			{
				if (___target == null)
					return;
				foreach (var label in new[] { ___activateValueLabel, ___deactivateValueLabel })
				{
					if (label == null)
						continue;
					label.minValue = ___target.MinValue;
					label.maxValue = ___target.MaxValue;
					// Make sure the box can hold every digit of the largest allowed value.
					int digits = ___target.MaxValue.ToString().Length;
					if (label.field != null && label.field.characterLimit > 0 && label.field.characterLimit < digits)
						label.field.characterLimit = digits;
				}
			}
		}
	}
}
