using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Rewired;
using UnityEngine;

[BepInPlugin("local.ballxpit.twogamepads","Local Coop Two Gamepads","2.1.11")]
[BepInDependency("sparrow.ballxpit.localcoop",BepInDependency.DependencyFlags.HardDependency)]
public class TwoGamepads : BasePlugin {
 static Type ControllerType;
 static FieldInfo Move,Aim,Mode;
 static MethodInfo ApplyMove,ApplyAim,AimArc;
 static ConfigEntry<bool> Enabled;
 static ConfigEntry<float> Deadzone;
 static ConfigEntry<float> AimSpeed;
 static ConfigEntry<bool> InvertY;
 static IGamepadTemplate Pad;
 static int LastCount=-1,LastPad=-1,LastFirst=-1;
 static ControllerRoles Roles=new ControllerRoles();
 static FieldInfo AutoShoot,SecondPlayer;
 static PropertyInfo RecallActive;
 static TwoGamepads Plugin;
 static MethodInfo ActiveControllerChanged;
 static int MappedFirst=-1,MapRetries;
 static bool FirstTemplateReady;
 static float NextMapRefresh;
 static bool HadFocus;
 static Rewired.Player RoutingPlayer;static bool OriginalAutoAssignment;static readonly List<int> OriginalJoysticks=new List<int>();
 static readonly ControllerFrameAim PrimaryTurn=new ControllerFrameAim();static IntPtr PrimaryAimOwner;
 static readonly ControllerFrameAim SecondTurn=new ControllerFrameAim();static IntPtr SecondAimOwner;
 public static bool TestShoot;static bool Warned; static float NextShotLog;
 public override void Load(){
  Plugin=this;
  Enabled=Config.Bind("Controllers","Enabled",true,"Separate first and second connected gamepads. Keyboard fallback remains available.");
  Deadzone=Config.Bind("Controllers","Deadzone",0.18f,new ConfigDescription("Radial analog stick deadzone.",new AcceptableValueRange<float>(0.05f,0.5f)));
  AimSpeed=Config.Bind("Controllers","AimTurnSpeed",75f,new ConfigDescription("Maximum P2 aim turn speed in degrees per second.",new AcceptableValueRange<float>(30f,420f)));
  if(AimSpeed.Value==180f)AimSpeed.Value=75f;
  InvertY=Config.Bind("Controllers","InvertY",false,"Invert both vertical sticks only if your controller uses reversed Y.");
  ControllerType=typeof(BALLxPITLocalCoop.Plugin).Assembly.GetType("BALLxPITLocalCoop.PlayerTwoController",true);
  Move=AccessTools.Field(ControllerType,"_playerTwoMoveDir");
  Aim=AccessTools.Field(ControllerType,"_playerTwoAimDir");
  Mode=AccessTools.Field(ControllerType,"_playerTwoAimMode");
  AutoShoot=AccessTools.Field(ControllerType,"_playerTwoAutoShootEnabled");
  SecondPlayer=AccessTools.Field(ControllerType,"_playerTwo");
  ApplyMove=AccessTools.Method(ControllerType,"ApplyPlayerTwoMovementOverrides");
  ApplyAim=AccessTools.Method(ControllerType,"ApplyPlayerTwoAim");
  AimArc=AccessTools.Method(ControllerType,"GetNativeAimArcDegrees");
  if(Move==null||Aim==null||Mode==null||ApplyMove==null||ApplyAim==null||AimArc==null)throw new InvalidOperationException("Unsupported coop controller version.");
  var h=new Harmony("local.ballxpit.twogamepads");
  CoopSession.Install(h,ControllerType);
  CoopPause.Install(h);
  ActiveControllerChanged=AccessTools.Method(typeof(InputMgr),"OnLastActiveControllerChanged");
  h.Patch(ActiveControllerChanged,prefix:new HarmonyMethod(typeof(TwoGamepads),nameof(AllowActiveController)));
  h.Patch(AccessTools.Method(typeof(Player),"UpdateAim"),prefix:new HarmonyMethod(typeof(TwoGamepads),nameof(BeforePrimaryAim)),postfix:new HarmonyMethod(typeof(TwoGamepads),nameof(PrimaryAim)));
  h.Patch(AccessTools.Method(typeof(Player),"SetAimDir"),prefix:new HarmonyMethod(typeof(TwoGamepads),nameof(PrimarySetAim)));
  foreach(var method in typeof(InputMgr).GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))if(method.Name=="GetAxis"||method.Name=="IsBtnHeld"||method.Name=="IsBtnDown"||method.Name=="IsBtnUp")h.Patch(method,prefix:new HarmonyMethod(typeof(TwoGamepads),nameof(EnforceInputOwner)){priority=Priority.First});
  h.Patch(AccessTools.Method(typeof(InputMgr),"GetAxis",new[]{typeof(GameActionType)}),prefix:new HarmonyMethod(typeof(TwoGamepads),nameof(PrimaryAxis)){priority=Priority.First});
  h.Patch(AccessTools.Method(typeof(InputMgr),"IsBtnHeld",new[]{typeof(GameActionType)}),prefix:new HarmonyMethod(typeof(TwoGamepads),nameof(PrimaryHeld)){priority=Priority.First});
  h.Patch(AccessTools.Method(ControllerType,"Update"),prefix:new HarmonyMethod(typeof(TwoGamepads),nameof(Poll)));
  h.Patch(AccessTools.Method(ControllerType,"UpdatePlayerTwoInput"),prefix:new HarmonyMethod(typeof(TwoGamepads),nameof(InputPrefix)));
  CoopVisuals.Install(h,ControllerType,Log);
  CoopCharacters.Install(h,ControllerType,Log);
  CoopDecisions.Install(h,ControllerType,Log);
  CoopCompatibility.Install(h,ControllerType,Log);
  CoopTutorial.Install(h);
  CoopMode.Install(h,ControllerType,Log);CoopCombat.Install(h,ControllerType,Log);CoopVitals.Install(h,ControllerType,Log);CoopRoundStats.Install(h,ControllerType);CoopBossMechanics.Install(h);CoopPresentation.Install(h);CoopComfort.Install(h);CoopPickups.Install(h,ControllerType);
  var native=ControllerType.Assembly.GetType("BALLxPITLocalCoop.NativePlayerPatches");RecallActive=AccessTools.Property(native,"IsGlobalRecallActive");h.Patch(AccessTools.Method(native,"StartPlayerTwoRunBalls"),postfix:new HarmonyMethod(typeof(TwoGamepads),nameof(ShotResult)));
  CoopAmmoReload.Install(h);CoopShootingRecovery.Install(h);CoopTestHarness.Install(h,ControllerType,Log);CoopVolleyAudit.Install(h,Log);AddComponent<CoopDriver>();
  foreach(string method in new[]{"UpdatePlayerTwoShootControls","TryPlayerTwoShootLateFrame"})
   h.Patch(AccessTools.Method(ControllerType,method),prefix:new HarmonyMethod(typeof(TwoGamepads),nameof(AllowCombat)),transpiler:new HarmonyMethod(typeof(TwoGamepads),nameof(ShootTranspiler)));
  Log.LogInfo("Two Gamepads loaded: P1 first gamepad; P2 second gamepad, left stick move, right stick aim, RT/RB/A shoot. X toggles autoshoot.");
 }
 static void Poll(object __instance){
  Pad=null;
  CoopVisuals.TwoControllers=false;
  if(!Enabled.Value||!ReInput.isReady)return;
  try{
   var sticks=ReInput.controllers.Joysticks;
   int count=ReInput.controllers.joystickCount;
   if(count!=LastCount){Plugin.Log.LogInfo("Connected gamepads: "+count);LastCount=count;}
   var ids=new int[count];var identities=new string[count];
   for(int i=0;i<count;i++){ids[i]=sticks[i].id;string guid=sticks[i].deviceInstanceGuid.ToString();identities[i]=string.IsNullOrEmpty(guid)||guid=="00000000-0000-0000-0000-000000000000"?sticks[i].hardwareIdentifier+"|"+sticks[i].name:guid;}
   Roles.Update(ids,identities,true);
   Joystick first=null,second=null;
   for(int i=0;i<count;i++){if(sticks[i].id==Roles.First)first=sticks[i];if(sticks[i].id==Roles.Second)second=sticks[i];}
   var mgr=InputMgr.I;
   if(mgr==null)return;
   var player=mgr.GetRewiredPlayer();
   if(player==null)return;
   Pad=second?.GetTemplate<IGamepadTemplate>();
   if(!CoopMode.Enabled){LeaveCoopRouting();return;}
   if(RoutingPlayer==null||RoutingPlayer.Pointer!=player.Pointer){LeaveCoopRouting();RoutingPlayer=player;OriginalAutoAssignment=player.controllers.excludeFromControllerAutoAssignment;for(int i=0;i<count;i++)if(player.controllers.ContainsController(sticks[i]))OriginalJoysticks.Add(sticks[i].id);}
   bool assignmentChanged=false;
   bool regainedFocus=Application.isFocused&&!HadFocus;
   HadFocus=Application.isFocused;
   // Rewired's automatic reassignment on focus changes must not merge coop slots.
   player.controllers.excludeFromControllerAutoAssignment=true;
   // Our shared coop menus read both templates directly. Rewired P1 must never
   // own P2, including loading and the frame of a late connection.
   for(int i=0;i<count;i++)if(sticks[i].id!=Roles.First&&player.controllers.ContainsController(sticks[i])){player.controllers.RemoveController(sticks[i]);assignmentChanged=true;}
   if(first!=null&&!player.controllers.ContainsController(first)){player.controllers.AddController(first,true);assignmentChanged=true;}
   bool templateReady=first!=null&&first.GetTemplate<IGamepadTemplate>()!=null;
   if(MappedFirst!=Roles.First||assignmentChanged||templateReady!=FirstTemplateReady||regainedFocus){
    MappedFirst=Roles.First;FirstTemplateReady=templateReady;MapRetries=3;NextMapRefresh=Time.unscaledTime+0.25f;
   }
   // Rewired may finish creating a newly connected joystick's maps after assignment.
   // Select P1 explicitly so a late P2 connection cannot leave P1 using P2's map.
   if(first!=null&&MapRetries>0&&Time.unscaledTime>=NextMapRefresh){
    MapRetries--;NextMapRefresh=Time.unscaledTime+0.5f;
    ActiveControllerChanged.Invoke(mgr,new object[]{player,first});
    mgr.RefreshControllerMap(true);
    if(mgr._lastJoystickGameMap!=null){MapRetries=0;Plugin.Log.LogInfo("Refreshed P1 controller mapping: "+first.name);}
   }
   if(Roles.First!=LastFirst||Roles.Second!=LastPad){
    if(Roles.First!=LastFirst)PrimaryTurn.Reset();if(Roles.Second!=LastPad)SecondTurn.Reset();
    if(!CoopVitals.Active)AutoShoot?.SetValue(__instance,false);
    Plugin.Log.LogInfo("Separated controllers: P1="+(first==null?"disconnected":first.name)+"; P2="+(second==null?"disconnected / keyboard":second.name));
    Plugin.Log.LogInfo("Controller vibration: P1="+(first!=null&&first.supportsVibration)+"; P2="+(second!=null&&second.supportsVibration));
    if(first!=null)Plugin.Log.LogInfo("P1 device: id="+first.id+", instance="+first.deviceInstanceGuid+", hardware="+first.hardwareIdentifier);
    if(second!=null)Plugin.Log.LogInfo("P2 device: id="+second.id+", instance="+second.deviceInstanceGuid+", hardware="+second.hardwareIdentifier);
    LastFirst=Roles.First;LastPad=Roles.Second;Warned=false;
   }
   if(second==null)return;
   Pad=second.GetTemplate<IGamepadTemplate>();
   CoopVisuals.TwoControllers=Pad!=null;
   if(Pad==null&&!Warned){Plugin.Log.LogWarning("Second controller has no standard Gamepad template; P2 keyboard fallback remains active.");Warned=true;}
  }catch(Exception e){if(!Warned){Plugin.Log.LogWarning("Gamepad routing unavailable: "+e.Message);Warned=true;}}
 }
 static bool AllowActiveController(Rewired.Controller __1){
  // The shared Rewired player belongs to P1 during independent coop decisions/combat.
  return !CoopMode.Enabled||!Enabled.Value||__1==null||__1.type!=Rewired.ControllerType.Joystick||__1.id==Roles.First;
 }
 static void EnforceInputOwner(){
  if(!CoopMode.Enabled||!Enabled.Value||!ReInput.isReady||InputMgr.I==null)return;var player=InputMgr.I.GetRewiredPlayer();if(player==null)return;player.controllers.excludeFromControllerAutoAssignment=true;
  var sticks=ReInput.controllers.Joysticks;for(int i=0;i<ReInput.controllers.joystickCount;i++)if(sticks[i].id!=Roles.First&&player.controllers.ContainsController(sticks[i]))player.controllers.RemoveController(sticks[i]);
 }
 static bool PrimaryInput(){return Enabled.Value&&CoopMode.Enabled&&CoopVitals.Active&&!CoopTestHarness.Active&&Application.isFocused&&Player.I==CoopCombat.PlayerAt(0);}
 static int InputSlot(){return !Enabled.Value||!CoopVitals.Active||CoopTestHarness.Active||!Application.isFocused?-1:CoopSessionRules.InputSlot(CoopMode.Enabled,CoopCharacters.IndependentReady,CoopCombat.SimulationOwner,Player.I==CoopCombat.PlayerAt(1));}
 static bool PrimaryAxis(GameActionType __0,ref float __result){
  int slot=InputSlot();if(slot<0)return true;var pad=GetPad(slot);Vector2 axis;
  if(pad==null){if(__0==GameActionType.kMoveHorizontal||__0==GameActionType.kMoveVertical||__0==GameActionType.kAimHorizontal||__0==GameActionType.kAimVertical){__result=0;return false;}return true;}
  if(__0==GameActionType.kMoveHorizontal||__0==GameActionType.kMoveVertical)axis=Stick(pad.leftStick.value,Deadzone.Value);
  else if(__0==GameActionType.kAimHorizontal||__0==GameActionType.kAimVertical)axis=Stick(pad.rightStick.value,Deadzone.Value);else return true;
  if(InvertY.Value)axis.y=-axis.y;__result=(__0==GameActionType.kMoveHorizontal||__0==GameActionType.kAimHorizontal)?axis.x:axis.y;return false;
 }
 static bool PrimaryHeld(GameActionType __0,ref bool __result){int slot=InputSlot();if(__0!=GameActionType.kShoot||slot<0)return true;var pad=GetPad(slot);__result=pad!=null&&(pad.rightTrigger.value>0.25f||pad.rightBumper.value||pad.a.value);return false;}
 static void BeforePrimaryAim(Player __instance,out Vector2 __state){__state=__instance.GetLastAimDir();}
 static bool PrimaryDirection(Player player,out Vector2 direction){
  direction=Vector2.up;
  if(!PrimaryInput()||CoopVitals.Down(0)||player!=CoopCombat.PlayerAt(0)||BallMgr.I==null)return false;
  var pad=GetPad(0);if(pad==null)return false;var stick=pad.rightStick.value;
  if(InvertY.Value)stick.y=-stick.y;
  if(BattleSaveData.I.CurChar.HasProp(CharProp.kAimless))return false;
  if(PrimaryAimOwner!=player.Pointer){PrimaryAimOwner=player.Pointer;PrimaryTurn.Reset();}
  var low=BallMgr.I.ThetaToAimDir(BallMgr.I.GetMinTheta());var high=BallMgr.I.ThetaToAimDir(BallMgr.I.GetMaxTheta());
  float min=Mathf.Atan2(low.y,low.x)*Mathf.Rad2Deg,max=Mathf.Atan2(high.y,high.x)*Mathf.Rad2Deg;
  if(min>max){float swap=min;min=max;max=swap;}
  var initial=player.GetLastAimDir();float previous=CoopAimRules.TargetDegrees(initial.x,initial.y,min,max);
  bool active=stick.sqrMagnitude>Deadzone.Value*Deadzone.Value;float desired=active?CoopAimRules.TargetDegrees(stick.x,stick.y,min,max):previous;
  float angle=PrimaryTurn.Update(Time.frameCount,active?Time.unscaledDeltaTime:0,previous,desired,AimSpeed.Value,min,max)*Mathf.Deg2Rad;
  direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));return true;
 }
 static void PrimarySetAim(Player __instance,ref Vector2 __0){Vector2 direction;if(PrimaryDirection(__instance,out direction))__0=direction;}
 static void PrimaryAim(Player __instance,Vector2 __state){Vector2 direction;if(PrimaryDirection(__instance,out direction))__instance.SetAimDir(direction);}
 static bool InputPrefix(object __instance){
  if(!CoopMode.Enabled)return true;
  if(Pad==null){Move.SetValue(__instance,Vector2.zero);ApplyMove.Invoke(__instance,new object[]{false});return false;}
  Vector2 move=!CoopVitals.Down(1)&&!CoopTestHarness.Active&&Application.isFocused&&AllowCombat()?Pad.leftStick.value:Vector2.zero;
  Vector2 aim=!CoopVitals.Down(1)&&!CoopTestHarness.Active&&Application.isFocused&&AllowCombat()?Pad.rightStick.value:Vector2.zero;
  if(InvertY.Value){move.y=-move.y;aim.y=-aim.y;}
  move=Stick(move,Deadzone.Value);
  if(CoopCharacters.IndependentReady&&(Automatic(1)||ReadShoot(KeyCode.Keypad0)))move*=MovementMultiplier(true);
  Move.SetValue(__instance,move);
  Mode.SetValue(__instance,Enum.ToObject(Mode.FieldType,0));
  if(aim.sqrMagnitude>Deadzone.Value*Deadzone.Value){
   object[] arc={15f,165f};AimArc.Invoke(__instance,arc);
   float theta=CoopAimRules.TargetDegrees(aim.x,aim.y,(float)arc[0],(float)arc[1]);
   var current=(Vector2)Aim.GetValue(__instance);
   float previous=CoopAimRules.TargetDegrees(current.x,current.y,(float)arc[0],(float)arc[1]);
   var actor=SecondPlayer.GetValue(__instance) as Player;if(actor!=null&&actor.Pointer!=SecondAimOwner){SecondAimOwner=actor.Pointer;SecondTurn.Reset();}
   theta=SecondTurn.Update(Time.frameCount,Time.unscaledDeltaTime,previous,theta,AimSpeed.Value,(float)arc[0],(float)arc[1]);
   float r=theta*Mathf.Deg2Rad;
   Aim.SetValue(__instance,new Vector2(Mathf.Cos(r),Mathf.Sin(r)));
  }
  ApplyMove.Invoke(__instance,new object[]{false});if(!CoopVitals.Down(1))ApplyAim.Invoke(__instance,null);
  return false;
 }
 public static Vector2 Stick(Vector2 v,float deadzone){
  float m=v.magnitude;if(m<=deadzone)return Vector2.zero;
  return v.normalized*Mathf.Clamp01((m-deadzone)/(1-deadzone));
 }
 public static float MovementMultiplier(bool shooting){if(!shooting||!CoopCharacters.IndependentReady)return 1;float ratio=1;CoopCharacters.ForSecond(()=>{if(UpgradeMgr.I.MoveSpeed>0)ratio=Mathf.Clamp01(UpgradeMgr.I.MoveSpeedWhileShooting/UpgradeMgr.I.MoveSpeed);});return ratio;}
 public static IGamepadTemplate GetPad(int slot){
  if(!ReInput.isReady)return null;
  int id=slot==0?Roles.First:Roles.Second;
  var sticks=ReInput.controllers.Joysticks;
  for(int i=0;i<ReInput.controllers.joystickCount;i++)if(sticks[i].id==id)return sticks[i].GetTemplate<IGamepadTemplate>();
  return null;
 }
 public static void VibratePad(int slot,float intensity,float duration){if(!ReInput.isReady)return;int id=slot==0?Roles.First:Roles.Second;var sticks=ReInput.controllers.Joysticks;for(int i=0;i<ReInput.controllers.joystickCount;i++)if(sticks[i].id==id&&sticks[i].supportsVibration)sticks[i].SetVibration(Mathf.Clamp01(intensity),Mathf.Clamp01(intensity),Mathf.Max(0,duration),Mathf.Max(0,duration));}
 static void ShotResult(bool __result,string __2){if(__result||Time.unscaledTime<NextShotLog)return;NextShotLog=Time.unscaledTime+5;Plugin.Log.LogInfo("P2 shot blocked: "+__2);if(CoopTestHarness.Active&&CoopCharacters.IndependentReady)CoopCharacters.ForSecond(()=>Plugin.Log.LogInfo("P2 launch diagnostics: health="+BattleSaveData.I.CurHealth+", interrupted="+BallMgr.I._isShootInterrupted+", state="+BallMgr.I.GetBallState()+", levelUp="+Player.I.IsRunningLevelUp+", idx="+BallMgr.I._curBallIdx+", shootComplete="+BallMgr.I.LastShootCompleteTime+", time="+TimeMgr.I.GetPhysicsTime()));}
 static bool AllowCombat(){return CoopMode.Enabled&&SceneMgr.GetCurScene()==GameScene.kGame&&!CoopVitals.Down(1)&&!CoopRoundStats.Complete&&GameMgr.I!=null&&GameMgr.I.CurState==GameState.kPlaying;}
 public static void ResetInputSession(){PrimaryTurn.Reset();SecondTurn.Reset();PrimaryAimOwner=SecondAimOwner=IntPtr.Zero;MappedFirst=-1;FirstTemplateReady=false;MapRetries=0;NextMapRefresh=0;HadFocus=false;}
 public static void LeaveCoopRouting(){if(RoutingPlayer==null)return;RoutingPlayer.controllers.excludeFromControllerAutoAssignment=OriginalAutoAssignment;if(ReInput.isReady){var sticks=ReInput.controllers.Joysticks;for(int i=0;i<ReInput.controllers.joystickCount;i++)if(OriginalJoysticks.Contains(sticks[i].id)&&!RoutingPlayer.controllers.ContainsController(sticks[i]))RoutingPlayer.controllers.AddController(sticks[i],false);}RoutingPlayer=null;OriginalJoysticks.Clear();ResetInputSession();}
 public static bool Automatic(int slot){if(!CoopCharacters.IndependentReady)return false;bool innate=false;if(slot==1)CoopCharacters.ForSecond(()=>innate=BattleSaveData.I.CurChar.HasProp(CharProp.kAlwaysShoot));else innate=BattleSaveData.I.CurChar.HasProp(CharProp.kAlwaysShoot);if(innate)return true;if(slot==1){var instance=AccessTools.Field(ControllerType,"_instance").GetValue(null);return instance!=null&&(bool)AutoShoot.GetValue(instance);}return PlayerPrefsMgr.sAutofire;}
 static bool ReadShoot(KeyCode key){if(TestShoot)return true;
  if(CoopTestHarness.Active)return false;
  bool keyboard=UnityEngine.Input.GetKey(key);
  if(Pad==null||!Application.isFocused)return keyboard;
  return keyboard||Pad.rightTrigger.value>0.25f||Pad.rightBumper.value||Pad.a.value;
 }
 static bool ReadToggle(KeyCode key){
  if(CoopTestHarness.Active)return false;
  bool keyboard=UnityEngine.Input.GetKeyDown(key);
  return keyboard||(Pad!=null&&Application.isFocused&&Pad.x.justPressed);
 }
 static IEnumerable<CodeInstruction> ShootTranspiler(IEnumerable<CodeInstruction> instructions){
  var key=AccessTools.Method(typeof(UnityEngine.Input),"GetKey",new[]{typeof(KeyCode)});
  var down=AccessTools.Method(typeof(UnityEngine.Input),"GetKeyDown",new[]{typeof(KeyCode)});
  foreach(var i in instructions){
   if(i.Calls(key))i.operand=AccessTools.Method(typeof(TwoGamepads),nameof(ReadShoot));
   if(i.Calls(down))i.operand=AccessTools.Method(typeof(TwoGamepads),nameof(ReadToggle));
   yield return i;
  }
 }
}
















