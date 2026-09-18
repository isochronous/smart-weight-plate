using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;

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
	}
}
