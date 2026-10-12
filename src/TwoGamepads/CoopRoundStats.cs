using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
public static class CoopRoundStats {
 public static readonly long[] Damage={0,0};public static readonly int[] Kills={0,0},Experience={0,0},Points={0,0},Proofs={0,0};public static long TeamDamage;public static int TeamKills;public static bool Complete,Victory;
 static int AccountedKills;static readonly Dictionary<IntPtr,int> LastDamager=new Dictionary<IntPtr,int>();
 static readonly Dictionary<IntPtr,int> Balls=new Dictionary<IntPtr,int>();
 static readonly Dictionary<IntPtr,int> Heroes=new Dictionary<IntPtr,int>();static int Source=-1,StatusDepth;static PropertyInfo Context;
 static MethodInfo Enter,Exit;static readonly System.Collections.Generic.HashSet<IntPtr> Dead=new System.Collections.Generic.HashSet<IntPtr>();
 sealed class Scope {public int Old;public bool Status,Closed,Scoped;public object Previous;public void Close(){if(!Closed){Closed=true;if(Scoped)Exit.Invoke(null,new[]{Previous});Source=Old;if(Status)StatusDepth--;}}}
 sealed class Hit {public int Before,Owner,Old;}
 public static void Reset(){Array.Clear(Damage,0,2);Array.Clear(Kills,0,2);Array.Clear(Experience,0,2);Array.Clear(Points,0,2);Array.Clear(Proofs,0,2);TeamDamage=0;TeamKills=0;Complete=false;Victory=false;Heroes.Clear();Balls.Clear();Dead.Clear();LastDamager.Clear();AccountedKills=0;Source=-1;StatusDepth=0;CoopPresentation.ResetRound();}
 public static void Install(Harmony h,Type controller){Context=AccessTools.Property(controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerContext"),"IsPlayerTwoContext");
  var context=controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerContext");Enter=AccessTools.Method(context,"EnterPlayerTwoContext");Exit=AccessTools.Method(context,"ExitContext");
  foreach(var method in typeof(GridMgr).GetMethods())if(method.Name.StartsWith("Damage")||method.Name=="ApplyHitEffects")h.Patch(method,prefix:new HarmonyMethod(typeof(CoopRoundStats),nameof(DamageCall)),finalizer:new HarmonyMethod(typeof(CoopRoundStats),nameof(EndScope)));
  foreach(var type in typeof(GridMgr).GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic))if(type.Name.Contains("DamagePieceRoutine"))h.Patch(AccessTools.Method(type,"MoveNext"),prefix:new HarmonyMethod(typeof(CoopRoundStats),nameof(Routine)),finalizer:new HarmonyMethod(typeof(CoopRoundStats),nameof(EndScope)));
  h.Patch(AccessTools.Method(typeof(GridPieceInst),"OnKilled"),prefix:new HarmonyMethod(typeof(CoopRoundStats),nameof(KillOwner)),postfix:new HarmonyMethod(typeof(CoopRoundStats),nameof(KillFinished)));h.Patch(AccessTools.Method(typeof(GridPieceObj),"DropDeathStuff"),prefix:new HarmonyMethod(typeof(CoopRoundStats),nameof(DeathOwner)),postfix:new HarmonyMethod(typeof(CoopRoundStats),nameof(KillFinished)));
  h.Patch(AccessTools.Method(typeof(BallMgr),"CreateBall"),postfix:new HarmonyMethod(typeof(CoopRoundStats),nameof(Created)));
  h.Patch(AccessTools.Method(typeof(BallMgr),"MoveBall"),prefix:new HarmonyMethod(typeof(CoopRoundStats),nameof(Moving)),finalizer:new HarmonyMethod(typeof(CoopRoundStats),nameof(EndScope)));
  h.Patch(AccessTools.Method(typeof(GridMgr),"UpdatePieceStatusEffects"),prefix:new HarmonyMethod(typeof(CoopRoundStats),nameof(StatusTick)),finalizer:new HarmonyMethod(typeof(CoopRoundStats),nameof(EndScope)));
  h.Patch(AccessTools.Method(typeof(GridPieceInst),"ApplyStatusEffect"),prefix:new HarmonyMethod(typeof(CoopRoundStats),nameof(StatusSource)));
  h.Patch(AccessTools.Method(typeof(GridPieceInst),"Damage"),prefix:new HarmonyMethod(typeof(CoopRoundStats),nameof(Before)),postfix:new HarmonyMethod(typeof(CoopRoundStats),nameof(After)));
 }
 static int Current(){return Context!=null&&(bool)Context.GetValue(null)?1:0;}
 public static int HeroOwner(HeroInst hero){if(hero==null)return -1;int owner;for(int slot=0;slot<2;slot++){var list=CoopCharacters.HeroesFor(slot);if(list==null)continue;for(int i=0;i<list.Count;i++){bool found=list[i].Pointer==hero.Pointer;if(found){Heroes[hero.Pointer]=slot;return slot;}}}return Heroes.TryGetValue(hero.Pointer,out owner)?owner:-1;}
 static void Created(HeroInst __0,BallObj __result){if(CoopMode.Enabled&&__0!=null){int owner=HeroOwner(__0);if(owner>=0)Heroes[__0.Pointer]=owner;if(__result!=null)Balls[__result.Pointer]=Player.I==CoopCombat.PlayerAt(1)?1:0;}}
 public static int BallOwner(BallObj ball){if(ball==null)return -1;int owner;if(Balls.TryGetValue(ball.Pointer,out owner))return owner;owner=HeroOwner(ball.Inst);if(owner<0)owner=HeroOwner(ball.ParentInst);return owner>=0?owner:(ball.ShooterCharIdx==1?1:0);}
 static int From(object value){var ball=value as BallObj;if(ball!=null){return BallOwner(ball);}return HeroOwner(value as HeroInst);}
 static void DamageCall(object[] __args,out Scope __state){__state=new Scope{Old=Source};if(!CoopMode.Enabled)return;foreach(var arg in __args){int owner=From(arg);if(owner>=0){Source=owner;break;}}}
 static void Routine(object __instance,out Scope __state){__state=new Scope{Old=Source};if(!CoopMode.Enabled||!CoopCharacters.IndependentReady)return;int owner=-1;var ballProperty=__instance.GetType().GetProperty("b");if(ballProperty!=null)owner=From(ballProperty.GetValue(__instance));if(owner<0){var heroProperty=__instance.GetType().GetProperty("h");if(heroProperty!=null)owner=From(heroProperty.GetValue(__instance));}Source=owner;if(owner==1){__state.Previous=Enter.Invoke(null,new object[]{CoopCombat.PlayerAt(1)});__state.Scoped=true;}}
 static void KillOwner(GridPieceInst __instance,out int __state){__state=Source;int previous;if(__state<0&&__instance!=null&&LastDamager.TryGetValue(__instance.Pointer,out previous))__state=previous;}
 static void DeathOwner(GridPieceObj __instance,out int __state){__state=Source;int previous;if(__state<0&&__instance!=null&&__instance.Inst!=null&&LastDamager.TryGetValue(__instance.Inst.Pointer,out previous))__state=previous;}
 static void KillFinished(int __state){ReconcileKills(__state);}
 static void ReconcileKills(int owner){if(!CoopMode.Enabled||Complete||BattleSaveData.I==null)return;int total=BattleSaveData.I.NumKills,delta=total-AccountedKills;if(delta<=0)return;AccountedKills=total;if(owner>=0)Kills[owner]+=delta;else TeamKills+=delta;}
 public static void Tick(){ReconcileKills(-1);}
 static void Moving(BallObj __0,out Scope __state){__state=new Scope{Old=Source};if(CoopMode.Enabled&&__0!=null){Source=BallOwner(__0);}}
 static void StatusTick(out Scope __state){__state=new Scope{Old=Source,Status=true};StatusDepth++;Source=-1;}
 static void EndScope(Scope __state){__state?.Close();}
 static void StatusSource(HeroInst __1){if(!CoopMode.Enabled||__1==null)return;int owner=HeroOwner(__1);if(owner>=0)Heroes[__1.Pointer]=owner;}
 static void Before(GridPieceInst __instance,DamageType __1,out Hit __state){__state=null;if(!CoopMode.Enabled||Complete||__instance.CurHealth<=0||__instance.Type.ToString().StartsWith("kAlly"))return;int owner=Source;
  if(StatusDepth>0&&__instance.StatusEffects!=null){foreach(var effect in __instance.StatusEffects){bool match=(__1==DamageType.kFire&&(effect.Type==StatusEffectType.kBurn||effect.Type==StatusEffectType.kFrostburn))||(__1==DamageType.kPoison&&effect.Type==StatusEffectType.kPoison)||(__1==DamageType.kBleed&&effect.Type==StatusEffectType.kBleed)||(__1==DamageType.kDisease&&effect.Type==StatusEffectType.kDisease);if(match){owner=HeroOwner(effect.LastSrc);break;}}}
  __state=new Hit{Before=__instance.CurHealth,Owner=owner,Old=Source};Source=owner;}
 static void After(GridPieceInst __instance,Hit __state){if(__state==null)return;Source=__state.Old;int damage=Math.Max(0,__state.Before-Math.Max(0,__instance.CurHealth));if(damage==0)return;if(__state.Owner>=0)Damage[__state.Owner]+=damage;else TeamDamage+=damage;LastDamager[__instance.Pointer]=__state.Owner;ReconcileKills(__state.Owner);}
 public static void Finish(bool victory){if(Complete)return;ReconcileKills(-1);Complete=true;Victory=victory;CoopPresentation.ResultReady();}
 public static void AttributeForTest(int owner,Action action){if(!CoopTestHarness.Active)throw new InvalidOperationException("Test-only attribution");int before=Source;Source=owner;try{action();}finally{Source=before;}}
}






