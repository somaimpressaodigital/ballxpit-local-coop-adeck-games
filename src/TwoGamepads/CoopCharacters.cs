using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Rewired;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

public static class CoopCharacters {
 public static bool Selecting;
 static bool Launching;
 static ManualLogSource Log;
 static readonly List<CharSelectItem> Items=new List<CharSelectItem>();
 static readonly List<CharInfo> Standalone=new List<CharInfo>();
 static int[] Index={0,0};static bool[] Ready={false,false};static float[] NextMove={0,0};
 static CharType? SecondChoice;
 static CharBattleInst SecondCharacter;
 static CharBattleInst FirstCharacter;
 public static CharBattleInst CharacterFor(int slot){return slot==1?SecondCharacter:FirstCharacter;}
 static PropertyInfo[] StatProperties;
 static object[] FirstStats,SecondStats;
 static bool Synchronizing;
 static Il2CppSystem.Collections.Generic.List<PassiveInst> FirstPassives,SecondPassives;
 static Player SecondRuntime;
 static float ArmedAt;
 static HeroInst Starter;
 static HeroType FirstStarter;
 static FieldInfo Second,SecondController;
 static MethodInfo Enter,Exit;
 static PropertyInfo InContext;
 static Dictionary<IntPtr,HeroInst> Clones;
 static FieldInfo InventoryHeroes,InventoryDirty;
 static FieldInfo InventoryFirstHeroes,InventoryDepth;
 static Il2CppSystem.Collections.Generic.List<HeroInst> FirstHeroes;
 static Il2CppSystem.Collections.Generic.List<HeroInst> OwnHeroes;
 static bool IndependentInventory;
 public static void Install(Harmony h,Type controller,ManualLogSource log){
  Log=log;
  h.Patch(AccessTools.Method(typeof(BallMgr),"CreateBall"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(PrepareBall)));
  h.Patch(AccessTools.Method(typeof(GameMgr),"Start"),postfix:new HarmonyMethod(typeof(CoopCharacters),nameof(PrepareHeroes)));
  var context=controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerContext",true);
  var shootingRuntime=controller.Assembly.GetType("BALLxPITLocalCoop.CharacterRuntimeInfo",true);
  h.Patch(AccessTools.PropertyGetter(shootingRuntime,"AlwaysShoots"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(SecondAlwaysShoots)));
  InContext=AccessTools.Property(context,"IsPlayerTwoContext");
  Enter=AccessTools.Method(context,"EnterPlayerTwoContext");Exit=AccessTools.Method(context,"ExitContext");
  Second=AccessTools.Field(controller,"_playerTwo");SecondController=AccessTools.Field(controller,"_playerTwoController");
  var inventory=controller.Assembly.GetType("BALLxPITLocalCoop.PlayerTwoHeroInventory",true);
  InventoryDepth=AccessTools.Field(inventory,"_contextDepth");h.Patch(AccessTools.Method(inventory,"EnterPlayerTwoInventory"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(ProtectFirstInventory)));h.Patch(AccessTools.Method(inventory,"ExitPlayerTwoInventory"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(ProtectFirstExit)));
  Clones=(Dictionary<IntPtr,HeroInst>)AccessTools.Field(inventory,"_clones").GetValue(null);
  InventoryHeroes=AccessTools.Field(inventory,"_playerTwoHeroes");InventoryDirty=AccessTools.Field(inventory,"_inventoryDirty");
  InventoryFirstHeroes=AccessTools.Field(inventory,"_playerOneHeroes");
  h.Patch(AccessTools.Method(typeof(CharSelectUI),"Activate"),postfix:new HarmonyMethod(typeof(CoopCharacters),nameof(Open)));
  h.Patch(AccessTools.Method(typeof(CharSelectUI),"Deactivate"),postfix:new HarmonyMethod(typeof(CoopCharacters),nameof(Close)));
  h.Patch(AccessTools.Method(typeof(CharSelectUI),"MyUpdate"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(UpdateSelection)));
  h.Patch(AccessTools.Method(typeof(CharSelectUI),"OnSelectClicked"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(BlockNativeConfirm)));


  h.Patch(AccessTools.Method(controller,"SpawnNativePlayerTwo"),postfix:new HarmonyMethod(typeof(CoopCharacters),nameof(Spawn)));
  // These singleton fields are read directly by native code. Swap their actual
  // values at context boundaries rather than patching managed field accessors.
  StatProperties=Array.FindAll(typeof(UpgradeMgr).GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly),p=>p.Name!="TgtXP"&&p.CanRead&&p.CanWrite&&(p.PropertyType==typeof(float)||p.PropertyType==typeof(int)||p.PropertyType==typeof(bool)));
  foreach(string method in new[]{"EnterPlayerTwoContext","EnterNearestPlayerContext","ExitContext","RestoreOriginalSingleton","RestoreOriginalPlayerContext"})
   h.Patch(AccessTools.Method(context,method),postfix:new HarmonyMethod(typeof(CoopCharacters),nameof(SyncCharacter)));
  h.Patch(AccessTools.Method(typeof(UpgradeMgr),"CalculateStats"),postfix:new HarmonyMethod(typeof(CoopCharacters),nameof(CalculatedStats)));
  h.Patch(AccessTools.Method(inventory,"GetOrCreate"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(StarterGetter)));
  h.Patch(AccessTools.Method(inventory,"EnsureInventoryReady"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(EnsureInventory)));
  h.Patch(AccessTools.Method(inventory,"InvalidateForHeroChanges"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(KeepIndependentInventory)));
  h.Patch(AccessTools.Method(inventory,"IsPlayerTwoHero"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(OwnHero)));
  h.Patch(AccessTools.Method(inventory,"Initialize"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(ResetInventory)));
  var runtime=controller.Assembly.GetType("BALLxPITLocalCoop.CharacterRuntimeInfo",true);
  h.Patch(AccessTools.PropertyGetter(runtime,"HasMultipleCharacters"),prefix:new HarmonyMethod(typeof(CoopCharacters),nameof(MultipleGetter)));
 }
 static void Open(CharSelectUI __instance){if(CoopMode.Preparing)return;
  Standalone.Clear();
  Selecting=false;IndependentInventory=false;SecondCharacter=null;FirstCharacter=null;SecondStats=null;FirstStats=null;SecondRuntime=null;Starter=null;SecondChoice=null;
  if(__instance.IsSelectingFusion||TwoGamepads.GetPad(1)==null)return;
  Items.Clear();
  var components=__instance.GetComponentsInChildren(Il2CppType.Of<CharSelectItem>(),true);
  for(int i=0;i<components.Length;i++){
   var item=components[i].Cast<CharSelectItem>();
   if(item.TgtInfo==null)continue;
   var meta=item.TgtInfo.GetInst();
   if(meta!=null&&item.TgtInfo.IncludeInGame())Items.Add(item);
  }
  if(Items.Count==0){Log.LogWarning("No available characters for coop selection.");return;}
  Items.Sort((a,b)=>((int)a.TgtInfo.Type).CompareTo((int)b.TgtInfo.Type));
  Index[0]=Index[1]=0;Ready[0]=Ready[1]=false;NextMove[0]=NextMove[1]=0;
  ArmedAt=Time.unscaledTime+0.3f;
  NativeChoiceOverlay.Create(__instance);
  Selecting=true;Log.LogInfo("Simultaneous character selection opened: "+Items.Count+" unlocked, available characters.");
 }
 static void Close(){if(CoopMode.Preparing)return;Selecting=false;Launching=false;NativeChoiceOverlay.Hide();}
 static int Count{get{return Standalone.Count>0?Standalone.Count:Items.Count;}}
 static CharInfo Info(int index){return Standalone.Count>0?Standalone[index]:Items[index].TgtInfo;}
 static bool Allowed(int index){var info=Info(index);if(CoopMode.Enabled)return CoopMode.Unlocked(info.Type);var meta=info.GetInst();return meta!=null&&meta.IsUnlocked&&meta.CanBeSentToBattle();}
 public static void OpenStandalone(Component source){
  Selecting=false;IndependentInventory=false;SecondCharacter=null;FirstCharacter=null;SecondRuntime=null;SecondStats=null;FirstStats=null;SecondChoice=null;Starter=null;
  Items.Clear();Standalone.Clear();
  var chars=MetaSaveData.I.Chars;for(int i=0;i<chars.Length;i++){var info=chars[i].GetInfo();if(info!=null&&info.IncludeInGame())Standalone.Add(info);}
  Standalone.Sort((a,b)=>((int)a.Type).CompareTo((int)b.Type));
  Index[0]=0;Index[1]=Math.Min(1,Standalone.Count-1);Ready[0]=Ready[1]=false;NextMove[0]=NextMove[1]=0;ArmedAt=Time.unscaledTime+0.3f;
  NativeChoiceOverlay.Create(source);Selecting=Standalone.Count>0;
 }
 public static void UpdateStandalone(){if(!CoopTestHarness.Active&&CoopMode.Enabled&&Selecting)UpdateSelection(null);}
 static bool BlockNativeConfirm(){return !Selecting||Launching;}
 static bool UpdateSelection(CharSelectUI __instance){if(CoopMode.Preparing)return false;
  if(!Selecting)return true;
  if(!Application.isFocused||Time.unscaledTime<ArmedAt)return false;
  for(int slot=0;slot<2;slot++){
   var pad=TwoGamepads.GetPad(slot);
   if(pad==null){Ready[slot]=false;continue;}
   if(pad.b.justPressed){if(CoopMode.Enabled&&slot==0&&!Ready[slot]){CoopMode.Exit();return false;}Ready[slot]=false;}
   var stick=pad.leftStick.value;float axis=Math.Abs(stick.x)>Math.Abs(stick.y)?stick.x:stick.y;
   int step=Math.Abs(stick.x)>Math.Abs(stick.y)?(stick.x>0?1:-1):(stick.y>0?-8:8);
   if(Math.Abs(axis)<0.45f)NextMove[slot]=0;
   if(!Ready[slot]&&Math.Abs(axis)>0.55f&&Time.unscaledTime>=NextMove[slot]){
    Index[slot]=(Index[slot]+step+Count)%Count;
    NextMove[slot]=Time.unscaledTime+0.22f;
   }
   if(pad.a.justPressed&&Allowed(Index[slot]))Ready[slot]=true;
  }
  if(Ready[0]&&Ready[1])Launch(__instance);
  return false;
 }
 static void Launch(CharSelectUI ui){
  SecondChoice=Info(Index[1]).Type;
  if(CoopMode.Enabled){Selecting=false;NativeChoiceOverlay.Hide();CoopMode.CharactersChosen(Info(Index[0]).Type,SecondChoice.Value);return;}
  Launching=true;
  try{
   ui.ClickItem(Items[Index[0]]);
   Log.LogInfo("Coop choices: P1="+Items[Index[0]].TgtInfo.Type+"; P2="+SecondChoice.Value);
   ui.OnSelectClicked();Selecting=false;
  }finally{Launching=false;}
 }
 static bool CharacterGetter(ref CharBattleInst __result){
  if(SecondCharacter==null||!(bool)InContext.GetValue(null))return true;
  __result=SecondCharacter;return false;
 }
 static object[] CaptureStats(){var values=new object[StatProperties.Length];for(int i=0;i<values.Length;i++)values[i]=StatProperties[i].GetValue(UpgradeMgr.I);return values;}
 static void ApplyStats(object[] values){if(values==null||UpgradeMgr.I==null)return;for(int i=0;i<values.Length;i++)StatProperties[i].SetValue(UpgradeMgr.I,values[i]);}
 static bool WasSecond;
 static void CaptureBoundary(){if(SecondCharacter==null||UpgradeMgr.I==null||BattleSaveData.I==null)return;bool second=(bool)InContext.GetValue(null);if(second)SecondStats=CaptureStats();else if(FirstCharacter!=null&&BattleSaveData.I.CurChar!=null&&BattleSaveData.I.CurChar.Pointer==FirstCharacter.Pointer)FirstStats=CaptureStats();}
 static void SyncCharacter(){
  if(Synchronizing||SecondCharacter==null||BattleSaveData.I==null)return;
  bool second=(bool)InContext.GetValue(null);
  if(second==WasSecond)return;
  Synchronizing=true;
  try{
   if(second){if(FirstCharacter!=null&&BattleSaveData.I.CurChar!=null&&BattleSaveData.I.CurChar.Pointer==FirstCharacter.Pointer){FirstPassives=BattleSaveData.I.Passives;FirstStats=CaptureStats();}BattleSaveData.I.CurChar=SecondCharacter;CharBattleInst.I=SecondCharacter;BattleSaveData.I.Passives=SecondPassives;if(OwnHeroes!=null)BattleSaveData.I.Heroes=OwnHeroes;if(SecondStats==null){UpgradeMgr.I.CalculateStats();SecondStats=CaptureStats();}else ApplyStats(SecondStats);}
   else{SecondStats=CaptureStats();BattleSaveData.I.CurChar=FirstCharacter;CharBattleInst.I=FirstCharacter;BattleSaveData.I.Passives=FirstPassives;if(FirstHeroes!=null){BattleSaveData.I.Heroes=FirstHeroes;InventoryFirstHeroes.SetValue(null,FirstHeroes);}ApplyStats(FirstStats);}
   WasSecond=second;
  }finally{Synchronizing=false;}
 }
 public static void PrepareHero(HeroInst hero){if(hero==null||hero._hasSpecial!=null&&hero._hasStatusEff!=null&&hero._hasAOE!=null)return;var special=new Il2CppStructArray<bool>((int)BallSpecialType.kNum);var status=new Il2CppStructArray<bool>((int)StatusEffectType.kNum);var aoe=new Il2CppStructArray<bool>((int)BallAOEType.kNum);int count=hero.GetNumInfo();for(int i=0;i<count;i++){var info=hero.GetInfo(i);if(info==null)continue;if(info.Specials!=null)for(int j=0;j<info.Specials.Count;j++)special[(int)info.Specials[j]]=true;if(info.HitEffects!=null)for(int j=0;j<info.HitEffects.Count;j++)status[(int)info.HitEffects[j]]=true;if(info.AOETypes!=null)for(int j=0;j<info.AOETypes.Count;j++)aoe[(int)info.AOETypes[j]]=true;}hero._hasSpecial=special;hero._hasStatusEff=status;hero._hasAOE=aoe;}
 static void PrepareBall(HeroInst __0){if(CoopMode.Enabled||IndependentReady)PrepareHero(__0);}
 static void PrepareHeroes(){if(!CoopMode.Enabled&& !IndependentReady)return;var list=BattleSaveData.I?.Heroes;if(list!=null)for(int i=0;i<list.Count;i++)PrepareHero(list[i]);}
 static void CalculatedStats(){PrepareHeroes();if(SecondCharacter!=null&&(bool)InContext.GetValue(null))SecondStats=CaptureStats();CoopVitals.RefreshStats();}
 static bool MultipleGetter(ref bool __result){
  if(SecondRuntime==null||!(bool)InContext.GetValue(null))return true;
  __result=SecondRuntime.CharControllers!=null&&SecondRuntime.CharControllers.Length>1;return false;
 }
 static bool StarterGetter(HeroInst __0,ref HeroInst __result){
  if(Starter==null||__0==null||__0.Type!=FirstStarter)return true;
  Clones[__0.Pointer]=Starter;__result=Starter;return false;
 }
 static bool EnsureInventory(){if(!IndependentInventory)return true;InventoryDirty.SetValue(null,false);InventoryHeroes.SetValue(null,OwnHeroes);return false;}
 static void ProtectFirstInventory(){if(!IndependentInventory||FirstHeroes==null||BattleSaveData.I==null)return;InventoryFirstHeroes.SetValue(null,FirstHeroes);if((int)InventoryDepth.GetValue(null)==0)BattleSaveData.I.Heroes=FirstHeroes;}
 static void ProtectFirstExit(){if(IndependentInventory&&FirstHeroes!=null)InventoryFirstHeroes.SetValue(null,FirstHeroes);}
 static bool KeepIndependentInventory(){if(!IndependentInventory)return true;InventoryDirty.SetValue(null,false);return false;}
 static void ResetInventory(){IndependentInventory=false;OwnHeroes=null;}
 static bool OwnHero(HeroInst __0,ref bool __result){
  if(!IndependentInventory||OwnHeroes==null||__0==null)return true;
  for(int i=0;i<OwnHeroes.Count;i++)if(OwnHeroes[i].Pointer==__0.Pointer){__result=true;return false;}
  return true;
 }
 public static bool IndependentReady {get{return SecondCharacter!=null&&SecondRuntime!=null;}}
 public static Il2CppSystem.Collections.Generic.List<HeroInst> HeroesFor(int slot){if(slot==1)return OwnHeroes;return FirstHeroes??InventoryFirstHeroes?.GetValue(null) as Il2CppSystem.Collections.Generic.List<HeroInst>??BattleSaveData.I?.Heroes;}
 public static Il2CppSystem.Collections.Generic.List<PassiveInst> PassivesFor(int slot){return slot==1?SecondPassives:FirstPassives??BattleSaveData.I?.Passives;}
 static bool SecondAlwaysShoots(ref bool __result){if(!IndependentReady)return true;__result=SecondCharacter.HasProp(CharProp.kAlwaysShoot);return false;}
 public static void ForSecond(Action action){
  if(!IndependentReady)throw new InvalidOperationException("Second character is not initialized.");
  object previous=Enter.Invoke(null,new object[]{SecondRuntime});
  try{action();}finally{Exit.Invoke(null,new[]{previous});}
 }
 static void Spawn(object __instance){
  if(!SecondChoice.HasValue)return;
  var player=Second.GetValue(__instance) as Player;if(player==null)return;
  object previous=null;
  var old=player.CharControllers;
  try{
   SecondCharacter=CoopMode.Character(SecondChoice.Value,1);
   FirstCharacter=BattleSaveData.I.CurChar;FirstHeroes=BattleSaveData.I.Heroes;FirstPassives=BattleSaveData.I.Passives;
   SecondPassives=new Il2CppSystem.Collections.Generic.List<PassiveInst>();
   var passives=BattleSaveData.I.Passives;
   if(passives!=null)for(int i=0;i<passives.Count;i++)SecondPassives.Add(new PassiveInst(passives[i]));
   WasSecond=false;
   previous=Enter.Invoke(null,new object[]{player});
   var info=SecondCharacter.GetInfo();
   int count=info.PrefabCharSecond!=null?2:1;
   var replacements=new Il2CppReferenceArray<PlayerCharController>(count);
   for(int i=0;i<count;i++){
    replacements[i]=SecondCharacter.CreateCharController(player,i);
    if(replacements[i]==null)throw new InvalidOperationException("Character controller creation returned null.");
   }
   player.CharControllers=replacements;
   SecondRuntime=player;
   SecondController.SetValue(__instance,replacements[0]);
   if(old!=null)for(int i=0;i<old.Length;i++)if(old[i]!=null){old[i].gameObject.SetActive(false);UnityEngine.Object.Destroy(old[i].gameObject);}
   var primary=BattleSaveData.I.Heroes;
   var ownStarter=info.StarterUpgrade?.ToHero();
   if(primary!=null&&primary.Count>0&&ownStarter!=null){
    FirstStarter=primary[0].Type;var ownHeroes=(Il2CppSystem.Collections.Generic.List<HeroInst>)InventoryHeroes.GetValue(null);
    OwnHeroes=ownHeroes;IndependentInventory=true;ownHeroes.Clear();BattleSaveData.I.Heroes=ownHeroes;UpgradeMgr.I.AddHero(ownStarter.Type,-1);Starter=ownHeroes[0];
    Log.LogInfo("P2 starter initialized through native AddHero: "+Starter.Type+", components="+Starter.CombinedHeroes.Count+", special cache="+(Starter._hasSpecial==null?-1:Starter._hasSpecial.Length));
   }
   IndependentInventory=true;
   OwnHeroes=(Il2CppSystem.Collections.Generic.List<HeroInst>)InventoryHeroes.GetValue(null);
   UpgradeMgr.I.CalculateStats();
   Log.LogInfo("P2 independent character initialized: "+SecondChoice.Value+", controllers="+count);
  }catch(Exception e){SecondCharacter=null;Starter=null;Log.LogError("Independent P2 initialization failed: "+e);}
  finally{if(previous!=null)Exit.Invoke(null,new[]{previous});}
 }
 static string Name(CharSelectItem item){return item.TgtInfo.GetNameSlug();}
 static readonly Dictionary<CharType,string> Profiles=new Dictionary<CharType,string>();
 public static void RefreshLocale(){Profiles.Clear();}
 public static string Profile(CharInfo info){
  string cached;if(Profiles.TryGetValue(info.Type,out cached))return cached;
  string gameplay=info.GameplayDesc??"";
  try{var text=CoopLocalization.Translate(info.GetGameplayDescSlug());if(!string.IsNullOrEmpty(text)&&!text.Contains("Missing Translation"))gameplay=text;}catch{}
  var good=new List<string>();var bad=new List<string>();
  var labels=new[]{CoopLocalization.Text("Resistência"),CoopLocalization.Text("Força"),CoopLocalization.Text("Liderança"),CoopLocalization.Text("Velocidade"),CoopLocalization.Text("Destreza"),CoopLocalization.Text("Inteligência")};
  // Compare the authored stats against the roster, rather than inventing traits.
  for(int stat=0;stat<6;stat++){
   float total=0;int n=0;foreach(var meta in MetaSaveData.I.Chars){var other=meta.GetInfo();if(other!=null&&other.IncludeInGame()&&other.Stats!=null&&other.Stats.Length>stat){total+=other.Stats[stat];n++;}}
   if(n==0||info.Stats==null||info.Stats.Length<=stat)continue;
   float delta=info.Stats[stat]-total/n;
   if(delta>=1)good.Add(labels[stat]+" "+info.Stats[stat]);else if(delta<=-1)bad.Add(labels[stat]+" "+info.Stats[stat]);
  }
  string result=gameplay+CoopLocalization.Text("\nVantagens: ")+(good.Count==0?CoopLocalization.Text("atributos equilibrados"):string.Join(", ",good))+CoopLocalization.Text("\nDesvantagens: ")+(bad.Count==0?CoopLocalization.Text("sem atributo abaixo da média"):string.Join(", ",bad))+CoopLocalization.Text(" (comparação com o elenco).");
  Profiles[info.Type]=result;return result;
 }
 public static void Draw(){
  if(!Selecting||Count==0)return;
  NativeChoiceOverlay.Show(CoopLocalization.Text("Escolham seus personagens"),CoopMode.Enabled?CoopLocalization.Text("Prova: primeira vitória de um personagem em uma fase • A / X: confirmar • Y / triângulo: melhorias."):CoopLocalization.Text("A fase avança quando os dois confirmarem. Somente personagens desbloqueados."));
  var sprites=new Sprite[Count];var unlocked=new bool[Count];for(int i=0;i<Count;i++){sprites[i]=Info(i).GetIconSprite();unlocked[i]=Allowed(i);}
  for(int slot=0;slot<2;slot++){
   var info=Info(Index[slot]);string name=info.Name;
   try{var translated=CoopLocalization.Translate(info.GetNameSlug());if(!string.IsNullOrEmpty(translated)&&!translated.Contains("Missing Translation"))name=translated;}catch{}
   if(CoopMode.Enabled)name+="\n"+CoopMode.CharacterExperienceText(slot,info.Type);
   NativeChoiceOverlay.Card(slot,unlocked[Index[slot]]?name:(CoopMode.Enabled?CoopLocalization.Text("Bloqueado\n")+CoopMode.CharacterRequirement(info.Type):CoopLocalization.Text("Bloqueado")),Ready[slot],info.GetIconSprite());
   NativeChoiceOverlay.CharacterGrid(slot,sprites,unlocked,Index[slot],false,Profile(info));
  }
 }
}













