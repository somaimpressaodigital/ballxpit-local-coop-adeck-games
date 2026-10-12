using HarmonyLib;
using UnityEngine;
public static class CoopPause {
 static readonly PauseInputGate Gate=new PauseInputGate();
 public static void Install(Harmony harmony){
  foreach(string method in new[]{"IsBtnDown","IsBtnHeld","IsBtnUp"})harmony.Patch(AccessTools.Method(typeof(InputMgr),method,new[]{typeof(GameActionType)}),prefix:new HarmonyMethod(typeof(CoopPause),nameof(NativePauseInput)){priority=Priority.First});
  harmony.Patch(AccessTools.Method(typeof(GameMgr),"SetState"),prefix:new HarmonyMethod(typeof(CoopPause),nameof(BeforeState)){priority=Priority.First},postfix:new HarmonyMethod(typeof(CoopPause),nameof(StateChanged)){priority=Priority.Last});
 }
 static bool InBattle(){return CoopMode.Enabled&&SceneMgr.I!=null&&SceneMgr.GetCurScene()==GameScene.kGame&&GameMgr.I!=null;}
 static bool NativePauseInput(GameActionType __0,ref bool __result){if(__0!=GameActionType.kPause||!InBattle())return true;__result=false;return false;}
 static void BeforeState(GameMgr __instance,out GameState __state){__state=__instance.CurState;}
 static void StateChanged(GameMgr __instance,GameState __state){var current=__instance.CurState;if(InBattle()&&current!=__state&&(current==GameState.kPaused||current==GameState.kPlaying))Gate.Transition(Time.frameCount,Time.unscaledTime);}
 public static void Reset(){Gate.Reset();}
 public static void Tick(){
  if(!InBattle()){Gate.Reset();return;}if(CoopTestHarness.Active)return;
  var first=TwoGamepads.GetPad(0);var second=TwoGamepads.GetPad(1);
  Process(Time.frameCount,Time.unscaledTime,Application.isFocused,first!=null&&first.start.value,second!=null&&second.start.value,Input.GetKey(KeyCode.Escape));
 }
 public static bool Process(int frame,float now,bool focused,bool first,bool second,bool escape){
  if(!InBattle())return false;
  var state=GameMgr.I.CurState;bool allowed=state==GameState.kPlaying||state==GameState.kPaused;
  if(!Gate.Read(frame,now,focused,allowed,first,second,escape)||PauseUI.I==null)return false;
  if(state==GameState.kPlaying){PauseUI.I.Activate();if(GameMgr.I.CurState!=GameState.kPaused)GameMgr.I.SetState(GameState.kPaused,false);}
  else{PauseUI.I.OnCloseClicked();if(GameMgr.I.CurState==GameState.kPaused)GameMgr.I.SetState(GameState.kPlaying,false);}
  return true;
 }
}
