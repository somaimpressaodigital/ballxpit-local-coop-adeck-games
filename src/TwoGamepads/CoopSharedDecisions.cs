using System;
using HarmonyLib;
using UnityEngine;
public static class CoopSharedDecisions {
 public static bool Pending;
 static bool Applying;
 static float Armed;
 static readonly DecisionGate Gate=new DecisionGate();
 static readonly float[] Next={0,0};
 public static void Install(Harmony harmony){
  harmony.Patch(AccessTools.Method(typeof(Player),"Heal"),prefix:new HarmonyMethod(typeof(CoopSharedDecisions),nameof(SharedHeal)));
  harmony.Patch(AccessTools.Method(typeof(InGameUI),"RefreshHealthBar"),prefix:new HarmonyMethod(typeof(CoopSharedDecisions),nameof(HealthBar)));
  harmony.Patch(AccessTools.Method(typeof(GameMgr),"SetState"),postfix:new HarmonyMethod(typeof(CoopSharedDecisions),nameof(StateChanged)));
  foreach(string method in new[]{"OnYesClicked","OnNoClicked"})harmony.Patch(AccessTools.Method(typeof(ReviveUI),method),prefix:new HarmonyMethod(typeof(CoopSharedDecisions),nameof(Guard)));
 }
 static void StateChanged(GameState __0){
  if(__0!=GameState.kRevive){Pending=false;return;}
  Pending=CoopMode.Enabled&&CoopCharacters.IndependentReady&&ReviveUI.I!=null;
  if(!Pending)return;
  Gate.Reset();Next[0]=Next[1]=0;Armed=Time.unscaledTime+0.4f;NativeChoiceOverlay.Create(ReviveUI.I);
 }
 static bool SharedHeal(float __0){if(CoopMode.Enabled&&CoopMode.Preparing)return false;if(!CoopMode.Enabled||!Applying)return true;BattleSaveData.I.CurHealth=Mathf.Clamp(BattleSaveData.I.CurHealth+__0,1,UpgradeMgr.I.MaxHealth);return false;}
 static bool HealthBar(ref bool __0){if(CoopMode.Enabled&&CoopMode.Preparing)return false;if(CoopMode.Enabled&&Applying)__0=false;return true;}
 static bool Guard(){return !Pending||Applying;}
 public static void Tick(){
  if(CoopTestHarness.Active)return;
  if(!Pending||Time.unscaledTime<Armed||!Application.isFocused)return;
  for(int slot=0;slot<2;slot++){
   var pad=TwoGamepads.GetPad(slot);if(pad==null){Gate.Cancel(slot);continue;}
   float axis=pad.leftStick.horizontal.value;if(Math.Abs(axis)<0.4f)Next[slot]=0;
   if(Math.Abs(axis)>0.55f&&Time.unscaledTime>=Next[slot]){Gate.Move(slot,axis>0?1:-1,2);Next[slot]=Time.unscaledTime+0.22f;}
   if(pad.b.justPressed)Gate.Cancel(slot);
   if(pad.a.justPressed)Gate.Confirm(slot,true);
  }
  Commit();
 }
 static void Commit(){
  if(!Pending||!Gate.Complete||Gate.Index[0]!=Gate.Index[1])return;
  Applying=true;
  try{if(Gate.Index[0]==0)ReviveUI.I.OnYesClicked();else ReviveUI.I.OnNoClicked();Pending=false;NativeChoiceOverlay.Hide();}
  finally{Applying=false;}
 }
 public static void Draw(){
  if(!Pending)return;
  bool disagree=Gate.Complete&&Gate.Index[0]!=Gate.Index[1];
  NativeChoiceOverlay.Show(CoopLocalization.Text("Decidam juntos"),disagree?CoopLocalization.Text("Escolhas diferentes. Alterem a escolha para concordar."):CoopLocalization.Text("A decisão sobre a vida compartilhada precisa da confirmação dos dois."));
  for(int slot=0;slot<2;slot++){NativeChoiceOverlay.Card(slot,"",Gate.Ready[slot],null);NativeChoiceOverlay.Options(slot,new[]{CoopLocalization.Text("Reviver a equipe"),CoopLocalization.Text("Encerrar a partida")},new Sprite[]{null,null},Gate.Index[slot]);}
 }
}



