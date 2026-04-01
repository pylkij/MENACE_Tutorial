# Your First Mod - MENACE

This tutorial will walk you step by step through the creation of your first ever mod for the game MENACE using C#, the MelonLoader, and the Menace Modkit (MMK). We will go step by step through the stages of creating a mod, with each step building on the framework established by the step before. This tutorial will cover setup, planning, implementation, bugfixing, patching, and a whole lot more bug fixing.

This is **not** an exhaustive reference for all things possible. Instead, think of this tutorial as a starting point to get you comfortable with making a basic mod from which you can explore your own dreams and ambitions. Starting with the basics will save you a lot of time and effort in the future. Trust me: I started my mod making journey by jumping straight in to the deep end and was only saved by a timely intervention.

## Setup

Every mod for MENACE written in C# will follow a similar basic structure. As you get more advanced, these hard and fast rules will become guidelines. But for now, stick to established patterns. Not only does this make your life easier, but it also makes helping you easier for other modders. The easier your code is to read and follow, the better it is as a beginner.

### Directory Structure

The directory structure will be created by the MMK in the staging folder when you click `+ Create New` on the `Mod Loader` tab of the MMK. By default, on windows this will be `C:\Users\YourUserName\Documents\MenaceModkit\staging`. For now, the only directory we are interested in is `src/`. `build/` will be handled by the MMK when you deploy your mod by clicking `Deploy to Game` in the MMK for the first time. 

```
YourFirstMod/
├── assets/
├── build/
│		└── YourFirstMod.dll
├── clones/
├── src/
│		└── YourFirstMod.cs
├── stats/
└── modpack.json
```

### YourFirstMod.cs

Create a `.cs` file in the `src/` directory and add this:

```csharp
using System;

using MelonLoader;
using Menace.ModpackLoader;

namespace YourFirstMod;

public class Plugin : IModpackPlugin
{
	private MelonLogger.Instance _log;
	private HarmonyLib.Harmony _harmony;

	public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
	{
		_log = logger;
		_harmony = harmony; // we will need this for later
		_log.Msg("YourFirstMod loaded.");
	}

  public void OnSceneLoaded(int buildIndex, string sceneName) { }
  public void OnUpdate() { }
  public void OnGUI() { }
  public void OnUnload() { }	
}
```

This is your mod. Currently, it doesn't do much: this mod will compile, it will be successfully loaded, and it will print a log line to let you know it was successful to the MelonLoader log (`Menace/MelonLoader/Latest.log`).

### modpack.json

This tells the MMK what it needs to do. What files it needs to compile, where they are located, and what the mod contains.

```json
{
  "manifestVersion": 2,
  "name": "YourFirstMod",
  "version": "1.0.0",
  "author": "Pylkij",
  "description": "A basic guide to getting started modding the game MENACE using c#",
  "createdDate": "2026-03-31T10:12:17.4186251-07:00",
  "modifiedDate": "2026-03-31T10:12:17.4186531-07:00",
  "loadOrder": 100,
  "dependencies": [],
  "code": {
    "sources": [
      "src/YourFirstMod.cs"
    ],
    "references": [
      "MelonLoader",
      "HarmonyLib",
      "Menace.ModpackLoader"
    ],
    "prebuiltDlls": [],
    "hasAnySources": true,
    "hasAnyPrebuilt": false,
    "hasAnyCode": true
  },
  "patches": {},
  "bundles": [],
  "assets": {},
  "securityStatus": "SourceVerified",
  "repositoryType": "None",
  "hasCode": true,
  "hasPatches": false,
  "hasBundles": false,
  "hasAssets": false
}
```

### First launch

Hit `Deploy to Game` once all of your files are in place, the MMK will compile the `YourFirstMod.dll`. I find that you have to deploy any new `.dll` twice. Often, the first compile will glitch, so just `Undeploy` and then `Deploy to Game` again. I don't know why this is. It could be my system. It could be a feature of the MMK. It doesn't really matter in the grand scheme of things.

Now, launch the game and get all the way to the Title Screen. Open `Latest.log` and check the log for these lines:

