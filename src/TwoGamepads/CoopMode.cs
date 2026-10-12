using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using BepInEx.Logging;
public static class CoopMode {
 public sealed class PlayerProgress {public int Points{get;set;} public int[] Stats{get;set;}=new int[6];public Dictionary<int,int> Levels{get;set;}=new Dictionary<int,int>();public Dictionary<int,int> Experience{get;set;}=new Dictionary<int,int>();}
 public sealed class Progress {public CoopPresentation.Report LastResult{get;set;}public int Version{get;set;}=3;public int Wins{get;set;}public int Losses{get;set;}public List<string> Proofs{get;set;}=new List<string>();public List<int> Unlocked{get;set;}=new List<int>{0,1};public List<int> Cleared{get;set;}=new List<int>();public PlayerProgress[] Players{get;set;}=new[]{new PlayerProgress(),new PlayerProgress()};}
 public static bool Enabled; public static bool Preparing; static int LaunchStage; static float LaunchAt,NextLaunchLog;static bool NativeStarting;
 public static Progress State=new Progress();
 static ManualLogSource Log;
 static string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BALLxPITCoop");
 static string FilePath=Path.Combine(Folder,"progress.json");
 static string SeedMetaPath,SeedBackupPath;static MetaSaveData CoopMeta;static MetaSaveData SoloMeta; static BattleSaveData SoloBattle; static string[] SoloPaths;
 static bool Awarded,ChoosingLevel,Returning,Perks;
 static CharType First,Second;
 static readonly List<LevelInfo> Levels=new List<LevelInfo>();public static List<LevelInfo> Phases{get{return Levels;}}
 static readonly DecisionGate Gate=new DecisionGate();
 static readonly float[] Next={0,0};
 static int LevelIndex; static float LevelArmed;
 public static void UseTestFolder(string path){Folder=path;FilePath=Path.Combine(path,"progress.json");}
 public static CharBattleInst Character(CharType type,int slot){
  if(!Enabled)return new CharBattleInst(type);
  var meta=MetaSaveData.I.Chars[(int)type];int old=meta.Lvl,level;State.Players[slot].Levels.TryGetValue((int)type,out level);meta.Lvl=Math.Max(1,level);
  try{var character=new CharBattleInst(type);ApplyStats(character,slot);character._hasProp=character.GetPropArray();return character;}finally{meta.Lvl=old;}
 }
 public static void Install(Harmony h,Type controller,ManualLogSource log){
  Log=log;
  h.Patch(AccessTools.Method(typeof(GameMgr),"Start"),postfix:new HarmonyMethod(typeof(CoopMode),nameof(StartHealth)));
  h.Patch(AccessTools.Method(typeof(SaveMgr),"LoadMeta"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(UseCoopMeta)));
  h.Patch(AccessTools.Method(typeof(SaveMgr),"LoadBattle"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(UsePreparedBattle)));
  h.Patch(AccessTools.Method(typeof(SaveMgr),"StartNewBattle"),postfix:new HarmonyMethod(typeof(CoopMode),nameof(BattleCreated)));
  h.Patch(AccessTools.Method(typeof(CharBattleInst),"HasProp",new[]{typeof(CharProp)}),prefix:new HarmonyMethod(typeof(CoopMode),nameof(CharacterProp)));
  h.Patch(AccessTools.Method(typeof(BuildingMgr),"GetBonusStat"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(NoCityStat)));
  h.Patch(AccessTools.Method(typeof(LevelData),"IsUnlocked"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(NativeLevelUnlocked)));
  h.Patch(AccessTools.Method(typeof(MainMenuUI),"Start"),postfix:new HarmonyMethod(typeof(CoopMode),nameof(Menu)));
  h.Patch(AccessTools.Method(typeof(MainMenuUI),"MyUpdate"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(CreditsInput)));
  h.Patch(AccessTools.Method(typeof(MainMenuUI),"SelectBtn"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(MenuChoice)));
  h.Patch(AccessTools.Method(typeof(MainMenuBtn),"OnClicked"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(MenuButtonClicked)));
  h.Patch(AccessTools.Method(typeof(CoolButton),"OnPointerEnter"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(CoopPointerEnter)));
  h.Patch(AccessTools.Method(typeof(CoolButton),"OnPointerExit"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(CoopPointerExit)));
  h.Patch(AccessTools.Method(typeof(CoolButton),"OnPointerDown"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(CoopPointerDown)));
  h.Patch(AccessTools.Method(typeof(CoolButton),"OnPointerUp"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(CoopPointerUp)));


  h.Patch(AccessTools.Method(typeof(GameMgr),"GoToBase"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(ReturnToMenu)));
  h.Patch(AccessTools.Method(typeof(GameMgr),"MarkLevelComplete"),postfix:new HarmonyMethod(typeof(CoopMode),nameof(Victory)));
  h.Patch(AccessTools.Method(typeof(GameMgr),"LoseGame"),postfix:new HarmonyMethod(typeof(CoopMode),nameof(Defeat)));
  h.Patch(AccessTools.PropertyGetter(typeof(Application),"persistentDataPath"),prefix:new HarmonyMethod(typeof(CoopMode),nameof(ProfilePath)));
  foreach(string m in new[]{"GetSlotDataPath","GetMetaSavePath","GetMetaSaveBackupPath","GetMetaSaveTempPath"})h.Patch(AccessTools.Method(typeof(SaveMgr),m),postfix:new HarmonyMethod(typeof(CoopMode),nameof(RedirectPath)));
 }
 static void Load(){
  Directory.CreateDirectory(Folder);
  if(File.Exists(FilePath))State=JsonSerializer.Deserialize<Progress>(File.ReadAllText(FilePath))??new Progress();
  if(State.Players==null||State.Players.Length!=2||State.Unlocked==null||State.Cleared==null)throw new InvalidDataException("Invalid coop progress file.");
  foreach(var player in State.Players){if(player.Experience==null)player.Experience=new Dictionary<int,int>();if(player.Levels==null)player.Levels=new Dictionary<int,int>();if(player.Stats==null||player.Stats.Length!=6)player.Stats=new int[6];player.Points=Math.Max(0,player.Points);for(int i=0;i<6;i++)player.Stats[i]=Math.Max(0,Math.Min(CoopProgressRules.MaxPermanentStat,player.Stats[i]));if(State.Version<2)foreach(var item in player.Levels)player.Experience[item.Key]=Math.Max(0,(item.Value-1)*3);}
  if(State.Proofs==null)State.Proofs=new List<string>();
  if(State.Version<3){foreach(var player in State.Players)foreach(var item in player.Levels)player.Experience[item.Key]=Math.Max(player.Experience.ContainsKey(item.Key)?player.Experience[item.Key]:0,CoopProgressRules.ExperienceForLevel(item.Value));foreach(int cleared in State.Cleared)foreach(int starter in new[]{0,1}){string proof=cleared+":"+starter;if(!State.Proofs.Contains(proof))State.Proofs.Add(proof);}}
  State.Proofs=new List<string>(new HashSet<string>(State.Proofs));if(State.Version<2)State.Wins=Math.Max(State.Wins,State.Cleared.Count);State.Version=3;
 }
 static void Save(){Directory.CreateDirectory(Folder);string temp=FilePath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(State,new JsonSerializerOptions{WriteIndented=true}));if(File.Exists(FilePath))File.Copy(FilePath,FilePath+".bak",true);File.Move(temp,FilePath,true);}
 static void Menu(MainMenuUI __instance){
  try{
   var old=__instance.Btns;if(old==null||old.Length==0)return;
   if(__instance.XfmBtns.Find("CoopLocalButton")!=null)return;
   var obj=UnityEngine.Object.Instantiate(old[0].gameObject,__instance.XfmBtns).Cast<GameObject>();obj.name="CoopLocalButton";
   var button=obj.GetComponent(Il2CppType.Of<MainMenuBtn>()).Cast<MainMenuBtn>();button.Option=(MainMenuOption)99;
   button.Btn=obj.GetComponentInChildren(Il2CppType.Of<CoolButton>(),true).Cast<CoolButton>();button.Txt=obj.GetComponentInChildren(Il2CppType.Of<TMPro.TextMeshProUGUI>(),true).Cast<TMPro.TextMeshProUGUI>();button.Loc=obj.GetComponentInChildren(Il2CppType.Of<I2.Loc.Localize>(),true)?.Cast<I2.Loc.Localize>();
   if(button.Loc!=null){button.Loc.enabled=false;UnityEngine.Object.Destroy(button.Loc);}button.Txt.text=CoopLocalization.Text("Coop local");
   // A cloned native button must own its click delegate rather than retaining
   // the source button's callback or waiting for its Awake subscription.
   button.Btn.OnClicked=(DelegateUtl.NoArgsEvent)(Action)(()=>button.OnClicked());
   button.Btn.OnHover=(DelegateUtl.NoArgsEvent)(Action)(()=>button.OnHover());button.Btn.OnHoverExit=(DelegateUtl.NoArgsEvent)(Action)(()=>button.OnHoverExit());button.Btn.OnSelected=button.Btn.OnHover;button.Btn.OnDeselected=button.Btn.OnHoverExit;
   foreach(var graphic in new[]{__instance.LogoBall,__instance.LogoX,__instance.LogoPit,__instance.LogoBackground})if(graphic!=null)graphic.raycastTarget=false;
   var decorations=__instance.GetComponentsInChildren(Il2CppType.Of<UnityEngine.UI.Graphic>(),true);foreach(var component in decorations){var graphic=component.Cast<UnityEngine.UI.Graphic>();if(graphic.GetComponentInParent(Il2CppType.Of<CoolButton>())==null)graphic.raycastTarget=false;}
   button.Btn.SetInteractable(true,true);
   if(button.Btn.ImgBackground!=null)button.Btn.ImgBackground.raycastTarget=true;
   button.Txt.raycastTarget=false;
   var start=old[0].transform.position;var step=old.Length>1?old[1].transform.position-start:new Vector3(0,-65,0);
   obj.transform.position=start;for(int i=0;i<old.Length;i++)old[i].transform.position=start+step*(i+1);
   var buttons=new Il2CppReferenceArray<MainMenuBtn>(old.Length+1);buttons[0]=button;for(int i=0;i<old.Length;i++)buttons[i+1]=old[i];obj.transform.SetSiblingIndex(0);__instance.Btns=buttons;
   Log.LogInfo("Coop local entry added to main menu.");
   if(Returning){Returning=false;Begin(__instance);}
  }catch(Exception e){Log.LogError("Coop menu creation failed: "+e);}
 }
 static bool CreditsInput(){return !CoopCredits.Active;}
 static bool MenuChoice(MainMenuOption __0){if(CoopCredits.Active)return false;if(NativeStarting)return true;
  if(Enabled&&(CoopCharacters.Selecting||ChoosingLevel||Perks))return false;
  if((int)__0==99){Begin(MainMenuUI.I);return false;}
  if(Enabled){Enabled=false;ChoosingLevel=false;Perks=false;NativeChoiceOverlay.Hide();RestoreSolo();}
  return true;
 }
 static bool MenuButtonClicked(MainMenuBtn __instance){
  if(CoopCredits.Active)return false;
  if(__instance.gameObject.name!="CoopLocalButton")return true;
  if(MainMenuUI.I!=null)MainMenuUI.I.SelectBtn((MainMenuOption)99);
  return false;
 }
 static bool IsCoopButton(CoolButton button){if(MainMenuUI.I==null||Enabled||!button.gameObject.activeInHierarchy)return false;var entry=button.GetComponentInParent(Il2CppType.Of<MainMenuBtn>())?.Cast<MainMenuBtn>();return entry!=null&&entry.gameObject.name=="CoopLocalButton"&&entry.Btn==button;}
 static bool CoopPointerEnter(CoolButton __instance){if(!IsCoopButton(__instance))return true;__instance._isPointerInside=true;if(__instance.IsInteractable())__instance.OnHover?.Invoke();return false;}
 static bool CoopPointerExit(CoolButton __instance){if(!IsCoopButton(__instance))return true;__instance._isPointerInside=false;__instance.OnHoverExit?.Invoke();return false;}
 static bool CoopPointerDown(CoolButton __instance,UnityEngine.EventSystems.PointerEventData __0){if(!IsCoopButton(__instance))return true;__instance._isPointerDown=__0.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left&&__instance.IsInteractable();return false;}
 static bool CoopPointerUp(CoolButton __instance,UnityEngine.EventSystems.PointerEventData __0){if(!IsCoopButton(__instance))return true;bool click=__instance._isPointerDown&&__instance._isPointerInside&&__instance.IsInteractable()&&__0.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left;__instance._isPointerDown=false;if(click)__instance.OnClicked?.Invoke();return false;}
 static void Begin(Component source){
  if(!CoopTestHarness.Active&&(TwoGamepads.GetPad(0)==null||TwoGamepads.GetPad(1)==null)){Log.LogWarning("Coop local requires two connected gamepads.");return;}
  Load();if(!Enabled){SaveMgr.I.LoadMeta();SeedMetaPath=SaveMgr.I.GetMetaSavePath(PlayerPrefsMgr.sSaveSlot);SeedBackupPath=SaveMgr.I.GetMetaSaveBackupPath(PlayerPrefsMgr.sSaveSlot);SoloMeta=MetaSaveData.I;SoloBattle=SaveMgr.I.BattleSave;SoloPaths=new[]{SaveMgr.kBattleSavePath,SaveMgr.kMetaSavePath,SaveMgr.kMetaSaveTempPath,SaveMgr.kMetaSaveBackupPath};}Enabled=true; Directory.CreateDirectory(Folder);RedirectStaticPaths();Awarded=false;Perks=false;ChoosingLevel=false;
  SaveMgr.kMetaSavePath=Path.Combine(Folder,Path.GetFileName(SeedMetaPath));SaveMgr.kMetaSaveBackupPath=Path.Combine(Folder,Path.GetFileName(SeedBackupPath));SaveMgr.kMetaSaveTempPath=Path.Combine(Folder,Path.GetFileName(SaveMgr.I.GetMetaSaveTempPath(PlayerPrefsMgr.sSaveSlot)));
  string ownMeta=Path.Combine(Folder,Path.GetFileName(SeedMetaPath)),ownBackup=Path.Combine(Folder,Path.GetFileName(SeedBackupPath));
  var meta=File.Exists(ownMeta)?SaveMgr.I.LoadMetaAtPath(ownMeta,ownBackup):File.Exists(SeedMetaPath)?SaveMgr.I.LoadMetaAtPath(SeedMetaPath,SeedBackupPath):new MetaSaveData();MetaSaveData.I=meta;SaveMgr.I.MetaSave=meta;CoopMeta=meta;meta.CharUnlockOrder.Clear();foreach(int unlocked in State.Unlocked)meta.CharUnlockOrder.Add((CharType)unlocked);
  for(int i=0;i<meta.Chars.Length;i++){var character=meta.Chars[i];character.IsUnlocked=State.Unlocked.Contains((int)character.Type);character.BonusStats=new Il2CppStructArray<int>((int)StatType.kNum);character.SetState(CharState.kIdle);}
  PopulateLevels();CoopCharacters.OpenStandalone(source);if(MainMenuUI.I!=null)MainMenuUI.I.gameObject.SetActive(false);
 }
 public static void Exit(){Save();Enabled=false;Preparing=false;ChoosingLevel=false;Perks=false;Returning=false;CoopCharacters.Selecting=false;NativeChoiceOverlay.Hide();RestoreSolo();if(MainMenuUI.I!=null)MainMenuUI.I.gameObject.SetActive(true);}
 public static bool Unlocked(CharType type){return !Enabled||State.Unlocked.Contains((int)type);}
 public static void ApplyStats(CharBattleInst character,int slot){if(!Enabled)return;
  // The menu's temporary character can precede the native battle-stat initialization.
  var stats=new Il2CppStructArray<int>((int)StatType.kNum);var initial=character.Stats??character.GetInfo().Stats;if(initial!=null)for(int i=0;i<Math.Min(stats.Length,initial.Length);i++)stats[i]=initial[i];character.Stats=stats;
  var seen=new Il2CppStructArray<int>((int)StatType.kNum);var oldSeen=character.LastSeenStats;if(oldSeen!=null)for(int i=0;i<Math.Min(seen.Length,oldSeen.Length);i++)seen[i]=oldSeen[i];character.LastSeenStats=seen;
  // Permanent bonuses are initial data, not an in-battle reward. AddStat also
  // reaches battle UI/singletons that do not exist at a fresh menu startup.
  var baseStats=new int[6];var infoStats=character.GetInfo().Stats;if(infoStats!=null)for(int i=0;i<Math.Min(6,infoStats.Length);i++)baseStats[i]=infoStats[i];
  var bonuses=CoopProgressRules.CharacterBonuses((int)character.Type,GetCharacterLevel(slot,character.Type),baseStats);
  for(int i=0;i<6;i++)character.Stats[i]+=State.Players[slot].Stats[i]+bonuses[i];}
 public static void CharactersChosen(CharType first,CharType second){
  First=first;Second=second;ChoosingLevel=true;LevelArmed=Time.unscaledTime+0.35f;LevelIndex=0;Gate.Reset();
  PopulateLevels();
 }
 static void PopulateLevels(){Levels.Clear();var objects=Resources.FindObjectsOfTypeAll(Il2CppType.Of<LevelInfo>());
  var seen=new HashSet<int>();for(int i=0;i<objects.Length;i++){var level=objects[i].Cast<LevelInfo>();if(seen.Add((int)level.Type))Levels.Add(level);}
  Levels.Sort((a,b)=>a.GetLevelNum().CompareTo(b.GetLevelNum()));
  if(Levels.Count==0)throw new InvalidOperationException("Level assets are not loaded.");
 }
 static bool LevelUnlocked(LevelInfo level){int index=Levels.IndexOf(level);return CoopProgressRules.CanUnlockStage(index,index>0&&State.Cleared.Contains((int)Levels[index-1].Type));}
 static bool NativeLevelUnlocked(LevelData __instance,ref bool __result){if(!Enabled)return true;__result=false;if(__instance.NGPlusLvl!=0)return false;for(int i=0;i<Levels.Count;i++)if(Levels[i].Type==__instance.Type){__result=LevelUnlocked(Levels[i]);return false;}if(Levels.Count==0)__result=__instance.Type==LevelMgr.kLevelOrder[0];return false;}
 static void StartRun(){Preparing=true;
  MetaSaveData.I.LastChar=First;MetaSaveData.I.LastLevel=Levels[LevelIndex].Type;MetaSaveData.I.LastLevelNGPlus=0;
  int level;State.Players[0].Levels.TryGetValue((int)First,out level);MetaSaveData.I.Chars[(int)First].Lvl=Math.Max(1,level);
  var placeholder=Character(First,0);CharBattleInst.I=placeholder;BattleSaveData.I=new BattleSaveData(placeholder);SaveMgr.I.BattleSave=BattleSaveData.I;
  ChoosingLevel=false;Preparing=true;LaunchStage=0;LaunchAt=Time.unscaledTime+3;Awarded=false;
  NativeChoiceOverlay.Loading(CoopLocalization.Text("Carregando partida coop"), CoopLocalization.Text("Os dois personagens estão sendo preparados."));SaveMgr.I.SaveMeta();NativeStarting=true;try{MainMenuUI.I.SelectBtn(MainMenuOption.kContinue);}finally{NativeStarting=false;}
 }
 static bool UseCoopMeta(SaveMgr __instance,ref bool __result){if(!Enabled||CoopMeta==null)return true;MetaSaveData.I=CoopMeta;__instance.MetaSave=CoopMeta;__result=__instance.MetaSave!=null;return false;}
 static bool UsePreparedBattle(SaveMgr __instance,ref bool __result){if(!Enabled)return true;if(Preparing&&SceneMgr.GetCurScene()!=GameScene.kGame){__result=false;return false;}if(__instance.BattleSave==null)return true;BattleSaveData.I=__instance.BattleSave;CharBattleInst.I=BattleSaveData.I.CurChar;__result=true;return false;}
 static void StartHealth(){if(!Enabled||BattleSaveData.I==null)return;UpgradeMgr.I.CalculateStats();BattleSaveData.I.CurHealth=UpgradeMgr.I.MaxHealth;}
 static void BattleCreated(){if(!Enabled||BattleSaveData.I==null)return;ApplyStats(BattleSaveData.I.CurChar,0);BattleSaveData.I.CurChar._hasProp=BattleSaveData.I.CurChar.GetPropArray();Log.LogInfo("Coop battle prepared through native launch flow: expected="+First+", actual="+BattleSaveData.I.CurChar.Type);}
 static void LaunchNative(){
  if(Time.unscaledTime<LaunchAt)return;if(Time.unscaledTime>=NextLaunchLog){Log.LogInfo("Coop preparation: scene="+SceneMgr.GetCurScene()+", stage="+LaunchStage+", base="+(BaseMgr.I==null?"none":BaseMgr.I.CurState.ToString())+", level UI="+(LevelSelectUI.I!=null));NextLaunchLog=Time.unscaledTime+5;}
  if(SceneMgr.GetCurScene()==GameScene.kGame){if(!CoopCharacters.IndependentReady)return;Preparing=false;NativeChoiceOverlay.Hide();return;}
  if(SceneMgr.GetCurScene()!=GameScene.kBase||BaseMgr.I==null||LevelSelectUI.I==null)return;
  if(LaunchStage==0){
   Log.LogInfo("Coop preparation: native character selection starts.");BaseMgr.I.SetState(BaseState.kSelectingChar,false);CharSelectUI.I.Activate();
   var characters=CharSelectUI.I.GetComponentsInChildren(Il2CppType.Of<CharSelectItem>(),true);bool found=false;
   for(int i=0;i<characters.Length;i++){var item=characters[i].Cast<CharSelectItem>();if(item.TgtInfo!=null&&item.TgtInfo.Type==First){CharSelectUI.I.ClickItem(item);found=true;break;}}
   if(!found)throw new InvalidOperationException("Chosen first character not found in native selector.");
   CharSelectUI.I.OnSelectClicked();LaunchStage=1;LaunchAt=Time.unscaledTime+1;Log.LogInfo("Coop preparation: native character accepted.");return;
  }
  if(BaseMgr.I.CurState==BaseState.kSelectingLoadout){LoadoutUI.I.OnConfirmClicked();LaunchAt=Time.unscaledTime+1;return;}
  if(LaunchStage==1){
   Log.LogInfo("Coop preparation: native level selection starts.");BaseMgr.I.SetState(BaseState.kSelectingLevel,false);LevelSelectUI.I.Activate();
   var items=LevelSelectUI.I.Items;for(int i=0;i<items.Length;i++)if(items[i]!=null&&items[i].TgtInfo!=null&&items[i].TgtInfo.Type==Levels[LevelIndex].Type){LevelSelectUI.I.ClickItem(items[i]);break;}
   LevelSelectUI.I._selectedLevel=Levels[LevelIndex];LevelSelectUI.I._selectedNGPlus=0;LevelSelectUI.I._selectedDiff=0;
   LaunchStage=2;Log.LogInfo("Coop preparation: native level accepted.");LevelSelectUI.I.OnPlayClicked();LaunchAt=Time.unscaledTime+2;return;
  }
  if(BaseMgr.I.CurState==BaseState.kEnteringLvl){Log.LogInfo("Coop preparation: completing native entry.");LevelMgr.I.SetLevel(Levels[LevelIndex].Type);SaveMgr.I.StartNewBattle();SaveMgr.I.SaveBattle();Log.LogInfo("Coop battle initialized: heroes="+BattleSaveData.I.Heroes.Count+", health="+BattleSaveData.I.CurHealth);BaseMgr.I.CompleteEnterLvl();LaunchAt=Time.unscaledTime+2;}
 }
 static void PrepareScene(){if(!Enabled)return;LevelMgr.I.SetLevel(BattleSaveData.I.CurLevel);BattleSaveData.I.CurChar._hasProp=BattleSaveData.I.CurChar.GetPropArray();var tiles=GameTileMgr.I;if(tiles!=null){if(GameCompleteObj.I==null&&tiles.GameComplete!=null){tiles.GameComplete.gameObject.SetActive(true);GameCompleteObj.I=tiles.GameComplete;}Log.LogInfo("Coop scene level selected; completion object="+(GameCompleteObj.I!=null));}}
 static bool CharacterProp(CharBattleInst __instance,CharProp __0,ref bool __result){if(!Enabled)return true;if(GameCompleteObj.I!=null&&GameCompleteObj.I._isRunning){__result=false;return false;}var props=__instance._hasProp;if(props==null){props=__instance.GetPropArray();__instance._hasProp=props;}int index=(int)__0;__result=props!=null&&index>=0&&index<props.Length&&props[index];return false;}
 static bool NoCityStat(ref int __result){if(!Enabled&&!CoopSpeedAudit.Comparing)return true;__result=0;return false;}
 static bool ProfilePath(ref string __result){if(!Enabled)return true;__result=Folder;return false;}
 static void RedirectPath(ref string __result){if(Enabled)__result=Path.Combine(Folder,Path.GetFileName(__result));}
 static void RedirectStaticPaths(){Directory.CreateDirectory(Folder);foreach(string name in new[]{"kBattleSavePath","kMetaSavePath","kMetaSaveTempPath","kMetaSaveBackupPath"}){var p=AccessTools.Property(typeof(SaveMgr),name);string path=(string)p.GetValue(null);p.SetValue(null,Path.Combine(Folder,string.IsNullOrEmpty(path)?name+".coop":Path.GetFileName(path)));}}
 static void RestoreSolo(){if(SoloMeta!=null){MetaSaveData.I=SoloMeta;SaveMgr.I.MetaSave=SoloMeta;SaveMgr.I.BattleSave=SoloBattle;BattleSaveData.I=SoloBattle;}if(SoloPaths!=null){SaveMgr.kBattleSavePath=SoloPaths[0];SaveMgr.kMetaSavePath=SoloPaths[1];SaveMgr.kMetaSaveTempPath=SoloPaths[2];SaveMgr.kMetaSaveBackupPath=SoloPaths[3];}}
 static void GainCharacterExperience(int slot,CharType type,int amount){var player=State.Players[slot];int current;player.Experience.TryGetValue((int)type,out current);player.Experience[(int)type]=Math.Max(0,current)+amount;player.Levels[(int)type]=CoopProgressRules.CharacterLevel(player.Experience[(int)type]);}
 static int HighestStage(){int highest=-1;for(int i=0;i<Levels.Count;i++)if(State.Cleared.Contains((int)Levels[i].Type))highest=Math.Max(highest,i);return highest;}
 static void RefreshUnlocks(){var roster=new List<CharInfo>();var chars=MetaSaveData.I.Chars;for(int i=0;i<chars.Length;i++)if(chars[i].GetInfo().IncludeInGame())roster.Add(chars[i].GetInfo());roster.Sort((a,b)=>((int)a.Type).CompareTo((int)b.Type));for(int i=0;i<roster.Count;i++)if(CoopProgressRules.CanUnlockCharacter(i,State.Proofs.Count,HighestStage(),Levels.Count)&&!State.Unlocked.Contains((int)roster[i].Type))State.Unlocked.Add((int)roster[i].Type);}
 public static string CharacterRequirement(CharType type){var roster=new List<CharType>();foreach(var meta in MetaSaveData.I.Chars)if(meta.GetInfo().IncludeInGame())roster.Add(meta.Type);roster.Sort((a,b)=>((int)a).CompareTo((int)b));int ordinal=roster.IndexOf(type);int stage=CoopProgressRules.RequiredStage(ordinal,LevelMgr.kLevelOrder.Length);return CoopLocalization.Text("Provas: ")+State.Proofs.Count+" / "+CoopProgressRules.RequiredProofs(ordinal)+CoopLocalization.Text(" • Vencer fase ")+(stage+1);}
 static bool UpgradeStageUnlocked(int rank){int required=CoopProgressRules.RequiredUpgradeStage(rank,Levels.Count);return Levels.Count>required&&State.Cleared.Contains((int)Levels[required].Type);}
 public static bool BuyUpgrade(int slot,int stat){if(slot<0||slot>1||stat<0||stat>5)return false;var player=State.Players[slot];int rank=player.Stats[stat];if(!CoopProgressRules.CanBuy(rank,player.Points)||!UpgradeStageUnlocked(rank))return false;player.Points-=CoopProgressRules.UpgradePrice(rank);player.Stats[stat]++;Save();return true;}
 public static int GetCharacterLevel(int slot,CharType type){int value;return State.Players[slot].Levels.TryGetValue((int)type,out value)?Math.Max(1,value):1;}
 public static string CharacterExperienceText(int slot,CharType type){int level=GetCharacterLevel(slot,type),experience;State.Players[slot].Experience.TryGetValue((int)type,out experience);if(level>=CoopProgressRules.MaxCharacterLevel)return CoopLocalization.Text("Nível ")+level+CoopLocalization.Text(" • Máximo");int progress=Math.Max(0,experience-CoopProgressRules.ExperienceForLevel(level));int needed=CoopProgressRules.ExperienceForLevel(level+1)-CoopProgressRules.ExperienceForLevel(level);return CoopLocalization.Text("Nível ")+level+" • XP "+progress+" / "+needed;}
 static void Victory(){
  if(!Enabled||Awarded||(CoopVitals.Active&&CoopVitals.Life.Defeated)||!BattleSaveData.I.CompletedLevel)return;Awarded=true;State.Wins++;
  int level=(int)BattleSaveData.I.CurLevel;bool fresh=!State.Cleared.Contains(level);if(fresh)State.Cleared.Add(level);
  for(int slot=0;slot<2;slot++)CoopRoundStats.Proofs[slot]=State.Proofs.Contains(level+":"+(int)(slot==0?First:Second))?0:1;bool newProof=false;foreach(var type in new[]{First,Second}){string proof=level+":"+(int)type;if(!State.Proofs.Contains(proof)){State.Proofs.Add(proof);newProof=true;}}
  for(int i=0;i<2;i++){CoopRoundStats.Points[i]=CoopProgressRules.VictoryPoints(fresh,newProof);CoopRoundStats.Experience[i]=CoopProgressRules.VictoryExperience(BattleSaveData.I.NumKills);State.Players[i].Points+=CoopRoundStats.Points[i];GainCharacterExperience(i,i==0?First:Second,CoopRoundStats.Experience[i]);}
  CoopRoundStats.Finish(true);State.LastResult=CoopPresentation.Capture(true,level,(int)First,(int)Second);RefreshUnlocks();Save();Log.LogInfo("Coop victory saved: wins="+State.Wins+", shared unlocks="+State.Unlocked.Count);
 }
 static void Defeat(){
  if(!Enabled||Awarded||Preparing||GameMgr.I==null||(CoopVitals.Active?!CoopVitals.Life.Defeated:GameMgr.I.CurState!=GameState.kGameOver))return;Awarded=true;State.Losses++;int reward=CoopProgressRules.DefeatPoints(BattleSaveData.I.NumKills);
  for(int i=0;i<2;i++){CoopRoundStats.Points[i]=reward;CoopRoundStats.Experience[i]=CoopProgressRules.DefeatExperience(BattleSaveData.I.NumKills);State.Players[i].Points+=reward;GainCharacterExperience(i,i==0?First:Second,CoopRoundStats.Experience[i]);}
  CoopRoundStats.Finish(false);State.LastResult=CoopPresentation.Capture(false,(int)BattleSaveData.I.CurLevel,(int)First,(int)Second);Save();Log.LogInfo("Coop defeat saved: losses="+State.Losses+", reward per player="+reward);
 }
 static bool ReturnToMenu(){if(!Enabled)return true;if(CoopPresentation.InterceptReturn())return false;CoopDecisions.AbortForSceneChange();Save();Returning=true;
  // Finish scene-owned animations before their player/ball transforms are destroyed.
  if(BallMgr.I!=null)MEC.Timing.KillCoroutines(BallMgr.I._ballAnim);
  if(GameCompleteObj.I!=null){MEC.Timing.KillCoroutines(GameCompleteObj.I._runAnim);MEC.Timing.KillCoroutines(GameCompleteObj.I._updateAnim);}
  if(LevelUpUI.I!=null)foreach(var property in typeof(LevelUpUI).GetProperties())if(property.PropertyType==typeof(MEC.CoroutineHandle)&&property.CanRead)MEC.Timing.KillCoroutines((MEC.CoroutineHandle)property.GetValue(LevelUpUI.I));
  CoopVitals.StopShots();NativeChoiceOverlay.Hide();SceneMgr.I.LoadScene(GameScene.kMainMenu,0f);return false;}
 public static void Update(){
  if(MainMenuUI.I!=null&&MainMenuUI.I.Btns!=null)foreach(var btn in MainMenuUI.I.Btns)if(btn!=null&&btn.gameObject.name=="CoopLocalButton"){btn.Option=(MainMenuOption)99;if(btn.Loc!=null)btn.Loc.enabled=false;if(btn.Txt!=null&&btn.Txt.text!=CoopLocalization.Text("Coop local"))btn.Txt.text=CoopLocalization.Text("Coop local");}
  if(!Enabled)return;if(Preparing){try{LaunchNative();}catch(Exception e){Preparing=false;Log.LogError("Coop preparation failed: "+e);NativeChoiceOverlay.Show(CoopLocalization.Text("Não foi possível preparar a partida"),CoopLocalization.Text("A preparação foi interrompida."));}return;}if(SceneMgr.GetCurScene()!=GameScene.kMainMenu||!Application.isFocused)return;
  if(CoopTestHarness.Active||CoopPresentation.Challenges)return;
  if(CoopCharacters.Selecting){
   var a=TwoGamepads.GetPad(0);var b=TwoGamepads.GetPad(1);
   if((a!=null&&a.x.justPressed)||(b!=null&&b.x.justPressed)){CoopPresentation.OpenChallenges();return;}if((a!=null&&a.y.justPressed)||(b!=null&&b.y.justPressed)){Perks=true;CoopCharacters.Selecting=false;Gate.Reset();}
   return;
  }
  if(Perks){
   for(int slot=0;slot<2;slot++){
    var pad=TwoGamepads.GetPad(slot);if(pad==null)continue;
    if(pad.b.justPressed){Perks=false;CoopCharacters.OpenStandalone(MainMenuUI.I);return;}
    float x=pad.leftStick.horizontal.value;if(Math.Abs(x)<0.4f)Next[slot]=0;
    if(Math.Abs(x)>0.55f&&Time.unscaledTime>=Next[slot]){Gate.Move(slot,x>0?1:-1,6);Next[slot]=Time.unscaledTime+0.22f;}
    if(pad.a.justPressed){BuyUpgrade(slot,Gate.Index[slot]);}
   }
   return;
  }
  if(!ChoosingLevel||Time.unscaledTime<LevelArmed)return;
  bool moved=false,confirm=false;
  for(int slot=0;slot<2;slot++){
   var pad=TwoGamepads.GetPad(slot);if(pad==null)continue;
   if(pad.b.justPressed){ChoosingLevel=false;CoopCharacters.OpenStandalone(MainMenuUI.I);return;}
   float axis=pad.leftStick.horizontal.value,vertical=pad.leftStick.vertical.value;
   int step=Math.Abs(vertical)>Math.Abs(axis)?(vertical>0?-4:4):(axis>0?1:-1);
   float value=Math.Max(Math.Abs(axis),Math.Abs(vertical));if(value<0.4f)Next[slot]=0;
   if(value>0.55f&&Time.unscaledTime>=Next[slot]){if(!moved){LevelIndex=(LevelIndex+step+Levels.Count)%Levels.Count;moved=true;}Next[slot]=Time.unscaledTime+0.22f;}
   confirm|=pad.a.justPressed;
  }
  if(confirm&&LevelUnlocked(Levels[LevelIndex]))StartRun();
 }
 public static void Draw(){
  if(!Enabled)return;if(Preparing){NativeChoiceOverlay.Loading(CoopLocalization.Text("Carregando partida coop"),CoopLocalization.Text("Os dois personagens estão sendo preparados."));return;}
  if(Perks){
   NativeChoiceOverlay.Show(CoopLocalization.Text("Melhorias permanentes por jogador"),CoopLocalization.Text("A / X: comprar • B / círculo: voltar • Custos e requisitos aumentam a cada nível."));
   string[] labels={CoopLocalization.Text("Resistência"),CoopLocalization.Text("Força"),CoopLocalization.Text("Liderança"),CoopLocalization.Text("Velocidade"),CoopLocalization.Text("Destreza"),CoopLocalization.Text("Inteligência")};
   for(int slot=0;slot<2;slot++){var p=State.Players[slot];int rank=p.Stats[Gate.Index[slot]];string name=labels[Gate.Index[slot]]+" +"+rank;if(rank>=CoopProgressRules.MaxPermanentStat)name+=CoopLocalization.Text("\nNível máximo");else name+=CoopLocalization.Text("\nCusto: ")+CoopProgressRules.UpgradePrice(rank)+CoopLocalization.Text(" • Pontos: ")+p.Points+"\n"+(UpgradeStageUnlocked(rank)?CoopLocalization.Text("Requisito cumprido: fase "):CoopLocalization.Text("Bloqueado: vencer fase "))+(CoopProgressRules.RequiredUpgradeStage(rank,Levels.Count)+1);NativeChoiceOverlay.Card(slot,name,false,null);}
  }else if(ChoosingLevel){
   var level=Levels[LevelIndex];NativeChoiceOverlay.Show(CoopLocalization.Text("Escolham a fase"),LevelUnlocked(level)?CoopLocalization.Text("Os dois controles movem a mesma seleção • A / X: iniciar • B / círculo: voltar."):CoopLocalization.Text("Fase bloqueada: concluam a anterior."));
   NativeChoiceOverlay.SinglePanel();NativeChoiceOverlay.Card(0,CoopLocalization.StageName(level),false,level.SprPreview);
   var sprites=new Sprite[Levels.Count];var available=new bool[Levels.Count];for(int i=0;i<Levels.Count;i++){sprites[i]=Levels[i].SprIcon;available[i]=LevelUnlocked(Levels[i]);}NativeChoiceOverlay.CharacterGrid(0,sprites,available,LevelIndex,true);
  }
 }
}





































