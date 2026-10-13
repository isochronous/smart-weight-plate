namespace SmartWeightPlate
{
	/// <summary>
	/// The mod's text, registered under the game's STRINGS tree by Isochronous.Translations
	/// (ModStrings.BUILDINGS.X is the key STRINGS.BUILDINGS.X, which is how the game finds a
	/// building's name) and replaced from translations/&lt;locale&gt;.po. Code reads the fields
	/// when it needs the text, so a translation loaded later is what the player sees.
	/// </summary>
	public static class ModStrings
	{
		private static readonly string Green = STRINGS.UI.FormatAsAutomationState("Green Signal", STRINGS.UI.AutomationState.Active);
		private static readonly string Red = STRINGS.UI.FormatAsAutomationState("Red Signal", STRINGS.UI.AutomationState.Standby);

		public static class BUILDINGS
		{
			public static class PREFABS
			{
				public static class SMARTWEIGHTPLATE
				{
					public static LocString NAME = STRINGS.UI.FormatAsLink("Smart Weight Plate", "SMARTWEIGHTPLATE");
					public static LocString DESC = "A weight plate that provides high and low thresholds, similar to smart batteries and reservoirs.";
					public static LocString EFFECT =
						"Sends a " + Green + " once the weight on the plate reaches the <b>High Threshold</b>, and keeps sending it until the weight drops to the <b>Low Threshold</b>.\n\n" +
						"Cannot be triggered by " + STRINGS.UI.FormatAsLink("Gas", "ELEMENTS_GAS") + " or " + STRINGS.UI.FormatAsLink("Liquids", "ELEMENTS_LIQUID") + ".";

					public static LocString LOGIC_PORT = "Weight Thresholds";
					public static LocString LOGIC_PORT_ACTIVE = "Sends a " + Green + " when the weight is at or above the <b>High Threshold</b>, until the <b>Low Threshold</b> is reached";
					public static LocString LOGIC_PORT_INACTIVE = "Sends a " + Red + " when the weight is at or below the <b>Low Threshold</b>, until the <b>High Threshold</b> is reached";
					public static LocString LOGIC_PORT_ACTIVE_INVERTED = "Sends a " + Green + " when the weight is at or below the <b>Low Threshold</b>, until the <b>High Threshold</b> is reached";
					public static LocString LOGIC_PORT_INACTIVE_INVERTED = "Sends a " + Red + " when the weight is at or above the <b>High Threshold</b>, until the <b>Low Threshold</b> is reached";
				}
			}
		}

		public static class UI
		{
			public static class UISIDESCREENS
			{
				public static class SMARTWEIGHTPLATE_SIDE_SCREEN
				{
					// Vanilla activation-range side screen (the two-slider panel the reservoirs use).
					public static LocString HIGH_LABEL = "High Threshold:";
					public static LocString LOW_LABEL = "Low Threshold:";
					// Format args: {0} this slider's value, {1} the other slider's value.
					public static LocString HIGH_TOOLTIP = "Sends a " + Green + " once the weight reaches <b>{0} kg</b>, until it drops to <b>{1} kg (Low Threshold)</b>";
					public static LocString LOW_TOOLTIP = "Sends a " + Red + " once the weight drops to <b>{0} kg</b>, until it reaches <b>{1} kg (High Threshold)</b>";
					public static LocString HIGH_TOOLTIP_INVERTED = "Sends a " + Red + " once the weight reaches <b>{0} kg</b>, until it drops to <b>{1} kg (Low Threshold)</b>";
					public static LocString LOW_TOOLTIP_INVERTED = "Sends a " + Green + " once the weight drops to <b>{0} kg</b>, until it reaches <b>{1} kg (High Threshold)</b>";

					// This mod's own side screen.
					public static LocString TITLE = "Smart Weight Plate";
					public static LocString CURRENT_WEIGHT = "Current Weight: {0}";
					public static LocString INVERT = "Invert Signal";
					public static LocString INVERT_TOOLTIP = "Swap the output: " + Green + " while the weight is low, " + Red + " once it reaches the High Threshold";
				}
			}
		}
	}
}
