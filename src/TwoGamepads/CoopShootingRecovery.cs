using System;
using System.Reflection;
using HarmonyLib;
public static class CoopShootingRecovery {
 sealed class ReadyScope {public BallMgr Manager;public Il2CppSystem.Collections.Generic.List<BallObj> Active,Sides;}
 sealed class VolleyScope{public int Owner;public object Previous;public bool Scoped;}
 static FieldInfo SecondIterators;static MethodInfo Enter,Exit;
 public static void Install(Harmony h){
  h.Patch(AccessTools.Method(typeof(BallMgr),"RunBalls"),prefix:new HarmonyMethod(typeof(CoopShootingRecovery),nameof(WaitPrimaryVolley)){priority=Priority.High});
  var asm=typeof(BALLxPITLocalCoop.Plugin).Assembly;var native=asm.GetType("BALLxPITLocalCoop.NativePlayerPatches");var context=asm.GetType("BALLxPITLocalCoop.NativePlayerContext");SecondIterators=AccessTools.Field(native,"_playerTwoRunBallsIterators");Enter=AccessTools.Method(context,"EnterPlayerTwoContext");Exit=AccessTools.Method(context,"ExitContext");h.Patch(AccessTools.Method(native,"StartPlayerTwoRunBalls"),prefix:new HarmonyMethod(typeof(CoopShootingRecovery),nameof(WaitSecondVolley)){priority=Priority.High});
  h.Patch(AccessTools.Method(typeof(BallMgr),"IsReadyToShoot"),prefix:new HarmonyMethod(typeof(CoopShootingRecovery),nameof(Ready)){priority=Priority.First},finalizer:new HarmonyMethod(typeof(CoopShootingRecovery),nameof(Restore)));
  foreach(var type in typeof(BallMgr).GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic))if(type.Name.StartsWith("__RunBalls_d__"))h.Patch(AccessTools.Method(type,"MoveNext"),postfix:new HarmonyMethod(typeof(CoopShootingRecovery),nameof(Completed)){priority=Priority.First});
 }
 static bool FullVolleyReady(){if(BattleSaveData.I.CurChar.HasProp(CharProp.kAlwaysShoot)||UpgradeMgr.I.NumFollowers<=1)return true;return BallMgr.I.GetNumReadyFollowers()>=UpgradeMgr.I.NumFollowers;}
 static bool WaitPrimaryVolley(){return !CoopMode.Enabled||!CoopCharacters.IndependentReady||Player.I!=CoopCombat.PlayerAt(0)||FullVolleyReady();}
 static bool WaitSecondVolley(ref bool __result,ref string __2){if(!CoopMode.Enabled||!CoopCharacters.IndependentReady)return true;bool ready=false;CoopCharacters.ForSecond(()=>ready=FullVolleyReady());if(ready)return true;__result=false;__2="waiting for the complete native common-ball reload";return false;}
 public static void Tick(){
  if(CoopTestHarness.Active&&!CoopTestHarness.PrimaryFireTestEnabled)return;
  if(!CoopMode.Enabled||!CoopVitals.Active||CoopVitals.Down(0)||CoopRoundStats.Complete||Player.I!=CoopCombat.PlayerAt(0)||GameMgr.I==null||GameMgr.I.CurState!=GameState.kPlaying||BallMgr.I==null)return;
  if(!CoopTestHarness.Active&&!UnityEngine.Application.isFocused)return;
  bool shoot=TwoGamepads.Automatic(0);var pad=TwoGamepads.GetPad(0);
  if(!CoopTestHarness.Active&&pad!=null)shoot|=pad.rightTrigger.value>0.25f||pad.rightBumper.value||pad.a.value;
  if(shoot&&BallMgr.I.GetBallState()!=BallState.kLaunching&&BallMgr.I.IsReadyToShoot())BallMgr.I.RunBalls();
 }
 static Il2CppSystem.Collections.Generic.List<BallObj> Primary(Il2CppSystem.Collections.Generic.List<BallObj> balls){var result=new Il2CppSystem.Collections.Generic.List<BallObj>();if(balls!=null)foreach(var ball in balls)if(ball!=null&&CoopRoundStats.BallOwner(ball)!=1)result.Add(ball);return result;}
 static void Ready(BallMgr __instance,out ReadyScope __state){
  __state=null;if(!CoopMode.Enabled||!CoopCharacters.IndependentReady||Player.I!=CoopCombat.PlayerAt(0)||__instance==null)return;
  __state=new ReadyScope{Manager=__instance,Active=__instance.ActiveBalls,Sides=__instance.SideBalls};
  __instance.ActiveBalls=Primary(__state.Active);__instance.SideBalls=Primary(__state.Sides);
  if(__instance._numActiveFollowers<0)__instance._numActiveFollowers=0;
  if(__instance.GetBallState()==BallState.kBouncing&&__instance.ActiveBalls.Count==0&&__instance._numActiveFollowers==0){__instance._curBallIdx=0;__instance.SetBallState(BallState.kIdle);}
 }
 static void PrimaryHeld(GameActionType __0,ref bool __result){
  if(__0!=GameActionType.kShoot||!CoopMode.Enabled||!CoopVitals.Active||CoopVitals.Down(0)||CoopRoundStats.Complete||Player.I!=CoopCombat.PlayerAt(0)||GameMgr.I==null||GameMgr.I.CurState!=GameState.kPlaying)return;
  if(!CoopTestHarness.Active&&!UnityEngine.Application.isFocused)return;
  if(TwoGamepads.Automatic(0)){__result=true;return;}
  if(CoopTestHarness.Active)return;
  var pad=TwoGamepads.GetPad(0);if(pad!=null&& (pad.rightTrigger.value>0.25f||pad.rightBumper.value||pad.a.value))__result=true;
 }
 static void Restore(ReadyScope __state){if(__state==null)return;__state.Manager.ActiveBalls=__state.Active;__state.Manager.SideBalls=__state.Sides;}
 static void EnterVolley(object __instance,out VolleyScope __state){__state=null;if(!CoopMode.Enabled||!CoopCharacters.IndependentReady||SceneMgr.GetCurScene()!=GameScene.kGame)return;var obj=__instance as Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase;if(obj==null)return;var ids=SecondIterators.GetValue(null) as System.Collections.Generic.HashSet<System.IntPtr>;int owner=ids!=null&&ids.Contains(obj.Pointer)?1:0;__state=new VolleyScope{Owner=owner,Previous=Player.I};if(owner==1){__state.Previous=Enter.Invoke(null,new object[]{CoopCombat.PlayerAt(1)});__state.Scoped=true;}else if(Player.I==CoopCombat.PlayerAt(1)){AccessTools.Method(typeof(BALLxPITLocalCoop.Plugin).Assembly.GetType("BALLxPITLocalCoop.NativePlayerContext"),"RestoreOriginalPlayerContext").Invoke(null,null);__state.Scoped=true;}}
 static void ExitVolley(VolleyScope __state){if(__state!=null&&__state.Scoped)Exit.Invoke(null,new[]{__state.Previous});}
 static void Completed(bool __result){if(__result||!CoopMode.Enabled||BallMgr.I==null||SceneMgr.GetCurScene()!=GameScene.kGame)return;ReleaseLaunch();}
 static void ReleaseLaunch(){if(BallMgr.I.GetBallState()!=BallState.kLaunching)return;BallMgr.I._allowAutoChange=true;BallMgr.I._isShootInterrupted=false;BallMgr.I.SetBallState(BallState.kBouncing);}
}