```
[10:20:57.497] [Menace_Modpack_Loader] Loading modpacks from: C:\Program Files (x86)\Steam\steamapps\common\Menace\Mods
[10:20:57.564] [Menace_Modpack_Loader]   Loaded [v2]: YourFirstMod v1.0.0 (order: 100)
[10:20:57.573] [Menace_Modpack_Loader]   [YourFirstMod] Loaded DLL: YourFirstMod.dll [source-verified]
[10:20:57.575] [Menace_Modpack_Loader]   [YourFirstMod] Discovered plugin: Plugin
[10:20:57.578] [Menace_Modpack_Loader] Loaded 1 modpack(s)
[10:20:57.579] [YourFirstMod] YourFirstMod loaded.
``` 

Congratulations, you have modded MENACE. Your mod doesn't do anything, but it works! A solid start.

## Planning

Now that we have a mod that compiles, loads, and prints a log message, let's move on to the planning stage. In this step, we will identify a problem, propose a solution, and collect the required information to implement the proposed solution. We don't want to write any code at this stage. Instead, try to keep the descriptions in plain language.

### The Problem

When attacking a tile with a hidden enemy on it with direct fire weapons, units suffer no accuracy penalty - this idea comes courtesy of Beagle.

### The Proposed Solution

To counteract this, we want a mod which:

1. Detects when an attack is made.
2. Checks if the tile being attacked has a unit on it.
3. Checks if that unit is hidden to the attacker.
4. Applies an accuracy debuff. For now, a 20% penalty.

### The How

