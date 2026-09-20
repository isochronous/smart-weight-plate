#pragma warning disable 649, 169 // [MyCmpGet]/[MyCmpAdd] fields are populated by the game via reflection
using KSerialization;
using UnityEngine;

namespace SmartWeightPlate
{
	/// <summary>
	/// The building component. Measures the weight on the tile above exactly like the
	/// vanilla Weight Plate (LogicMassSensor): a solid tile, loose items, and anything
	/// carrying a FloorSwitchActivator (Duplicants, critters). The output is a latch with
	/// Reservoir (SmartReservoir) semantics: it becomes "heavy" once the weight reaches the high
	/// threshold and "light" again once the weight drops to the low threshold. Light sends
	/// green, heavy sends red, unless the signal is inverted.
	///
	/// Implements IActivationRangeTarget so the vanilla two-slider side screen edits the
	/// thresholds. That interface names its sliders after the reservoir's use of them:
	/// ActivateValue is the UPPER slider (our high threshold) and DeactivateValue the LOWER
	/// one (our low threshold); the screen enforces upper >= lower.
	/// </summary>
	[SerializationConfig(MemberSerialization.OptIn)]
	public sealed class SmartWeightPlate : KMonoBehaviour, IActivationRangeTarget, ISim200ms
	{
		public static readonly HashedString PortId = "SmartWeightPlateLogicPort";

		public const float MaxMass = 2000f;
		private const int DefaultLow = 10;
		private const int DefaultHigh = 100;

		[Serialize] private int lowThreshold = DefaultLow;
		[Serialize] private int highThreshold = DefaultHigh;
		/// <summary>Latch: set once the weight reaches the high threshold, cleared once it drops to the low one.</summary>
		[Serialize] private bool heavy;
		[Serialize] private bool invert;

		[MyCmpGet] private LogicPorts logicPorts;
		[MyCmpGet] private KSelectable selectable;
		[MyCmpGet] private KBatchedAnimController animController;
		[MyCmpAdd] private CopyBuildingSettings copyBuildingSettings;

		private float massSolid;
		private float massPickupables;
		private float massActivators;

		private HandleVector<int>.Handle solidChangedEntry;
		private HandleVector<int>.Handle pickupablesChangedEntry;
		private HandleVector<int>.Handle activatorChangedEntry;

		private bool spawned;
		private bool visualsInitialized;
		private bool wasPressed;
		private bool wasOn;
		private bool lastSignal;
		private bool signalSent;

		private static readonly EventSystem.IntraObjectHandler<SmartWeightPlate> OnCopySettingsDelegate =
			new EventSystem.IntraObjectHandler<SmartWeightPlate>((component, data) => component.OnCopySettings(data));

		public float CurrentValue => massSolid + massPickupables + massActivators;

		/// <summary>True while the latch is in the heavy state (before inversion).</summary>
		public bool IsHeavy => heavy;

		/// <summary>The signal currently on the output port.</summary>
		public bool IsSignalOn => heavy == invert;

		public bool Invert
		{
			get => invert;
			set
			{
				if (invert == value)
					return;
				invert = value;
				Refresh();
			}
		}

		// ---- IActivationRangeTarget ----

		/// <summary>Upper slider: the high threshold.</summary>
		public float ActivateValue
		{
			get => highThreshold;
			set
			{
				highThreshold = Clamp(value);
				Refresh();
			}
		}

		/// <summary>Lower slider: the low threshold.</summary>
		public float DeactivateValue
		{
			get => lowThreshold;
			set
			{
				lowThreshold = Clamp(value);
				Refresh();
			}
		}

		public float MinValue => 0f;
		public float MaxValue => MaxMass;
		public bool UseWholeNumbers => true;
		public string ActivationRangeTitleText => ModStrings.RangeTitle;
		public string ActivateSliderLabelText => ModStrings.HighLabel;
		public string DeactivateSliderLabelText => ModStrings.LowLabel;
		public string ActivateTooltip => invert ? ModStrings.HighTooltipInverted : ModStrings.HighTooltip;
		public string DeactivateTooltip => invert ? ModStrings.LowTooltipInverted : ModStrings.LowTooltip;

		private static int Clamp(float value)
		{
			return Mathf.Clamp(Mathf.RoundToInt(value), 0, (int)MaxMass);
		}

		// ---- lifecycle ----

		protected override void OnPrefabInit()
		{
			base.OnPrefabInit();
			Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
		}

		protected override void OnSpawn()
		{
			base.OnSpawn();
			int cell = SensedCell;
			solidChangedEntry = GameScenePartitioner.Instance.Add("SmartWeightPlate.SolidChanged", gameObject, cell,
				GameScenePartitioner.Instance.solidChangedLayer, OnSolidChanged);
			pickupablesChangedEntry = GameScenePartitioner.Instance.Add("SmartWeightPlate.PickupablesChanged", gameObject, cell,
				GameScenePartitioner.Instance.pickupablesChangedLayer, OnPickupablesChanged);
			activatorChangedEntry = GameScenePartitioner.Instance.Add("SmartWeightPlate.ActivatorChanged", gameObject, cell,
				GameScenePartitioner.Instance.floorSwitchActivatorChangedLayer, OnActivatorsChanged);
			// Anything already in the cell registered before us and will not raise an event.
			OnSolidChanged(null);
			OnPickupablesChanged(null);
			OnActivatorsChanged(null);
			spawned = true;
			Refresh();
		}

