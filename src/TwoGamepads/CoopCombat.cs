using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime;
using BepInEx.Logging;
public static class CoopCombat {
 static FieldInfo Instance,First,Second;
 static MethodInfo Position;
 static MethodInfo Enter,Exit;
 static ManualLogSource Log;
 static bool Creating;
 static int CreationDepth;
 sealed class CreationScope{public bool Outer,Closed;public void Close(){if(!Closed){Closed=true;CreationDepth--;}}}
 static readonly Dictionary<IntPtr,bool> BossTargets=new Dictionary<IntPtr,bool>();
 static readonly HashSet<IntPtr> SecondHoming=new HashSet<IntPtr>();
 static ArrowObj LastSecondProjectile;
 sealed class ArrowStep{public Vector3 Position;public object Previous;public bool Scoped;public void Restore(){if(Scoped){Scoped=false;Exit.Invoke(null,new[]{Previous});}}}
 static Shapes.Disc Marker;
 static float PauseChanged;
 public static int Duplicated,SecondHits;
 public static int SimulationOwner=-1;
 sealed class PlayerScope{public int Previous;}
 sealed class MeleeScope{public object Previous;public bool Scoped;}
 static MethodInfo Restore;
 public static Player PlayerAt(int slot){var c=Instance?.GetValue(null);return c==null?null:(slot==0?First:Second).GetValue(c) as Player;}
 public static Vector3 Pos(Player player){return (Vector3)Position.Invoke(null,new object[]{player});}
 public static void Install(Harmony h,Type controller,ManualLogSource log){
  Log=log;Instance=AccessTools.Field(controller,"_instance");First=AccessTools.Field(controller,"_playerOne");Second=AccessTools.Field(controller,"_playerTwo");
  var context=controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerContext");Position=AccessTools.Method(context,"GetPlayerPosition");Enter=AccessTools.Method(context,"EnterPlayerTwoContext");Exit=AccessTools.Method(context,"ExitContext");
  Restore=AccessTools.Method(context,"RestoreOriginalPlayerContext");
  h.Patch(AccessTools.Method(context,"EnterNearestPlayerContext"),prefix:new HarmonyMethod(typeof(CoopCombat),nameof(LivingTarget)));
  h.Patch(AccessTools.Method(typeof(GridPieceObj),"OnAttackPlayer"),prefix:new HarmonyMethod(typeof(CoopCombat),nameof(Melee)),finalizer:new HarmonyMethod(typeof(CoopCombat),nameof(EndMelee)));
  h.Patch(AccessTools.Method(typeof(Player),"MyUpdate"),prefix:new HarmonyMethod(typeof(CoopCombat),nameof(Simulate)){priority=Priority.Last},finalizer:new HarmonyMethod(typeof(CoopCombat),nameof(EndSimulation)));
  foreach(var m in typeof(PoolMgr).GetMethods())if(m.Name=="CreateArrow")h.Patch(m,prefix:new HarmonyMethod(typeof(CoopCombat),nameof(BeginCreation)),postfix:new HarmonyMethod(typeof(CoopCombat),nameof(ArrowCreated)),finalizer:new HarmonyMethod(typeof(CoopCombat),nameof(EndCreation)));
  h.Patch(AccessTools.Method(typeof(ArrowObj),"MyFixedUpdate"),prefix:new HarmonyMethod(typeof(CoopCombat),nameof(ArrowBefore)),postfix:new HarmonyMethod(typeof(CoopCombat),nameof(ArrowUpdated)),finalizer:new HarmonyMethod(typeof(CoopCombat),nameof(ArrowFinished)));
  h.Patch(AccessTools.Method(typeof(GameMgr),"Start"),postfix:new HarmonyMethod(typeof(CoopCombat),nameof(NewBattle)));
  h.Patch(AccessTools.Method(typeof(GameMgr),"SetState"),prefix:new HarmonyMethod(typeof(CoopCombat),nameof(AllowState)),postfix:new HarmonyMethod(typeof(CoopCombat),nameof(StateChanged)));
  h.Patch(AccessTools.Method(typeof(InputMgr),"Vibrate",new[]{typeof(float),typeof(float)}),prefix:new HarmonyMethod(typeof(CoopCombat),nameof(Vibrate)));
  h.Patch(AccessTools.Method(typeof(BallMgr),"RecallBalls"),prefix:new HarmonyMethod(typeof(CoopCombat),nameof(LiveRecall)));
 }
 public static Player NearestLiving(Vector3 origin){var a=PlayerAt(0);var b=PlayerAt(1);if(a==null||CoopVitals.Down(0))return b!=null&&!CoopVitals.Down(1)?b:null;if(b==null||CoopVitals.Down(1))return a;return ((Vector2)(Pos(a)-origin)).sqrMagnitude<=((Vector2)(Pos(b)-origin)).sqrMagnitude?a:b;}
 static bool LivingTarget(ref Player __result){if(!CoopVitals.Active||(!CoopVitals.Down(0)&&!CoopVitals.Down(1)))return true;var previous=Player.I;if(CoopVitals.Down(0)&&!CoopVitals.Down(1))Enter.Invoke(null,new object[]{PlayerAt(1)});else Restore.Invoke(null,null);__result=previous;return false;}
 static bool Melee(GridPieceObj __instance,out MeleeScope __state){__state=null;if(!CoopVitals.Active||__instance.Inst==null)return true;var target=NearestLiving(__instance.GetWorldCenterPos());if(target==null)return false;__state=new MeleeScope{Previous=Player.I,Scoped=true};if(CoopVitals.Owner(target)==1)Enter.Invoke(null,new object[]{target});else Restore.Invoke(null,null);return true;}
 static void EndMelee(MeleeScope __state){if(__state!=null&&__state.Scoped)Exit.Invoke(null,new[]{__state.Previous});}
 static void Simulate(Player __instance,out PlayerScope __state){__state=new PlayerScope{Previous=SimulationOwner};if(CoopVitals.Active)SimulationOwner=CoopVitals.Owner(__instance);}
 static void EndSimulation(PlayerScope __state){if(__state!=null)SimulationOwner=__state.Previous;}
 static void BeginCreation(out CreationScope __state){__state=new CreationScope{Outer=CreationDepth++==0};}
 static void EndCreation(CreationScope __state){__state?.Close();}
 static void ArrowCreated(PoolMgr __instance,ArrowObj __result,ArrowType __0,GridPieceInst __1,Vector2 __2,CreationScope __state){
  __state.Close();if(__result!=null)SecondHoming.Remove(__result.Pointer);if(!__state.Outer||Creating||!CoopMode.Enabled||__result==null||__1==null||__result.IsFriendly()||!CoopCharacters.IndependentReady)return;
  if(__1.Type.ToString().StartsWith("kAlly")||__1.Type.ToString()=="kGemspring"||__1.Type.ToString()=="kDefault")return;
  var p1=PlayerAt(0);var p2=PlayerAt(1);if(p1==null||p2==null)return;
  var origin=__result.transform.position;
  Vector2 toward1=((Vector2)(Pos(p1)-origin)).normalized,toward2=((Vector2)(Pos(p2)-origin)).normalized;
  if(toward2.sqrMagnitude<0.01f)return;
  if(CoopVitals.Down(0)){if(!CoopVitals.Down(1)){__result.SetAimDir(toward2);TrackSecond(__result);}return;}
  if(CoopVitals.Down(1)){if(Vector2.Dot(__2.normalized,toward2)>0.96f)__result.SetAimDir(toward1);return;}
  float offset=Vector2.SignedAngle(toward1,__2.normalized);
  // Only aimed boss shots switch targets; fixed radial patterns retain their shape and density.
  if(StatUtl.IsBoss(__1.Type)){
   if(Mathf.Abs(offset)>15)return;
   bool targetSecond;BossTargets.TryGetValue(__1.Pointer,out targetSecond);BossTargets[__1.Pointer]=!targetSecond;
   if(targetSecond){__result.SetAimDir(toward2);TrackSecond(__result);}return;
  }
  float angle=(Mathf.Atan2(toward2.y,toward2.x)*Mathf.Rad2Deg+offset)*Mathf.Deg2Rad;
  Creating=true;try{var second=__instance.CreateArrow(__0,__1,new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),origin);TrackSecond(second);Duplicated++;}finally{Creating=false;}
 }
 static void TrackSecond(ArrowObj arrow){LastSecondProjectile=arrow;if(arrow!=null&&(arrow.Type==ArrowType.kHoming||arrow.Type==ArrowType.kHomingRock))SecondHoming.Add(arrow.Pointer);}
 static void NewBattle(){BossTargets.Clear();SecondHoming.Clear();LastSecondProjectile=null;CoopBossMechanics.Reset();}
 static void ArrowBefore(ArrowObj __instance,out ArrowStep __state){__state=new ArrowStep{Position=__instance.transform.position};if(CoopVitals.Down(1))SecondHoming.Remove(__instance.Pointer);if(CoopMode.Enabled&&CoopCharacters.IndependentReady&&(SecondHoming.Contains(__instance.Pointer)||(CoopVitals.Down(0)&&!CoopVitals.Down(1)))&&!__instance.IsFriendly()){__state.Previous=Enter.Invoke(null,new object[]{PlayerAt(1)});__state.Scoped=true;}}
 static void ArrowUpdated(ArrowObj __instance,ArrowStep __state){__state.Restore();ArrowAfter(__instance,__state.Position);}
 static void ArrowFinished(ArrowStep __state){__state?.Restore();}
 static void ArrowAfter(ArrowObj __instance,Vector3 __state){
  if(!CoopMode.Enabled||!CoopCharacters.IndependentReady||GameMgr.I==null||GameMgr.I.CurState!=GameState.kPlaying||__instance._isRemoving||__instance.IsFriendly()||!__instance.gameObject.activeInHierarchy)return;
  var player=PlayerAt(1);if(player==null||CoopVitals.Down(1)||!player.gameObject.activeInHierarchy)return;
  Vector2 a=__state,b=__instance.transform.position,p=Pos(player),delta=b-a;
  float t=delta.sqrMagnitude<0.000001f?0:Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude);
  float radius=Mathf.Max(0.12f,Mathf.Max(ArrowObj.kHitDist,Player.kProjCheckRadius));
  if((p-(a+delta*t)).sqrMagnitude>radius*radius)return;
  CoopCharacters.ForSecond(()=>{if(player.CanBeDamaged(PieceDmgType.kArrow)){__instance.OnHitPlayer();SecondHits++;}});
 }
 static void StateChanged(GameState __0){
  if(!CoopMode.Enabled)return;
  if(__0==GameState.kPaused||__0==GameState.kPlaying)PauseChanged=Time.unscaledTime;
  if(__0!=GameState.kPlaying)return;
  for(int i=0;i<2;i++){var player=PlayerAt(i);if(player!=null)player.IsRunningLevelUp=false;}
 }
 static bool AllowState(GameMgr __instance,GameState __0){if(CoopMode.Enabled&&(__instance==null||SceneMgr.GetCurScene()!=GameScene.kGame))return false;return !CoopMode.Enabled||BattleSaveData.I==null||!BattleSaveData.I.CompletedLevel||(__0!=GameState.kLevelUp&&__0!=GameState.kBonusBall&&__0!=GameState.kBonusPassive);}
 static bool LiveRecall(BallMgr __instance){return !CoopMode.Enabled||(__instance!=null&&SceneMgr.GetCurScene()==GameScene.kGame);}
 static bool Vibrate(float __0,float __1){
  if(!CoopMode.Enabled||!CoopCharacters.IndependentReady)return true;
  // Player.I is scoped to P2 during native hits and shooting, so vibration follows the owner.
  if(Player.I!=PlayerAt(1))return true;
  TwoGamepads.VibratePad(1,__0,__1);return false;
 }
 public static void Tick(){
  if(!CoopMode.Enabled||SceneMgr.GetCurScene()!=GameScene.kGame){if(Marker!=null)Marker.gameObject.SetActive(false);return;}
  var first=PlayerAt(0);if(first!=null&&PlayerAt(1)!=null){
   if(Marker==null){var obj=new GameObject("CoopP1Indicator");Marker=obj.AddComponent(Il2CppType.Of<Shapes.Disc>()).Cast<Shapes.Disc>();Marker.Type=Shapes.DiscType.Disc;Marker.Geometry=Shapes.DiscGeometry.Billboard;Marker.ZTest=UnityEngine.Rendering.CompareFunction.Always;Marker.SortingOrder=100;Marker.RenderQueue=4000;Marker.Radius=0.13f;Marker.Color=new Color(1,0.5f,0.72f,1);}
   Marker.gameObject.SetActive(!CoopVitals.Down(0));Marker.transform.position=Pos(first)+new Vector3(0,0,-1f);
  }
  if(CoopTestHarness.Active||!Application.isFocused||GameMgr.I==null||Time.unscaledTime-PauseChanged<0.25f)return;
  bool pressed=Input.GetKeyDown(KeyCode.Escape);
  for(int i=0;i<2;i++){var pad=TwoGamepads.GetPad(i);if(pad!=null&&pad.start.justPressed)pressed=true;}
  if(!pressed)return;
  if(GameMgr.I.CurState==GameState.kPlaying)GameMgr.I.SetState(GameState.kPaused,false);
  else if(GameMgr.I.CurState==GameState.kPaused&&PauseUI.I!=null)PauseUI.I.OnCloseClicked();
 }
}



