using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PeterHan.PLib.UI;
using UnityEngine;

namespace SmartWeightPlate
{
	/// <summary>
	/// Small PLib panel shown above the vanilla two-slider screen: the current weight and
	/// the Invert Signal checkbox. The thresholds themselves are edited by the vanilla
	/// ActiveRangeSideScreen, which the building qualifies for through IActivationRangeTarget.
	/// </summary>
	public sealed class SmartWeightPlateSideScreen : SideScreenContent, IRender200ms
	{
		private static readonly FieldInfo SideScreensField = AccessTools.Field(typeof(DetailsScreen), "sideScreens");

		/// <summary>Horizontal inset; the root itself carries no margin so it never exceeds the vanilla 280px width.</summary>
		private const int Inset = 8;
		private static readonly Vector2 CheckSize = new Vector2(16f, 16f);

		private SmartWeightPlate target;
		private bool built;
		private GameObject weightLabel;
		private GameObject invertCheck;

		public override bool IsValidForTarget(GameObject go)
		{
			return go != null && go.GetComponent<SmartWeightPlate>() != null;
		}

		public override string GetTitle()
		{
			return ModStrings.SideScreenTitle;
		}

		/// <summary>Higher sorts first; the vanilla range screen is 0.</summary>
		public override int GetSideScreenSortOrder()
		{
			return 1;
		}

		public override void SetTarget(GameObject go)
		{
			base.SetTarget(go);
			target = go != null ? go.GetComponent<SmartWeightPlate>() : null;
			if (target == null)
				return;
			EnsureBuilt();
			RefreshAll();
		}

		public override void ClearTarget()
		{
			base.ClearTarget();
			target = null;
		}

		public void Render200ms(float dt)
		{
			if (target != null && built && gameObject.activeInHierarchy)
				UpdateWeight();
		}

		private void EnsureBuilt()
		{
			if (built)
				return;
			built = true;
			PPanel root = new PPanel("SmartWeightPlateRoot")
			{
				Direction = PanelDirection.Vertical,
				Alignment = TextAnchor.UpperLeft,
				Spacing = 6,
				Margin = new RectOffset(0, 0, 8, 8),
				FlexSize = Vector2.right,
				DynamicSize = true,
			};
			root.AddChild(new PLabel("CurrentWeight")
			{
				Text = " ",
				TextStyle = PUITuning.Fonts.TextDarkStyle,
				TextAlignment = TextAnchor.MiddleLeft,
				Margin = new RectOffset(Inset, Inset, 0, 0),
				FlexSize = Vector2.right,
				DynamicSize = true,
			}.AddOnRealize(go => weightLabel = go));
			root.AddChild(new PCheckBox("Invert")
			{
				Text = ModStrings.Invert,
				ToolTip = ModStrings.InvertTooltip,
				TextStyle = PUITuning.Fonts.TextDarkStyle,
				TextAlignment = TextAnchor.MiddleLeft,
				CheckSize = CheckSize,
				Margin = new RectOffset(Inset, Inset, 0, 0),
				FlexSize = Vector2.right,
				OnChecked = (_, __) => ToggleInvert(),
			}.AddOnRealize(go => invertCheck = go));
			root.AddTo(gameObject);
		}

		private void ToggleInvert()
		{
			if (target == null)
				return;
			target.Invert = !target.Invert;
			RefreshAll();
			RefreshRangeScreen();
		}

		private void RefreshAll()
		{
			if (target == null || !built)
				return;
			if (invertCheck != null)
				PCheckBox.SetCheckState(invertCheck, target.Invert ? PCheckBox.STATE_CHECKED : PCheckBox.STATE_UNCHECKED);
			UpdateWeight();
		}

		private void UpdateWeight()
		{
			if (weightLabel == null || target == null)
				return;
			PUIElements.SetText(weightLabel, string.Format(ModStrings.CurrentWeight, GameUtil.GetFormattedMass(target.CurrentValue)));
		}

		/// <summary>
		/// The vanilla range screen only rebuilds its slider tooltips on SetTarget or a slider
		/// move, so after an inversion re-point it at the same building to pick up the new text.
		/// </summary>
		private void RefreshRangeScreen()
		{
			if (target == null || DetailsScreen.Instance == null || SideScreensField == null)
				return;
			if (!(SideScreensField.GetValue(DetailsScreen.Instance) is List<DetailsScreen.SideScreenRef> refs))
				return;
			foreach (DetailsScreen.SideScreenRef r in refs)
			{
				if (r.screenInstance is ActiveRangeSideScreen range && range.gameObject.activeInHierarchy)
					range.SetTarget(target.gameObject);
			}
		}
	}
}
