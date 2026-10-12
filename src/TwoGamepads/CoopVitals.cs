using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;
public static class CoopVitals {
 public static readonly CoopLifeRules Life=new CoopLifeRules();
 public static bool Ready;static int Slot,Transactions;static bool AllowDefeat;static ManualLogSource Log;static PropertyInfo Context;static MethodInfo CancelSecond;
 public static readonly float[] RescueProgress={0,0},ImmuneUntil={0,0};
 public static bool Active {get{return Ready&&CoopMode.Enabled&&SceneMgr.GetCurScene()==GameScene.kGame;}}
 sealed class DamageScope {public int Owner,PreviousSlot;public float Before;public bool Closed;public void Close(){if(!Closed){Closed=true;Transactions--;Slot=PreviousSlot;}}}
 static bool WritingBacking;
 static readonly System.Collections.Generic.Dictionary<IntPtr,bool> RendererStates=new System.Collections.Generic.Dictionary<IntPtr,bool>();
 public static bool Down(int slot){return Active&&Life.Down(slot);}
 public static int Owner(Player player){var second=CoopCombat.PlayerAt(1);return second!=null&&player!=null&&player.Pointer==second.Pointer?1:0;}
 public static void Install(Harmony h,Type controller,ManualLogSource log){
  Log=log;var context=controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerContext");Context=AccessTools.Property(context,"IsPlayerTwoContext");
  CancelSecond=AccessTools.Method(controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerPatches"),"CancelActivePlayerTwoCoroutine");
  h.Patch(AccessTools.Method(controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerPatches"),"P2RunGameOverPrefix"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(LegacyDeath)));
  h.Patch(AccessTools.Method(typeof(GameMgr),"Start"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(NewRound)));
  foreach(string method in new[]{"EnterPlayerTwoContext","EnterNearestPlayerContext","ExitContext","RestoreOriginalSingleton","RestoreOriginalPlayerContext"})h.Patch(AccessTools.Method(context,method),postfix:new HarmonyMethod(typeof(CoopVitals),nameof(Sync)));
  h.Patch(AccessTools.Method(typeof(Player),"Damage"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(BeforeDamage)){priority=Priority.First},postfix:new HarmonyMethod(typeof(CoopVitals),nameof(AfterDamage)){priority=Priority.Last},finalizer:new HarmonyMethod(typeof(CoopVitals),nameof(FinishDamage)));
  h.Patch(AccessTools.Method(typeof(Player),"Heal"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(Heal)));
  h.Patch(AccessTools.PropertySetter(typeof(BattleSaveData),"CurHealth"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(HealthWritten)));
  h.Patch(AccessTools.Method(typeof(Player),"RunGameOver"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(PlayerDeath)));
  h.Patch(AccessTools.Method(typeof(Player),"MyUpdate"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(PlayerUpdate)));
  h.Patch(AccessTools.Method(typeof(Player),"FixedUpdate"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(PlayerFixed)));
  h.Patch(AccessTools.Method(typeof(Player),"CanBeDamaged"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(CanDamage)));
  h.Patch(AccessTools.Method(typeof(GameMgr),"LoseGame"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(TeamDeath)));
  h.Patch(AccessTools.Method(typeof(BallMgr),"RunBalls"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(CanShoot)));
  h.Patch(AccessTools.Method(typeof(BallMgr),"MoveBalls"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(MoveWorld)));
  foreach(var type in typeof(BallMgr).GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic))if(type.Name.StartsWith("__RunBalls_d__")){h.Patch(AccessTools.Method(type,"MoveNext"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(BallStep)){priority=Priority.First});h.Patch(AccessTools.Method(type,"MoveNext"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(FinishVolley)){priority=Priority.Last});}
  h.Patch(AccessTools.Method(typeof(GameMgr),"MarkLevelComplete"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(CanWin)));
  h.Patch(AccessTools.Method(typeof(InGameUI),"RefreshHealthBar"),prefix:new HarmonyMethod(typeof(CoopVitals),nameof(HealthBar)));
 }
 static void NewRound(){Ready=false;Slot=Transactions=0;RendererStates.Clear();AllowDefeat=false;RescueProgress[0]=RescueProgress[1]=ImmuneUntil[0]=ImmuneUntil[1]=0;CoopRoundStats.Reset();}
 static void Backing(){if(BattleSaveData.I==null)return;WritingBacking=true;try{BattleSaveData.I.CurHealth=Life.Down(Slot)?1:Life.Health[Slot];}finally{WritingBacking=false;}}
 static void Capture(){}
 static void HealthWritten(ref float __0){if(!Active||WritingBacking||Transactions>0)return;int owner=(bool)Context.GetValue(null)?1:0;if(!Life.Down(owner))Life.Health[owner]=Mathf.Clamp(__0,0,Life.Maximum[owner]);__0=Life.Down(owner)?1:Life.Health[owner];}
 static void Sync(){if(!Active)return;if(Transactions==0){Slot=(bool)Context.GetValue(null)?1:0;Backing();}}
 static bool BeforeDamage(Player __instance,ref EnemyAttackResult __result,float __0,PieceDmgType __1,out DamageScope __state){__state=null;if(!Active)return true;int owner=Owner(__instance);if(CoopCombat.SimulationOwner==1&&owner==0){EnemyAttackResult routed=default;CoopCharacters.ForSecond(()=>routed=CoopCombat.PlayerAt(1).Damage(__0,__1));__result=routed;return false;}if(Life.Down(owner))return false;__state=new DamageScope{Owner=owner,PreviousSlot=Slot,Before=Life.Health[owner]};Transactions++;Slot=owner;BattleSaveData.I.CurHealth=Life.Health[owner];return true;}
 static void AfterDamage(DamageScope __state){if(__state==null)return;float health=Mathf.Clamp(BattleSaveData.I.CurHealth,0,Life.Maximum[__state.Owner]);Life.Health[__state.Owner]=health;__state.Close();if(health<__state.Before){RescueProgress[__state.Owner]=0;TwoGamepads.VibratePad(__state.Owner,0.65f,0.2f);}if(health<=0&&__state.Before>0){if(__state.Owner==1)CancelSecond?.Invoke(null,null);Log.LogInfo("Individual player down: P"+(__state.Owner+1)+"; rescue used="+Life.Rescued[__state.Owner]);}Backing();}
 static void FinishDamage(DamageScope __state){if(__state!=null&&!__state.Closed){__state.Close();Backing();}}
 static bool Heal(Player __instance,float __0){if(CoopMode.Enabled&&CoopMode.Preparing)return false;if(!Active)return true;int owner=Owner(__instance);Life.SetMaximum(owner,UpgradeMgr.I.MaxHealth);Life.Heal(owner,__0);Backing();return false;}
 public static void RefreshStats(){if(!Active||UpgradeMgr.I==null)return;int owner=(bool)Context.GetValue(null)?1:0;Life.SetMaximum(owner,UpgradeMgr.I.MaxHealth);Backing();}
 static bool CanDamage(Player __instance,ref bool __result){if(!Active)return true;int owner=Owner(__instance);if(!Life.Down(owner)&&Time.unscaledTime>=ImmuneUntil[owner])return true;__result=false;return false;}
 static bool PlayerUpdate(Player __instance){return !Active||!Life.Down(Owner(__instance));}
 static bool PlayerFixed(Player __instance){return !Active||!Life.Down(Owner(__instance));}
 static bool PlayerDeath(){return !Active;}
 static bool LegacyDeath(ref bool __result){if(!Active)return true;__result=false;return false;}
 static bool TeamDeath(){return !Active||AllowDefeat||Life.Defeated;}
 static bool CanShoot(){return !Active||!Life.Down(Owner(Player.I));}
 static bool CanWin(){return !Active||!Life.Defeated;}
 static bool MoveWorld(BallMgr __instance){return !CoopMode.Enabled||(__instance!=null&&BallMgr.I!=null&&__instance.Pointer==BallMgr.I.Pointer&&SceneMgr.GetCurScene()==GameScene.kGame&&!CoopRoundStats.Complete);}
 static bool BallStep(ref bool __result){if(!CoopMode.Enabled||(SceneMgr.GetCurScene()==GameScene.kGame&&!CoopRoundStats.Complete))return true;__result=false;return false;}
 static bool FinishVolley(object __instance,ref bool __result){
  if(!CoopMode.Enabled||__instance==null||BallMgr.I==null)return true;
  var type=__instance.GetType();int state=(int)AccessTools.Property(type,"__1__state").GetValue(__instance);
  if(state!=1)return true;
  int ball=(int)AccessTools.Property(type,"_ballIdx_5__4").GetValue(__instance),maxBall=(int)AccessTools.Property(type,"_maxBallIdx_5__6").GetValue(__instance);
  int follower=(int)AccessTools.Property(type,"_followerIdx_5__5").GetValue(__instance),maxFollower=(int)AccessTools.Property(type,"_maxFollowerIdx_5__7").GetValue(__instance);
  if(ball<maxBall||follower<maxFollower)return true;
  // The launch is exhausted. Close its iterator before native code can spin
  // on a completed volley after a per-player runtime swap or global recall.
  AccessTools.Property(type,"__1__state").SetValue(__instance,-1);
  BallMgr.I._allowAutoChange=true;BallMgr.I._isShootInterrupted=false;BallMgr.I.LastShootCompleteTime=TimeMgr.I._physicsTime;BallMgr.I._lastShootCompleteUnityTime=Time.time;
  if(BallMgr.I.GetBallState()==BallState.kLaunching)BallMgr.I.SetBallState(BallState.kBouncing);
  __result=false;return false;
 }
 public static void StopShots(){CancelSecond?.Invoke(null,null);if(BallMgr.I!=null)MEC.Timing.KillCoroutines(BallMgr.I._ballAnim);}
 static bool HealthBar(){return !CoopMode.Enabled||!Ready;}
 public static bool Rescue(int donor){if(!Active||!Life.Rescue(donor))return false;RescueProgress[donor]=0;ImmuneUntil[1-donor]=Time.unscaledTime+1.5f;var target=CoopCombat.PlayerAt(1-donor);if(target!=null)target.IsRunningLevelUp=false;Backing();TwoGamepads.VibratePad(donor,0.35f,0.25f);TwoGamepads.VibratePad(1-donor,0.65f,0.35f);Log.LogInfo("P"+(donor+1)+" rescued P"+(2-donor)+"; paid exactly half current health.");return true;}
 public static void AutoRescue(){if(!Active||GameMgr.I==null||GameMgr.I.CurState!=GameState.kPlaying)return;for(int donor=0;donor<2;donor++)if(Life.CanRescue(donor))Rescue(donor);}
 static void DeadVisuals(){for(int slot=0;slot<2;slot++){var player=CoopCombat.PlayerAt(slot);if(player==null)continue;var renderers=player.GetComponentsInChildren(Il2CppInterop.Runtime.Il2CppType.Of<Renderer>(),true);for(int i=0;i<renderers.Length;i++){var renderer=renderers[i].Cast<Renderer>();if(Life.Down(slot)){if(!RendererStates.ContainsKey(renderer.Pointer))RendererStates[renderer.Pointer]=renderer.enabled;renderer.enabled=false;}else{bool was;if(RendererStates.TryGetValue(renderer.Pointer,out was)){renderer.enabled=was;RendererStates.Remove(renderer.Pointer);}}}}}
 public static void Tick(){
  if(!CoopMode.Enabled||SceneMgr.GetCurScene()!=GameScene.kGame||GameMgr.I==null||CoopMode.Preparing||!CoopCharacters.IndependentReady)return;
  if(!Ready){float first=UpgradeMgr.I.MaxHealth,second=first;CoopCharacters.ForSecond(()=>second=UpgradeMgr.I.MaxHealth);Life.Reset(first,second);Slot=0;Ready=true;BattleSaveData.I.NumRevives=0;Backing();Log.LogInfo("Individual health initialized: "+first+" / "+second);}
  if(GameMgr.I.CurState==GameState.kPlaying){Capture();float first=UpgradeMgr.I.MaxHealth,second=first;CoopCharacters.ForSecond(()=>second=UpgradeMgr.I.MaxHealth);Life.SetMaximum(0,first);Life.SetMaximum(1,second);Backing();}
  BattleSaveData.I.NumRevives=0;
  DeadVisuals();
  if(!CoopTestHarness.Active&&GameMgr.I.CurState==GameState.kPlaying){AutoRescue();DeadVisuals();}
  if(Life.Defeated&&!CoopRoundStats.Complete&&GameMgr.I.CurState==GameState.kPlaying){AllowDefeat=true;BattleSaveData.I.CurHealth=0;try{GameMgr.I.LoseGame();}finally{AllowDefeat=false;}return;}
  if(CoopTestHarness.Active||!Application.isFocused||GameMgr.I.CurState!=GameState.kPlaying)return;
  // Rescue is automatic; the original one-rescue-per-player cost remains.
 }
}




