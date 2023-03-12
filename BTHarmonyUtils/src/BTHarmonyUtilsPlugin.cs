using BepInEx;
using JetBrains.Annotations;

namespace BTHarmonyUtils {

	/// <summary>
	/// BTHarmonyUtils plugin
	/// </summary>
	[PublicAPI]
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class BTHarmonyUtilsPlugin : BaseUnityPlugin {

		/// <summary>
		/// BTHU plugin guid
		/// </summary>
		public const string pluginGuid = "blazingtwist.harmonyutils";
		
		/// <summary>
		/// BTHU plugin display-name
		/// </summary>
		public const string pluginName = "BT Harmony Utils";
		
		/// <summary>
		/// BTHU plugin version
		/// </summary>
		public const string pluginVersion = "0.2.0";

	}

}
