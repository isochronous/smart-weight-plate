using System;
using System.Collections.Generic;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using UnityEngine;

namespace SmartWeightPlate
{
	public sealed class SmartWeightPlateMod : UserMod2
	{
		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
			PUtil.InitLibrary(false);
			ModStrings.Register();
			Debug.Log("[SmartWeightPlate] Loaded version " + typeof(SmartWeightPlateMod).Assembly.GetName().Version);
		}

		public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
		{
			base.OnAllModsLoaded(harmony, mods);
			BetterAutomationOverlayCompat.Apply(harmony);
		}
	}

	/// <summary>
	/// Better Automation Overlay labels every IActivationRangeTarget as "low% - high%", which
	/// is right for the vanilla ones (all percentages) but not for a plate set in kilograms.
	/// When that mod is present, its label for our building is rewritten with the mass unit.
	/// </summary>
	internal static class BetterAutomationOverlayCompat
	{
		private const string LabelType = "BetterLogicOverlay.LogicSettingDisplay.ActivationRangeTargetSetting";

		public static void Apply(Harmony harmony)
		{
			try
			{
				var getSetting = AccessTools.Method(AccessTools.TypeByName(LabelType), "GetSetting");
				if (getSetting == null)
					return;
				harmony.Patch(getSetting, postfix: new HarmonyMethod(typeof(BetterAutomationOverlayCompat), nameof(Postfix)));
				Debug.Log("[SmartWeightPlate] Better Automation Overlay found; plate labels use mass units");
			}
			catch (Exception e)
			{
				Debug.LogWarning("[SmartWeightPlate] Better Automation Overlay compatibility skipped: " + e.Message);
			}
		}

		private static void Postfix(Component __instance, ref string __result)
		{
			if (__instance != null && __instance.TryGetComponent(out SmartWeightPlate plate))
				__result = plate.DeactivateValue + " - " + plate.ActivateValue + STRINGS.UI.UNITSUFFIXES.MASS.KILOGRAM;
		}
	}
}
