using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
public static class CoopAmmoReload {
 static bool Flushing;static int Logged;static readonly Il2CppSystem.Collections.Generic.List<BallObj>[] HeldLists=new Il2CppSystem.Collections.Generic.List<BallObj>[2];
 public static void Install(Harmony h){h.Patch(AccessTools.Method(typeof(BallMgr),"CreateBall"),postfix:new HarmonyMethod(typeof(CoopAmmoReload),nameof(Born)));}
 public static void Reset(){HeldLists[0]=HeldLists[1]=null;Flushing=false;Logged=0;}
 static void Born(BallObj __result){if(!CoopMode.Enabled||__result==null)return;foreach(var list in HeldLists)if(list!=null)while(list.Remove(__result)){};}
 public static void Tick(){if(!CoopVitals.Active||!CoopCharacters.IndependentReady||CoopRoundStats.Complete||GameMgr.I==null||GameMgr.I.CurState!=GameState.kPlaying)return;Maintain(0);CoopCharacters.ForSecond(()=>Maintain(1));}
 static void Maintain(int slot){var m=BallMgr.I;if(m==null)return;var held=m.EarlyReturnedBalls;HeldLists[slot]=held;if(held!=null){var seen=new HashSet<System.IntPtr>();for(int i=held.Count-1;i>=0;i--){var b=held[i];if(b==null||!b.gameObject.activeInHierarchy||!b.IsBaby()||CoopRoundStats.BallOwner(b)!=slot||!seen.Add(b.Pointer))held.RemoveAt(i);}m._earlyReturnedFollowers=held.Count;}
  if(!Flushing&&m.GetBallState()!=BallState.kLaunching&&held!=null&&held.Count>0){var pending=new List<BallObj>();foreach(var ball in held)if(ball!=null&&ball.IsBaby()&&CoopRoundStats.BallOwner(ball)==slot)pending.Add(ball);Flushing=true;try{foreach(var ball in pending){held.Remove(ball);int before=m.RechargingFollowers.Count;m.RemoveBall(ball,false,BallReturnType.kNormal);if(Logged++<12&&CoopTestHarness.Active)UnityEngine.Debug.Log("RELOAD RETURN P"+(slot+1)+": charge="+before+" -> "+m.RechargingFollowers.Count+", activeObject="+ball.gameObject.activeInHierarchy+", raw="+m._numActiveFollowers);}m._earlyReturnedFollowers=held.Count;}finally{Flushing=false;}}
  int actual=0;if(m.ActiveBalls!=null)foreach(var ball in m.ActiveBalls)if(ball!=null&&ball.gameObject.activeInHierarchy&&ball.IsBaby()&&CoopRoundStats.BallOwner(ball)==slot)actual++;
  m._numActiveFollowers=actual;
  var heroes=CoopCharacters.HeroesFor(slot);if(heroes!=null)foreach(var hero in heroes){var ball=hero.Obj;if(ball==null)continue;bool wrong=(ball.Inst==null||ball.Inst.Pointer!=hero.Pointer)&&(ball.ParentInst==null||ball.ParentInst.Pointer!=hero.Pointer);bool retired=!ball.gameObject.activeInHierarchy;if(wrong||retired){hero.Obj=null;hero.LastReturnTime=TimeMgr.I.GetPhysicsTime();}}
 }
}






