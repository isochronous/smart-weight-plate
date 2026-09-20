using STRINGS;

namespace SmartWeightPlate
{
	public static class ModStrings
	{
		private const string PrefabKey = "STRINGS.BUILDINGS.PREFABS.SMARTWEIGHTPLATE.";

		private static readonly string Green = UI.FormatAsAutomationState("Green Signal", UI.AutomationState.Active);
		private static readonly string Red = UI.FormatAsAutomationState("Red Signal", UI.AutomationState.Standby);

		public static readonly string Name = UI.FormatAsLink("Smart Weight Plate", "SMARTWEIGHTPLATE");
		public const string Desc = "Smart weight plates keep a pile of material between two limits, so a sweeper or loader tops it up only once it has run low.";
		public static readonly string Effect =
			"Sends a " + Green + " once the weight on the plate drops to the <b>Low Threshold</b>, and keeps sending it until the weight reaches the <b>High Threshold</b>.\n\n" +
			"Cannot be triggered by " + UI.FormatAsLink("Gas", "ELEMENTS_GAS") + " or " + UI.FormatAsLink("Liquids", "ELEMENTS_LIQUID") + ".";

		public const string PortName = "Weight Thresholds";
		public static readonly string PortActive = "Sends a " + Green + " when the weight is at or below the <b>Low Threshold</b>, until the <b>High Threshold</b> is reached";
		public static readonly string PortInactive = "Sends a " + Red + " when the weight is at or above the <b>High Threshold</b>, until the <b>Low Threshold</b> is reached";

		// Vanilla activation-range side screen (the two-slider panel the reservoirs use).
		public const string RangeTitle = "Weight Thresholds";
		public const string HighLabel = "High Threshold:";
		public const string LowLabel = "Low Threshold:";
		// Format args: {0} this slider's value, {1} the other slider's value.
		public static readonly string HighTooltip = "Sends a " + Red + " once the weight reaches <b>{0} kg</b>, until it drops to <b>{1} kg (Low Threshold)</b>";
		public static readonly string LowTooltip = "Sends a " + Green + " once the weight drops to <b>{0} kg</b>, until it reaches <b>{1} kg (High Threshold)</b>";
		public static readonly string HighTooltipInverted = "Sends a " + Green + " once the weight reaches <b>{0} kg</b>, until it drops to <b>{1} kg (Low Threshold)</b>";
		public static readonly string LowTooltipInverted = "Sends a " + Red + " once the weight drops to <b>{0} kg</b>, until it reaches <b>{1} kg (High Threshold)</b>";

		// This mod's own side screen.
		public const string SideScreenTitleKey = "STRINGS.UI.UISIDESCREENS.SMART_WEIGHT_PLATE_SIDE_SCREEN.TITLE";
		public const string SideScreenTitle = "Smart Weight Plate";
		public const string CurrentWeight = "Current Weight: {0}";
		public const string Invert = "Invert Signal";
		public static readonly string InvertTooltip = "Swap the output: " + Red + " while the weight is low, " + Green + " once it reaches the High Threshold";

		public static void Register()
		{
			Strings.Add(PrefabKey + "NAME", Name);
			Strings.Add(PrefabKey + "DESC", Desc);
			Strings.Add(PrefabKey + "EFFECT", Effect);
			Strings.Add(SideScreenTitleKey, SideScreenTitle);
		}
	}
}
