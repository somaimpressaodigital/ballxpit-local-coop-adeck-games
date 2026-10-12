using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx.Logging;
public static class CoopCompatibility {
 static MethodInfo OldMaintenance,Filter,Synchronize,Enter,Exit;
 static FieldInfo Running,Recall,GameOver;
 static ManualLogSource Log;
 static bool Warned,Logged;
 public static void Install(Harmony h,Type controller,ManualLogSource log){
  Log=log;
  h.Patch(AccessTools.Method(typeof(BaseGridMgr),"FindEmptySpots"),prefix:new HarmonyMethod(typeof(CoopCompatibility),nameof(CityPlacement)));
  h.Patch(AccessTools.Method(typeof(BaseMgr),"Start"),prefix:new HarmonyMethod(typeof(CoopCompatibility),nameof(CityPlacement)));
  h.Patch(AccessTools.Method(typeof(UIUtl.__FadeGroup_d__29),"MoveNext"),prefix:new HarmonyMethod(typeof(CoopCompatibility),nameof(FadeAlive)));
  h.Patch(AccessTools.Method(typeof(InGameUI.__RunLevelCompleteBonus_d__123),"MoveNext"),prefix:new HarmonyMethod(typeof(CoopCompatibility),nameof(BonusAlive)));
  h.Patch(AccessTools.Method(typeof(Player.__RunShieldBounce_d__115),"MoveNext"),prefix:new HarmonyMethod(typeof(CoopCompatibility),nameof(ShieldAlive)));
  if(AccessTools.Property(typeof(BallMgr),"AdultSideBalls")!=null)return;
  var patches=controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerPatches",true);
  var context=controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerContext",true);
  OldMaintenance=AccessTools.Method(patches,"RunPlayerTwoBallMgrMaintenance");
  Filter=AccessTools.Method(patches,"FilterPlayerTwoBalls");
  Synchronize=AccessTools.Method(patches,"SynchronizePlayerTwoEarlyReturnedFollowerCount");
  Running=AccessTools.Field(patches,"_runningPlayerTwoBallMgrMaintenance");
  Recall=AccessTools.Field(patches,"_globalRecallActive");GameOver=AccessTools.Field(patches,"_playerOneGameOverActive");
  Enter=AccessTools.Method(context,"EnterPlayerTwoContext");Exit=AccessTools.Method(context,"ExitContext");
  h.Patch(AccessTools.Method(controller,"LateUpdate"),transpiler:new HarmonyMethod(typeof(CoopCompatibility),nameof(ReplaceMaintenance)));
  Log.LogInfo("Compatibility enabled: this game version has no separate AdultSideBalls collection.");
 }
 static bool CityPlacement(){return !CoopMode.Enabled;}
 static bool FadeAlive(UIUtl.__FadeGroup_d__29 __instance,ref bool __result){if(!CoopMode.Enabled||__instance.grp!=null)return true;__result=false;return false;}
 static bool BonusAlive(InGameUI.__RunLevelCompleteBonus_d__123 __instance,ref bool __result){if(!CoopMode.Enabled||__instance.__4__this!=null)return true;__result=false;return false;}
 static bool ShieldAlive(Player.__RunShieldBounce_d__115 __instance,ref bool __result){if(!CoopMode.Enabled)return true;var player=__instance.__4__this;if(player!=null&&SceneMgr.GetCurScene()==GameScene.kGame&&!CoopRoundStats.Complete)return true;__result=false;return false;}
 static IEnumerable<CodeInstruction> ReplaceMaintenance(IEnumerable<CodeInstruction> instructions){
  foreach(var i in instructions){if(i.Calls(OldMaintenance))i.operand=AccessTools.Method(typeof(CoopCompatibility),nameof(Maintain));yield return i;}
 }
 static void Maintain(BallMgr manager,Player player){
  if(manager==null||player==null||(bool)Running.GetValue(null)||(bool)Recall.GetValue(null)||(bool)GameOver.GetValue(null))return;
  if(GameMgr.I!=null&&GameMgr.I.CurState!=GameState.kPlaying)return;
  var active=manager.ActiveBalls;var sides=manager.SideBalls;object previous=null;
  try{
   var ownActive=(Il2CppSystem.Collections.Generic.List<BallObj>)Filter.Invoke(null,new object[]{active});
   var ownSides=(Il2CppSystem.Collections.Generic.List<BallObj>)Filter.Invoke(null,new object[]{sides});
   previous=Enter.Invoke(null,new object[]{player});
   manager.ActiveBalls=ownActive;manager.SideBalls=ownSides;
   Synchronize.Invoke(null,new object[]{manager});Running.SetValue(null,true);
   manager.MyUpdate();Synchronize.Invoke(null,new object[]{manager});
   if(!Logged){Log.LogInfo("Compatible P2 maintenance completed without moving world balls twice.");Logged=true;}
  }catch(Exception e){if(!Warned){Log.LogError("P2 compatibility maintenance failed: "+e);Warned=true;}}
  finally{Running.SetValue(null,false);manager.ActiveBalls=active;manager.SideBalls=sides;if(previous!=null)Exit.Invoke(null,new[]{previous});}
 }
}