		protected override void OnCleanUp()
		{
			GameScenePartitioner.Instance.Free(ref solidChangedEntry);
			GameScenePartitioner.Instance.Free(ref pickupablesChangedEntry);
			GameScenePartitioner.Instance.Free(ref activatorChangedEntry);
			base.OnCleanUp();
		}

		public void Sim200ms(float dt)
		{
			Refresh();
		}

		private void OnCopySettings(object data)
		{
			SmartWeightPlate other = (data as GameObject)?.GetComponent<SmartWeightPlate>();
			if (other == null)
				return;
			lowThreshold = other.lowThreshold;
			highThreshold = other.highThreshold;
			invert = other.invert;
			Refresh();
		}

		// ---- measurement (mirrors LogicMassSensor) ----

		private int SensedCell => Grid.CellAbove(this.NaturalBuildingCell());

		private void OnSolidChanged(object data)
		{
			int cell = SensedCell;
			massSolid = Grid.IsValidCell(cell) && Grid.Solid[cell] ? Grid.Mass[cell] : 0f;
		}

		private void OnPickupablesChanged(object data)
		{
			float total = 0f;
			int cell = SensedCell;
			var entries = ListPool<ScenePartitionerEntry, SmartWeightPlate>.Allocate();
			Vector2I xy = Grid.CellToXY(cell);
			GameScenePartitioner.Instance.GatherEntries(xy.x, xy.y, 1, 1, GameScenePartitioner.Instance.pickupablesLayer, entries);
			for (int i = 0; i < entries.Count; i++)
			{
				Pickupable pickupable = entries[i].obj as Pickupable;
				if (pickupable == null || pickupable.wasAbsorbed)
					continue;
				KPrefabID id = pickupable.KPrefabID;
				// Critters that walk, hover, or flop count through their FloorSwitchActivator instead.
				if (!id.HasTag(GameTags.Creature) || id.HasTag(GameTags.Creatures.Walker) || id.HasTag(GameTags.Creatures.Hoverer) || id.HasTag(GameTags.Creatures.Flopping))
					total += pickupable.PrimaryElement.Mass;
			}
			entries.Recycle();
			massPickupables = total;
		}

		private void OnActivatorsChanged(object data)
		{
			float total = 0f;
			int cell = SensedCell;
			var entries = ListPool<ScenePartitionerEntry, SmartWeightPlate>.Allocate();
			Vector2I xy = Grid.CellToXY(cell);
			GameScenePartitioner.Instance.GatherEntries(xy.x, xy.y, 1, 1, GameScenePartitioner.Instance.floorSwitchActivatorLayer, entries);
			for (int i = 0; i < entries.Count; i++)
			{
				FloorSwitchActivator activator = entries[i].obj as FloorSwitchActivator;
				if (activator != null)
					total += activator.PrimaryElement.Mass;
			}
			entries.Recycle();
			massActivators = total;
		}

		// ---- latch, signal, visuals ----

		/// <summary>Re-evaluates the latch and pushes the signal, status item, and animation.</summary>
		private void Refresh()
		{
			if (!spawned || logicPorts == null)
				return;
			float mass = CurrentValue;
			if (highThreshold <= lowThreshold)
			{
				// No band to hold across: behave as a plain threshold so an equal pair
				// cannot flip the latch every tick when the weight sits exactly on it.
				heavy = mass >= highThreshold;
			}
			else if (heavy)
			{
				if (mass <= lowThreshold)
					heavy = false;
			}
			else if (mass >= highThreshold)
			{
				heavy = true;
			}

			bool on = IsSignalOn;
			if (!signalSent || on != lastSignal)
			{
				logicPorts.SendSignal(PortId, on ? 1 : 0);
				lastSignal = on;
				signalSent = true;
				if (selectable != null)
					selectable.SetStatusItem(Db.Get().StatusItemCategories.Power,
						on ? Db.Get().BuildingStatusItems.LogicSensorStatusActive : Db.Get().BuildingStatusItems.LogicSensorStatusInactive);
			}
			UpdateVisualState(on);
		}

		/// <summary>
		/// Same animation set as the vanilla plate: "down" while the latch is heavy, "up"
		/// otherwise, in the on or off colour of the current signal.
		/// </summary>
		private void UpdateVisualState(bool on)
		{
			if (animController == null)
				return;
			bool pressed = heavy;
			if (visualsInitialized && pressed == wasPressed && on == wasOn)
				return;
			string state = (on ? "on_" : "off_") + (pressed ? "down" : "up");
			if (!visualsInitialized)
			{
				animController.Play(state);
			}
			else
			{
				animController.Play(state + "_pre");
				animController.Queue(state);
			}
			visualsInitialized = true;
			wasPressed = pressed;
			wasOn = on;
		}
	}
}