The next step during the planning process is identifying **how** all of these steps might be accomplished using the available tools. The first place to check should always be the [Menace SDK](https://github.com/p0ss/MenaceAssetPacker/blob/main/docs/coding-sdk/what-is-sdk.md). This is a fantastic resource for modding that allows us to avoid as much IL2CPP shenanigans as possible. It is exposed through the `Menace.SDK` namespace, and requires no additional effort on the part of the modder.

Next, we will review the [API Documentation](https://github.com/p0ss/MenaceAssetPacker/tree/main/docs/coding-sdk/api) and see what parts should help us accomplish each of our goals:

1. [TacticalEventHooks](https://github.com/p0ss/MenaceAssetPacker/blob/main/docs/coding-sdk/api/tactical-event-hooks.md)
	- This is how we will detect when an attack is made. 
	- During OnInitialize, we will subscribe to `OnAttackTileStart`
	- Conveniently, this Event returns the attacker and the tile being targeted.
2. [TileMap](https://github.com/p0ss/MenaceAssetPacker/blob/main/docs/coding-sdk/api/tile-map.md)
	- This is how we will check if the tile has an actor on it.
	- `GetActorOnTile` will tell us who is on the tile, and will return `GameObj.Null` if the tile is empty.
3. [LineOfSight](https://github.com/p0ss/MenaceAssetPacker/blob/main/docs/coding-sdk/api/line-of-sight.md)
	- This is how we will check if the attacker can see the target.
	- `CanActorSee` takes `actor` and `target` and returns `true` or `false`
4. [Intercept](https://github.com/p0ss/MenaceAssetPacker/blob/main/docs/coding-sdk/api/intercept.md)
	- `OnGetAccuracy` looks to do what we need.
	- We subscribe to it the same as `OnAttackTileStart`, and then modify it by changing the `result`.

There is one outstanding problem: we only want to modify direct fire weapon attacks, not all attacks. There is a solution to this, but it is quite complicated to implement, so we won't worry about that for now.

## Implementation

In order for the code to do what we want, we now need to take all of these parts, and make them work together. First, let's make the namespace declaration:

```csharp
using System;

using MelonLoader;
using Menace.ModpackLoader;
using Menace.SDK; // <-- New

namespace YourFirstMod;
```

Next, let's subscribe to `TacticalEventHooks`:

```csharp
	public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
	{
		_log = logger;
		_harmony = harmony; // We will need this for later. Ominous foreshadowing
		_log.Msg("YourFirstMod loaded.");

		// Initialize the event hook before subscribing. Note: the .md does not
		// declare this but the TacticalEventHooks.cs does. There are some 
		// documentation gaps like this.
		TacticalEventHooks.Initialize(_harmony);

		TacticalEventHooks.OnAttackTileStart += OnAttackStart;
	}
```

Any time you use a subscription, you also need to clean up the handlers:

```csharp
	public void OnUnload()
	{
		TacticalEventHooks.OnAttackTileStart -= OnAttackStart;
	}
```

Now, we need to tell the code what to actually do with those subscriptions:

```csharp
	// When an attack starts
	private void OnAttackStart(IntPtr attacker, IntPtr tile)
	{
		_log.Msg("An attack has started.");

		// Wrap raw IntPtr values in GameObj before passing. We do this because
		// GetActorOnTile and CanActorSee both expect GameObj
		var tileObj = new GameObj(tile);
		var attackerObj = new GameObj(attacker);

		// Is there an actor on the target tile?
		var target = TileMap.GetActorOnTile(tileObj);
		if (target == GameObj.Null)
		{
			_log.Msg("There was no target on that tile.");
			return;
		}

		// Can the attacker see the target?
		if (LineOfSight.CanActorSee(attackerObj, target))
		{
			_log.Msg("The attacker can see the target.");
			return;
		}

		// Intercept the attack accuracy
		Intercept.OnGetAccuracy += (GameObj props, GameObj owner, ref float result) =>
		{
			// Apply debuff
			result *= 0.8f;
			_log.Msg("Accuracy debuff applied");
		};
	}
```

## Debugging

The mod compiles, it loads, you get in game and... no log message after an attack was made. Welcome to programming. This is why you included all of those nice comment lines at every stage so you can see what is going on and where the mod is failing. In our case, right at the very start.

So, what do we do now? Well, the logs do show us two useful lines:

```
[11:03:54.282] [Menace_Modpack_Loader] [TacticalEventHooks] TacticalManager type not found
...
[11:03:54.283] [Menace_Modpack_Loader] [TacticalEventHooks] TacticalManager type not found
```

It could be that we are simply trying to subscribe to the TacticalEventHook before it is possible. Easy enough fix. Let's move the subscription to the tactical scene:

```csharp
	public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
	{
		_log = logger;
		_harmony = harmony; // we will need this for later
		_log.Msg("YourFirstMod loaded.");
	}

	public void OnSceneLoaded(int buildIndex, string sceneName)
	{
		if (sceneName == "Tactical")
		{
			TacticalEventHooks.Initialize(_harmony);
			TacticalEventHooks.OnAttackTileStart += OnAttackStart;
			_log.Msg("Subscribed to OnAttackTileStart — check for TacticalEventHooks init message above.");
		}
	}
```

And let's add a debug line sow we know where to check the log. Unfortunately, subscribing doesn't have a return status, so we can't directly confirm the Harmony patches succeeded. But, the SDK source tells us that it should fire a message to the logs itself after initialization succeeds. So, our message line just reminds us to look for something like `[TacticalEventHooks] Initialized with {patchCount} event hooks`

```
[11:20:29.615] [Menace_Modpack_Loader] [TacticalEventHooks] TacticalManager type not found
[11:20:29.616] [YourFirstMod] Subscribed to OnAttackTileStart — check for TacticalEventHooks init message above.
```

No joy. I smell a bug. Let's add a diagnostics line in `OnSceneLoaded`:

```csharp
	public void OnSceneLoaded(int buildIndex, string sceneName)
	{
		if (sceneName == "Tactical")
		{
	    // Temporary diagnostic — remove after
	    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
	    {
	        if (asm.GetName().Name.Contains("Menace") || asm.GetName().Name.Contains("CSharp"))
	            _log.Msg($"Assembly: {asm.GetName().Name}");
	    }

			TacticalEventHooks.Initialize(_harmony);
			TacticalEventHooks.OnAttackTileStart += OnAttackStart;
			_log.Msg("Subscribed to OnAttackTileStart — check for TacticalEventHooks init message above.");
		}
	}
```

```
[11:25:23.847] [YourFirstMod] Assembly: Microsoft.CodeAnalysis.CSharp
[11:25:23.847] [YourFirstMod] Assembly: Menace.DataExtractor
[11:25:23.848] [YourFirstMod] Assembly: Menace.ModpackLoader
[11:25:23.848] [YourFirstMod] Assembly: Assembly-CSharp
[11:25:23.849] [YourFirstMod] Assembly: Microsoft.CSharp
[11:25:23.849] [YourFirstMod] Assembly: Assembly-CSharp-firstpass
[11:25:23.850] [Menace_Modpack_Loader] [TacticalEventHooks] TacticalManager type not found
[11:25:23.850] [YourFirstMod] Subscribed to OnAttackTileStart — check for TacticalEventHooks init message above.
```

Good. `Assembly-CSharp` is there, so that's not the problem. The SDK is finding the right assembly, it just can't find `TacticalManager` inside it. Maybe we are still too early. The SDK does have another helpful tool for us [GameState](https://github.com/p0ss/MenaceAssetPacker/blob/main/docs/coding-sdk/api/game-state.md), specifically: `TacticalReady`. This delays until 30 ticks after a tactical scene is detected to give everything time to fully load. Let's use that instead of OnSceneLoaded.

```csharp
	public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
	{
		_log = logger;
		_harmony = harmony; // we will need this for later
		_log.Msg("YourFirstMod loaded.");

		GameState.TacticalReady += OnTacticalReady;
	}

	private void OnTacticalReady()
	{
		TacticalEventHooks.Initialize(_harmony);
		TacticalEventHooks.OnAttackTileStart += OnAttackStart;
		_log.Msg("Subscribed to OnAttackTileStart.");
	}

	public void OnSceneLoaded(int buildIndex, string sceneName) { }
```

Because this is a new subscription, make sure to unsubscribe OnUnload:

```csharp
	public void OnUnload()
	{
		GameState.TacticalReady -= OnTacticalReady;
		TacticalEventHooks.OnAttackTileStart -= OnAttackStart;
	}
```

```
[11:36:11.597] [Menace_Modpack_Loader] [TacticalEventHooks] TacticalManager type not found
...
[11:36:37.850] [Menace_Modpack_Loader] [TacticalEventHooks] TacticalManager type not found
[11:36:37.850] [YourFirstMod] Subscribed to OnAttackTileStart.
```

So, our OnTacticalReady delay worked. However, `TacticalManager type not found` still means that it is failing, even this late in the process. There is another clue: earlier in the log something polls `TacticalManager`, and fails to find it. Unfortunately, the SDK has a guard `if (_initialized) return;` and it would appear that this guard is being set even though the the subscription fails, blocking future attempts. Let's sidestep the SDK.

## Patching

Patches can be an intimidating process, but it doesn't need to be. In fact, all the SDK is doing is patching. That's super helpful because it means we can borrow the SDKs approach, and just run the patch later in the boot sequence to make sure it goes through.

There are different types of patches, and each has its use-cases. To start with, `Prefix` and `Postfix` are what you are going to use. To decide which, think: am I trying to intercept the existing method before it does anything, or am I trying to modify the result of the method? If you are modifying the result (like we are here), `Postfix`.

```csharp
	private void OnTacticalReady()
	{
	    // A check to make sure we can actually find the TacticalManager
	    var tacticalManager = GameState.FindManagedType("Menace.Tactical.TacticalManager");
	    if (tacticalManager == null)
	    {
	        _log.Error("TacticalManager not found.");
	        return;
	    }

	    // Using reflection, we are going to search through the methods.
	    // Without this, GetMethod uses a default that only finds public
	    // instance methods. Probably, the method is public, but the SDK
	    // uses this pattern as a safety net, so we will too.
	    var method = tacticalManager.GetMethod("InvokeOnAttackTileStart",
	        System.Reflection.BindingFlags.Instance |
	        System.Reflection.BindingFlags.Public |
	        System.Reflection.BindingFlags.NonPublic);

	    // If the method can't be found, return.
	    if (method == null)
	    {
	        _log.Error("InvokeOnAttackTileStart not found.");
	        return;
	    }

	    // The postfix patch
	    var postfix = typeof(Plugin).GetMethod(nameof(OnAttackTileStart_Postfix),
	        System.Reflection.BindingFlags.Static |
	        System.Reflection.BindingFlags.NonPublic);

	    _harmony.Patch(method, postfix: new HarmonyMethod(postfix));
	    _log.Msg("Patched InvokeOnAttackTileStart.");
	}
```

Because we are now using Reflection, add that to our using declarations at the top:

```csharp
using System;
using System.Reflection;
```

Update `_log` and `_harmony` to `static` since they are being accessed differently now:

```csharp
	private static MelonLogger.Instance _log;
	private static HarmonyLib.Harmony _harmony;
```

Update OnAttackStart to become a postfix and update how we are passing the variables `attackerObj` and `tileObj`:

```csharp
	// When an attack starts
	private static void OnAttackTileStart_Postfix(object attacker, object tile)
	{
		_log.Msg("An attack has started.");

		// Wrap raw IntPtr values in GameObj before passing. We do this because
		// GetActorOnTile and CanActorSee both expect GameObj
		var tileObj = new GameObj((IntPtr)tile);
		var attackerObj = new GameObj((IntPtr)attacker);

		// Is there an actor on the target tile?
		var target = TileMap.GetActorOnTile(tileObj);
		if (target == GameObj.Null)
		{
			_log.Msg("There was no target on that tile.");
			return;
		}

		// Can the attacker see the target?
		if (LineOfSight.CanActorSee(attackerObj, target))
		{
			_log.Msg("The attacker can see the target.");
			return;
		}

		// Intercept the attack accuracy
		Intercept.OnGetAccuracy += (GameObj props, GameObj owner, ref float result) =>
		{
			// Apply debuff
			result *= 0.8f;
			_log.Msg("Accuracy debuff applied");
		};
	}
```

and finally, remove the TacticalEventHooks unsubscription:

```csharp
	public void OnUnload()
	{
		GameState.TacticalReady -= OnTacticalReady;
	}
```

Run it:

```
[12:16:36.157] [YourFirstMod] TacticalManager not found.
```

Hunh. Let's run a diagnostic scan to see what's actually in the assembly:

```csharp
	private void OnTacticalReady()
	{
		// Temp debugging diagnostic
	    var asm = GameState.GameAssembly;
	    if (asm == null)
	    {
	        _log.Error("GameAssembly is null.");
	        return;
	    }

	    foreach (var type in asm.GetTypes())
	    {
	        if (type.Name.Contains("TacticalManager"))
	            _log.Msg($"Found type: {type.FullName}");
	    }
	  // ... The rest of OnTacticalReady()
```

```
[12:16:36.153] [YourFirstMod] Found type: Il2CppMenace.Tactical.TacticalManager
[12:16:36.153] [YourFirstMod] Found type: Il2CppMenace.Tactical.TacticalManagerInspector
```

That would do it. The correct name is `Il2CppMenace.Tactical.TacticalManger`, and not `Menace.Tactical.TacticalManager`. This also explains the SDK issue: it isn't a timing issue as previously hypothesized, but a name match.

The more experienced among you how made it this far may well have seen this coming, and to be honest, I did too. This first came up when I made `Let Me Finish` so I knew where this is going. But, I figured this as helpful for two reasons for new modders:

1. This is how a lot of debugging works. Find solution, build solution, modify solution, iterate until solution works. Just because the documentation for something says, "Do X", that doesn't always mean that is the way to do it. Often, solutions won't work right away, and you have to work through the problem to figure it out.
2. Runtime scans for names will save you a huge amount of tedium. Before building any mod, it is well worth your time to go off and properly identify what things are actually called by doing a runtime scan. Trust, but verify.

This also means that we probably don't need to have this patch applied during OnTacticalReady, since the issue isn't a timing one, but a naming issue. Let's do a refactor. Move everything into OnInitialize again, and wrap it in a try/catch block so that if something goes wrong, we don't crash the game:

```csharp
	public void OnInitialize(MelonLogger.Instance logger, HarmonyLib.Harmony harmony)
	{
		_log = logger;
		_harmony = harmony; // we will need this for later
		_log.Msg("YourFirstMod loaded.");

		try
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
	    var method = tacticalManager.GetMethod("InvokeOnAttackTileStart",
	        System.Reflection.BindingFlags.Instance |
	        System.Reflection.BindingFlags.Public |
	        System.Reflection.BindingFlags.NonPublic);

	    // If the method can't be found, return.
	    if (method == null)
	    {
	        _log.Error("InvokeOnAttackTileStart not found.");
	        return;
	    }

	    // The postfix patch
	    var postfix = typeof(Plugin).GetMethod(nameof(OnAttackTileStart_Postfix),
	        System.Reflection.BindingFlags.Static |
	        System.Reflection.BindingFlags.NonPublic);

	    _harmony.Patch(method, postfix: new HarmonyMethod(postfix));
	    _log.Msg("Patched InvokeOnAttackTileStart.");
	  	}

		catch (Exception ex)
		{
			_log.Error("Failed to patch InvokeOnAttackTileStart:");
			_log.Error(ex.ToString());
		}
	}
```

And reset OnUnload() to default since we don't have any more subscriptions:

```csharp
	public void OnUpdate() { }
	public void OnGUI() { }
	public void OnUnload() { }
```

```
[13:22:10.632] Failed to patch void Il2CppMenace.Tactical.TacticalManager::InvokeOnAttackTileStart(Il2CppMenace.Tactical.Actor _actor, Il2CppMenace.Tactical.Skills.Skill _skill, Il2CppMenace.Tactical.Tile _targetTile, float _attackDurationInSec): System.Exception: Parameter "attacker" not found in method void Il2CppMenace.Tactical.TacticalManager::InvokeOnAttackTileStart(Il2CppMenace.Tactical.Actor _actor, Il2CppMenace.Tactical.Skills.Skill _skill, Il2CppMenace.Tactical.Tile _targetTile, float _attackDurationInSec)
```

Getting somewhere! However, it does seem like we are back to the debugging phase.

## Debugging (Round 2)

Now that the patch is actually being tried, let's update those parameter names to match what the game is expecting:

```csharp
	// When an attack starts
	private static void OnAttackTileStart_Postfix(object _actor, object _targetTile)
	{
		_log.Msg("An attack has started.");

		// Wrap raw IntPtr values in GameObj before passing. We do this because
		// GetActorOnTile and CanActorSee both expect GameObj
		var tileObj = new GameObj((IntPtr)_targetTile);
		var attackerObj = new GameObj((IntPtr)_actor);

		// ... The rest of OnAttackTileStart_Postfix()
```

```
[13:29:40.814] [YourFirstMod] Patched InvokeOnAttackTileStart.
```

Yay!

```
[13:30:32.936] [Il2CppInterop] During invoking native->managed trampoline
System.InvalidCastException: Unable to cast object of type 'Il2CppMenace.Tactical.Tile' to type 'System.IntPtr'.
   at YourFirstMod.Plugin.OnAttackTileStart_Postfix(Object _actor, Object _targetTile)
   at DMD<Il2CppMenace.Tactical.TacticalManager::InvokeOnAttackTileStart>(TacticalManager this, Actor _actor, Skill _skill, Tile _targetTile, Single _attackDurationInSec)
   at (il2cpp -> managed) InvokeOnAttackTileStart(IntPtr , IntPtr , IntPtr , IntPtr , Single , Il2CppMethodInfo* )
```

Boo! Let's see how the SDK handles this issue. Got it. A `GetPointer` helper method. Let's borrow that.

```csharp
	// Helper
	private static IntPtr GetPointer(object obj)
	{
	    if (obj == null) return IntPtr.Zero;
	    if (obj is Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase il2cppObj)
	        return il2cppObj.Pointer;
	    return IntPtr.Zero;
	}

	// When an attack starts
	private static void OnAttackTileStart_Postfix(object _actor, object _targetTile)
	{
		_log.Msg("An attack has started.");

		// Wrap raw IntPtr values in GameObj before passing. We do this because
		// GetActorOnTile and CanActorSee both expect GameObj
		var tileObj = new GameObj(GetPointer(_targetTile));
		var attackerObj = new GameObj(GetPointer(_actor));

		// ... The rest of OnAttackTileStart_Postfix()
```

And that fixed it. Now, let's do some test runs.

1. An empty tile: `[14:01:12.110] [YourFirstMod] There was no target on that tile.`
2. An enemy we can see: `[14:01:15.767] [YourFirstMod] The attacker can see the target.`
3. An enemy that is hidden: crickets.

How much do you want to bet that `Intercept` has the same issue as `TacticalEventHooks`? Let's check. Sure does. Not a huge surprise. The good news is, now we know how to fix it. Let's declare the new patch target and clean the code up a bit so we can read it more easily:

```csharp
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
```

And add the patch logic:

```csharp
	private static void OnGetAccuracy_Postfix(object __instance, ref float __result)
	{
	   	if (_applyAccuracyDebuff)
	    {
	    	__result *= 0.8f;
	    	_applyAccuracyDebuff = false;
	    	_log.Msg("Accuracy debuff applied");
	    }
	}
```

Also, make sure to declare the new `_applyAccuracyDebuff = false;`

```csharp
	private static MelonLogger.Instance _log;
	private static HarmonyLib.Harmony _harmony;
	private static bool _applyAccuracyDebuff = false;
```

And finally, change the way the OnAttackTileStart_Postfix checks this `bool`:

```csharp
		// Can the attacker see the target?
		if (LineOfSight.CanActorSee(attackerObj, target))
		{
			_log.Msg("The attacker can see the target.");
			return;
		}

		// Intercept the attack accuracy
		_applyAccuracyDebuff = true;
	}
```

That was a lot, so let's review what we did:

1. We patched `OnGetAccuracy` directly with a postfix, bypassing the SDK. The patch we made checks if `_applyAccuracyDebuff` is true, and if it is, debuffs the accuracy, and then resets the `bool`. We also modified the `OnAttackTileStart_Postfix` to change `_applyAccuracyDebuff` to `true` in the case that there is a target on the tile and we can't see them. Finally, we declared `_applyAccuracyDebuff` at the top, so the plugin has a baseline of "don't modify accuracy".
2. We refactored the code now that we have two patches, so that it is all a bit more organized, with the two Patch methods as separate entities. This doesn't have to be done, but personally, as soon as I have more than one patch, I like to separate them out for readability.

Let's run a test and see:

```
[14:42:16.044] [YourFirstMod] An attack has started.
[14:42:16.045] [YourFirstMod] Accuracy debuff applied
```

Congratulations! It works. Well done. You have successfully made a mod which debuffs the accuracy of an attacker by 20% if it can't see the target.

"But what about indirect fire weapons?" You ask. I'll be honest, I was hoping you would forget. Okay, fine. The place to check is `IsLineOfFireNeeded` on the `SkillTemplate`. This is a `bool`. If true, our plugin needs to apply, if false, we can skip. Let's add that. Fortunately, OnAttackTileStart can already pass a `_skill` parameters. So:

```csharp
	// When an attack starts
	private static void OnAttackTileStart_Postfix(object _actor, object _targetTile, object _skill)
	{
		_log.Msg("An attack has started.");

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
		                _log.Msg("Skill does not require line of fire, skipping debuff.");
		                return;
		            }
		        }
		    }
		}

	// ... The rest of OnAttackTileStart_Postfix()
```

From the `_skill` parameter, we pull the template. Then, on the template we use reflection to check the template for `IsLineOfFireNeeded`. If Line of Fire is not needed, we print a debug line and return out of the function. See why I wanted to skip this part? It isn't pretty.

```
[15:24:42.881] [YourFirstMod] An attack has started.
[15:24:42.881] [YourFirstMod] Skill does not require line of fire, skipping debuff.
```

Brilliant. One final run to make sure we haven't broken functionality in an unexpected way. Everything checks. We are almost done. Time for final clean up. We probably don't want users to get spammed with development log lines when they use our mod. There are two options:

1. Remove them.
2. Leave them but put them behind a debugLogging gate.

I typically go for option 2, as it helps me check if I break things when I add features later, or if the game code changes. To accomplish this, we are going to add a new `bool`:

```csharp
	private static MelonLogger.Instance _log;
	private static HarmonyLib.Harmony _harmony;
	private static bool _applyAccuracyDebuff = false;
	private static bool _debugLogging = false;
```

And then on every message line we wish to suppress `if (_debugLogging) _log.Msg("some message");`. I will leave some message unsuppressed though: Errors are useful to always show, and the `"Patched ..."` lines are nice for users to see so they can verify that the mod is working.

All that's left is to `Export Modpack` and share it with others!

## Closing thoughts

By getting to this part of the tutorial, you have learned how to:

1. Set up a new mod for the first time.
2. Plan a mod to solve a problem.
3. Research the required tools.
4. Implement your plan.
5. Debug when the implementation doesn't work right away.
6. Diagnose and work through documentation discrepancies.
7. Debug some more.
8. And finally, to be a good steward, leaving behind clean log lines for everyone who follows.

Coding is an iterative process, and it will only get easier with time and practice. Each problem solved is a future problem identified. Each solution is a tool you can apply to a future problem. So, go forth, and create some cool stuff.