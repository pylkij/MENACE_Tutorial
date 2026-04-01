using System;
using System.Reflection;

using MelonLoader;
using Menace.ModpackLoader;
using HarmonyLib;
using Menace.SDK;

using Il2CppMenace.Tactical;

namespace YourFirstMod;

public class Plugin : IModpackPlugin
{
	private static MelonLogger.Instance _log;
	private static HarmonyLib.Harmony _harmony;
	private static bool _applyAccuracyDebuff = false;
	private static bool _debugLogging = false;

	public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
	{
		_log = logger;
		_harmony = harmony; // we will need this for later
		_log.Msg("YourFirstMod loaded.");

		try
		{
			Patch_InvokeOnAttackTileStart();
			Patch_OnGetAccuracy();
	  	}

		catch (Exception ex)
		{
			_log.Error("Failed to patch InvokeOnAttackTileStart:");
			_log.Error(ex.ToString());
		}
	}

	private static void Patch_InvokeOnAttackTileStart()
	{
		// A check to make sure we can actually find the TacticalManager
		var tacticalManager = GameState.FindManagedType("Il2CppMenace.Tactical.TacticalManager");
		if (tacticalManager == null)
		{
			_log.Error("TacticalManager not found.");
			return;
		}

		// Using reflection, we are going to search through the methods.
		// Without this, GetMethod uses a default that only finds public
		// instance methods. Probably, the method is public, but the SDK
		// uses this pattern as a safety net, so we will too.
		var attackMethod = tacticalManager.GetMethod("InvokeOnAttackTileStart",
		    System.Reflection.BindingFlags.Instance |
		    System.Reflection.BindingFlags.Public |
		    System.Reflection.BindingFlags.NonPublic);

		// If the method can't be found, return.
		if (attackMethod == null)
		{
		    _log.Error("InvokeOnAttackTileStart not found.");
		    return;
		}

		// The postfix patch
		var attackPostfix = typeof(Plugin).GetMethod(nameof(OnAttackTileStart_Postfix),
		    System.Reflection.BindingFlags.Static |
		    System.Reflection.BindingFlags.NonPublic);

		_harmony.Patch(attackMethod, postfix: new HarmonyMethod(attackPostfix));
		_log.Msg("Patched InvokeOnAttackTileStart.");
	}

	private static void Patch_OnGetAccuracy()
	{
		var entityProperties = GameState.FindManagedType("Il2CppMenace.Tactical.EntityProperties");
		if (entityProperties == null)
		{
			_log.Error("EntityProperties not found.");
			return;
		}

		var accuracyMethod = entityProperties.GetMethod("GetAccuracy",
    	BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		var accuracyPostfix = typeof(Plugin).GetMethod(nameof(OnGetAccuracy_Postfix),
    	BindingFlags.Static | BindingFlags.NonPublic);

		_harmony.Patch(accuracyMethod, postfix: new HarmonyMethod(accuracyPostfix));
		_log.Msg("Patched GetAccuracy.");
	}

	public void OnSceneLoaded(int buildIndex, string sceneName) { }

	// Helper
	private static IntPtr GetPointer(object obj)
	{
	    if (obj == null) return IntPtr.Zero;
	    if (obj is Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase il2cppObj)
	        return il2cppObj.Pointer;
	    return IntPtr.Zero;
	}

	// When an attack starts
	private static void OnAttackTileStart_Postfix(object _actor, object _targetTile, object _skill)
	{
		if (_debugLogging) _log.Msg("An attack has started.");

		// Wrap raw IntPtr values in GameObj before passing. We do this because
		// GetActorOnTile and CanActorSee both expect GameObj
		var tileObj = new GameObj(GetPointer(_targetTile));
		var attackerObj = new GameObj(GetPointer(_actor));

		var templateProp = _skill.GetType().GetProperty("m_Template",
		    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		if (templateProp != null)
		{
		    var template = templateProp.GetValue(_skill);
		    if (template != null)
		    {
		        var losProp = template.GetType().GetProperty("IsLineOfFireNeeded",
		            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		        if (losProp != null)
		        {
		            bool isLoFNeeded = (bool)losProp.GetValue(template);
		            if (!isLoFNeeded)
		            {
		                if (_debugLogging) _log.Msg("Skill does not require line of fire, skipping debuff.");
		                return;
		            }
		        }
		    }
		}

		// Is there an actor on the target tile?
		var target = TileMap.GetActorOnTile(tileObj);
		if (target == GameObj.Null)
		{
			if (_debugLogging) _log.Msg("There was no target on that tile.");
			return;
		}

		// Can the attacker see the target?
		if (LineOfSight.CanActorSee(attackerObj, target))
		{
			if (_debugLogging) _log.Msg("The attacker can see the target.");
			return;
		}

		// Intercept the attack accuracy
		_applyAccuracyDebuff = true;
	}

	private static void OnGetAccuracy_Postfix(object __instance, ref float __result)
	{
	   	if (_applyAccuracyDebuff)
	    {
	    	__result *= 0.8f;
	    	_applyAccuracyDebuff = false;
	    	if (_debugLogging) _log.Msg("Accuracy debuff applied");
	    }
	}

    public void OnUpdate() { }
    public void OnGUI() { }
	public void OnUnload() { }
}