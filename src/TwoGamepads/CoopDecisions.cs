using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
public static class CoopDecisions {
 sealed class Option {public string Name;public UpgradeChoice Upgrade;public HeroCombo? Combo;}
 static readonly List<Option>[] Options={new List<Option>(),new List<Option>()};
 static readonly DecisionGate Gate=new DecisionGate();
 static readonly float[] Next={0,0};
 static ManualLogSource Log;
 static LevelUpUI UI;
 static bool Pending,Applying,Started,ErrorBlocked;
 static bool AwaitResume;
 static float Armed;
 static readonly List<PropertyInfo> Lists=new List<PropertyInfo>();
 static I2.Loc.LocalizationParamsManager DescriptionParams;
 public static void Install(Harmony h,Type controller,ManualLogSource log){
  Log=log;
  foreach(var p in typeof(LevelUpUI).GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))if(p.CanRead&&p.CanWrite&&(p.Name.StartsWith("_avail")||p.Name=="_choices"||p.Name=="_fusionChoices"))Lists.Add(p);
  h.Patch(AccessTools.Method(typeof(LevelUpUI),"Activate",new[]{typeof(LevelUpType)}),prefix:new HarmonyMethod(typeof(CoopDecisions),nameof(CanOpen)),postfix:new HarmonyMethod(typeof(CoopDecisions),nameof(Open)));
  h.Patch(AccessTools.Method(typeof(LevelUpUI),"MyUpdate"),prefix:new HarmonyMethod(typeof(CoopDecisions),nameof(Update)));
  foreach(string m in new[]{"OnSelectUpgradeClicked","OnContinueClicked","ApplyCombo","CloseLevelUp","OnCloseClicked","SkipFreeUpgrades","ConfirmIgnoreFuser","ConfirmIgnoreBonus"})h.Patch(AccessTools.Method(typeof(LevelUpUI),m),prefix:new HarmonyMethod(typeof(CoopDecisions),nameof(Guard)));
  h.Patch(AccessTools.Method(typeof(LevelUpUI),"ShouldAutopick"),prefix:new HarmonyMethod(typeof(CoopDecisions),nameof(Autopick)));
  h.Patch(AccessTools.Method(typeof(LevelUpUI),"Deactivate"),postfix:new HarmonyMethod(typeof(CoopDecisions),nameof(Closed)));
  h.Patch(AccessTools.Method(typeof(LevelUpUI),"OnExitComplete"),postfix:new HarmonyMethod(typeof(CoopDecisions),nameof(Resume)));

 }
 static void Open(LevelUpUI __instance){
  if(!CanOpen())return;
  UI=__instance;Started=false;ErrorBlocked=false;Gate.Reset();Next[0]=Next[1]=0;
  Pending=CoopCharacters.IndependentReady&&(CoopMode.Enabled||TwoGamepads.GetPad(1)!=null);
  if(Pending){NativeChoiceOverlay.Create(__instance);NativeChoiceOverlay.ClearChoices();}
  Armed=Time.unscaledTime+0.6f;
  if(Pending){TwoGamepads.VibratePad(1,0.3f,0.15f);Log.LogInfo("Coop decision opened: "+UI.Type+"; waiting for both players.");}
 }
 static bool Guard(){return !Pending||Applying;}
 static bool CanOpen(){return !CoopMode.Enabled||BattleSaveData.I==null||!BattleSaveData.I.CompletedLevel;}
 public static void AbortForSceneChange(){Pending=false;Started=false;AwaitResume=false;UI=null;NativeChoiceOverlay.Hide();}
 static bool Autopick(ref bool __result){if(!Pending)return true;__result=false;return false;}
 static void Closed(){Pending=false;Started=false;UI=null;NativeChoiceOverlay.Hide();}
 static void Resume(){
  if(!AwaitResume||Pending)return;AwaitResume=false;
  if(GameMgr.I!=null&&(GameMgr.I.CurState==GameState.kLevelUp||GameMgr.I.CurState==GameState.kBonusBall||GameMgr.I.CurState==GameState.kBonusPassive))GameMgr.I.SetState(GameState.kPlaying,false);
 }
 static string UpgradeName(UpgradeChoice choice){
  if(choice==null||choice.Info==null)return CoopLocalization.Text("Continuar");
  string slug=choice.Info.Slug;
  try{var translated=CoopLocalization.Translate(choice.Info.GetUpgradeSlug(1));if(!string.IsNullOrEmpty(translated)&&!translated.Contains("Missing Translation"))return translated;}catch{}
  return choice.Info.Name;
 }
 static string HeroName(HeroType type){var info=new HeroInst(type).GetInfo();try{var translated=CoopLocalization.Translate(info.GetUpgradeSlug(1));if(!string.IsNullOrEmpty(translated)&&!translated.Contains("Missing Translation"))return translated;}catch{}return info.Name;}
 static string Description(UpgradeInfo info,int level=1){
  if(info==null)return "";
  string text=info.Desc??"";
  try{
   var translated=CoopLocalization.Translate(info.GetDescSlug());
   if(!string.IsNullOrEmpty(translated)&&!translated.Contains("Missing Translation"))text=translated;
   if(DescriptionParams==null){
    var root=new GameObject("CoopDescriptionParameters");root.SetActive(false);UnityEngine.Object.DontDestroyOnLoad(root);
    DescriptionParams=root.AddComponent<I2.Loc.LocalizationParamsManager>();
    DescriptionParams._IsGlobalManager=false;
   }
   DescriptionParams._Params.Clear();
   info.ApplyPropertyParams(DescriptionParams);
   info.ApplyUpgradeParams(level,DescriptionParams,false);
   I2.Loc.LocalizationManager.ApplyLocalizationParams(ref text,DescriptionParams.gameObject,true);
  }catch(Exception e){if(!DescriptionWarning){DescriptionWarning=true;Log.LogWarning("Coop description parameters unavailable: "+e.Message);}}
  return text;
 }
 static bool DescriptionWarning;
 static int ChoiceLevel(UpgradeChoice choice){
  if(choice.IsNew)return 1;
  int idx=choice.EquipmentIdx;
  if(choice.Type==UpgradeType.kHero&&idx>=0&&idx<BattleSaveData.I.Heroes.Count)return BattleSaveData.I.Heroes[idx].Lvl+1;
  if(choice.Type==UpgradeType.kPassive&&idx>=0&&idx<BattleSaveData.I.Passives.Count)return BattleSaveData.I.Passives[idx].Lvl+1;
  var nativeItem=UI.GetCurUpgradeItem(choice);return nativeItem!=null?nativeItem.DisplayedLvl+1:1;
 }
 static string OptionName(Option choice){if(choice.Upgrade!=null)return UpgradeName(choice.Upgrade);if(choice.Combo.HasValue)return CoopLocalization.Text("Fusão: ")+HeroName(choice.Combo.Value.H1)+" + "+HeroName(choice.Combo.Value.H2);return CoopLocalization.Relocalize(choice.Name);}
 static string ChoiceDescription(Option choice){
  return choice.Upgrade!=null?Description(choice.Upgrade.Info,ChoiceLevel(choice.Upgrade)):choice.Combo.HasValue?Description(new HeroInst(choice.Combo.Value.H1).GetInfo())+"\n+ "+Description(new HeroInst(choice.Combo.Value.H2).GetInfo()):"";
 }
 static void Append(List<Option> target,Il2CppSystem.Collections.Generic.List<UpgradeChoice> choices){
  if(choices==null)return;
  for(int i=0;i<choices.Count;i++)if(choices[i]!=null&&choices[i].Info!=null)target.Add(new Option{Name=UpgradeName(choices[i]),Upgrade=choices[i]});
 }
 static void Build(List<Option> target,bool primary){
  target.Clear();
  if(UI.Type==LevelUpType.kFuser){
   // Native menu combo indices can belong to the other inventory after it
   // populates both panels. Build pairs from the current player's own list.
   {
    var heroes=BattleSaveData.I.Heroes;
    for(int i=0;i<heroes.Count;i++)for(int j=i+1;j<heroes.Count;j++){
     var a=heroes[i];var b=heroes[j];
     if(a.Lvl>=UpgradeInst<HeroInfo>.kMaxSoloLvl&&b.Lvl>=UpgradeInst<HeroInfo>.kMaxSoloLvl&&!a.IsBadCombo(b))
      target.Add(new Option{Name=CoopLocalization.Text("Fusão: ")+HeroName(a.Type)+" + "+HeroName(b.Type),Combo=new HeroCombo(i,a.Type,j,b.Type)});
    }
   }
   Append(target,UI._availMerges);
  }
  if(primary&&UI._choices!=null&&UI._choices.Count>0)Append(target,UI._choices);
  else{
   if(UI.Type!=LevelUpType.kBonusPassive){Append(target,UI._availHeroUpgrades);Append(target,UI._availNewHeroes);}
   if(UI.Type!=LevelUpType.kBonusBall){Append(target,UI._availPassiveUpgrades);Append(target,UI._availNewPassives);Append(target,UI._availPetUpgrades);Append(target,UI._availNewPetUpgrades);}
  }
  // Normal level-ups retain three choices; fusion events expose eligible pairs.
  if(UI.Type!=LevelUpType.kFuser&&target.Count>3){
   var rng=new System.Random(BattleSaveData.I.GetCurLvlSeed()^BattleSaveData.I.UpgradeLvl^(primary?37:71));
   for(int i=target.Count-1;i>0;i--){int j=rng.Next(i+1);var t=target[i];target[i]=target[j];target[j]=t;}
   target.RemoveRange(3,target.Count-3);
  }
  if(target.Count==0)target.Add(new Option{Name=CoopLocalization.Text("Continuar — nenhuma opção disponível")});
 }
 static void Prepare(){
  Build(Options[0],true);
  var saved=new object[Lists.Count];for(int i=0;i<saved.Length;i++)saved[i]=Lists[i].GetValue(UI);
  try{CoopCharacters.ForSecond(()=>{UI.PopulateUpgrades();Build(Options[1],false);
   if(UI.Type==LevelUpType.kFuser&&Options[1].Count==1&&Options[1][0].Upgrade==null&&!Options[1][0].Combo.HasValue){
    var type=UI.Type;try{UI.Type=LevelUpType.kNormal;UI.PopulateUpgrades();Build(Options[1],false);}finally{UI.Type=type;}
   }
  });}
  finally{for(int i=0;i<saved.Length;i++)Lists[i].SetValue(UI,saved[i]);}
  for(int slot=0;slot<2;slot++){if(CoopVitals.Down(slot)&&CoopVitals.Life.Rescued[slot]){Options[slot].Clear();Options[slot].Add(new Option{Name=CoopLocalization.Text("Fora da partida — aguardando parceiro")});}if(Options[slot].Count==1&&Options[slot][0].Upgrade==null&&!Options[slot][0].Combo.HasValue)Gate.Confirm(slot,true);}
  Started=true;Log.LogInfo("Coop decision options: P1="+Options[0].Count+"; P2="+Options[1].Count);
 }
 static bool Update(){
  if(!Pending)return true;
  if(ErrorBlocked)return false;
  if(Time.unscaledTime<Armed)return false;
  if(!Started)Prepare();
  if(CoopTestHarness.Active)return false;
  if(!Application.isFocused)return false;
  for(int s=0;s<2;s++){
   if(Options[s].Count==1&&Options[s][0].Upgrade==null&&!Options[s][0].Combo.HasValue)continue;
   var pad=TwoGamepads.GetPad(s);if(pad==null){Gate.Cancel(s);continue;}
   float axis=pad.leftStick.horizontal.value;
   if(Math.Abs(axis)<0.45f)Next[s]=0;
   if(Math.Abs(axis)>0.55f&&Time.unscaledTime>=Next[s]){Gate.Move(s,axis>0?1:-1,Options[s].Count);Next[s]=Time.unscaledTime+0.22f;}
   if(pad.b.justPressed)Gate.Cancel(s);
   if(pad.a.justPressed){Gate.Confirm(s,true);TwoGamepads.VibratePad(s,0.15f,0.08f);}
  }
  if(Gate.Complete)Commit();
  return false;
 }
 static void Apply(Option choice){if(choice.Upgrade!=null)UpgradeMgr.I.ApplyUpgrade(choice.Upgrade);else if(choice.Combo.HasValue){var pair=choice.Combo.Value;var heroes=BattleSaveData.I.Heroes;int first=-1,second=-1;for(int i=0;i<heroes.Count;i++)if(heroes[i].Type==pair.H1){first=i;break;}for(int i=0;i<heroes.Count;i++)if(i!=first&&heroes[i].Type==pair.H2){second=i;break;}if(first<0||second<0)throw new InvalidOperationException("Fusion ingredients no longer belong to this player.");UpgradeMgr.I.CombineHeroes(new HeroCombo(first,pair.H1,second,pair.H2));}UpgradeMgr.I.CalculateStats();}
 static void Commit(){
  var ui=UI;
  Applying=true;
  try{
   Apply(Options[0][Gate.Index[0]]);
   CoopCharacters.ForSecond(()=>Apply(Options[1][Gate.Index[1]]));
   Log.LogInfo("Both coop decisions committed: "+Options[0][Gate.Index[0]].Name+" / "+Options[1][Gate.Index[1]].Name);
   Pending=false;
   AwaitResume=true;
   ui.CloseLevelUp();
  }catch(Exception e){ErrorBlocked=true;Log.LogError("Coop decision failed; no automatic retry to avoid applying a choice twice: "+e);}
  finally{Applying=false;}
 }
 public static void Draw(){
  if(!Pending||UI==null)return;
  NativeChoiceOverlay.Show(UI.Type==LevelUpType.kFuser?CoopLocalization.Text("Fusões e melhorias — escolham juntos"):CoopLocalization.Text("Melhorias dos dois jogadores"),ErrorBlocked?CoopLocalization.Text("Não foi possível aplicar a escolha. A fase continua pausada."):CoopLocalization.Text("O jogo só continua quando os dois confirmarem."));
  for(int slot=0;slot<2;slot++){
   var choice=Started?Options[slot][Gate.Index[slot]]:null;
   Sprite icon=choice!=null&&choice.Upgrade!=null?choice.Upgrade.Info.Icon:null;
   NativeChoiceOverlay.Card(slot,choice==null?CoopLocalization.Text("Preparando opções..."):OptionName(choice),Gate.Ready[slot],icon);
   if(Started){
    var names=new string[Options[slot].Count];var icons=new Sprite[names.Length];var pairs=new Sprite[names.Length];
    for(int i=0;i<names.Length;i++){var option=Options[slot][i];names[i]=OptionName(option);icons[i]=option.Upgrade==null?null:option.Upgrade.Info.Icon;if(option.Combo.HasValue){icons[i]=new HeroInst(option.Combo.Value.H1).GetInfo().GetIcon();pairs[i]=new HeroInst(option.Combo.Value.H2).GetInfo().GetIcon();}}
    string description="";if(slot==1)CoopCharacters.ForSecond(()=>description=ChoiceDescription(choice));else description=ChoiceDescription(choice);NativeChoiceOverlay.Options(slot,names,icons,Gate.Index[slot],pairs,description);
   }
  }
 }
}



