using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
public static class CoopSession {
 static FieldInfo Instance;static MethodInfo Reset;static FieldInfo Intro;
 public static void Install(Harmony harmony,Type controller){
  Instance=AccessTools.Field(controller,"_instance");Reset=AccessTools.Method(controller,"ResetPrototype");Intro=AccessTools.Field(controller,"_isFollowingIntro");
  foreach(string method in new[]{"Update","LateUpdate","OnGUI","RefreshBinding","SpawnNativePlayerTwo"})harmony.Patch(AccessTools.Method(controller,method),prefix:new HarmonyMethod(typeof(CoopSession),nameof(AllowRuntime)){priority=Priority.First});
  harmony.Patch(AccessTools.Method(controller,"UpdateIntroFollow"),prefix:new HarmonyMethod(typeof(CoopSession),nameof(FollowIntro)));
 }
 static bool AllowRuntime(){return CoopMode.Enabled&&SceneMgr.I!=null&&CoopSessionRules.AllowSecondPlayer(true,SceneMgr.GetCurScene()==GameScene.kGame);}
 static bool FollowIntro(object __instance){if(!AllowRuntime())return false;if(GameMgr.I!=null&&GameMgr.I.CurState==GameState.kPlaying){Intro.SetValue(__instance,false);return false;}return true;}
 public static void ResetRound(){
  // Restore and remove native P2 before discarding its character/inventory snapshot.
  var controller=Instance?.GetValue(null);if(controller!=null)Reset.Invoke(controller,null);
  CoopCharacters.ResetSession();CoopVitals.Ready=false;CoopAmmoReload.Reset();CoopCombat.SimulationOwner=-1;TwoGamepads.ResetInputSession();
 }
}
