using HarmonyLib;
public static class CoopTutorial {
 public static void Install(Harmony h){
  h.Patch(AccessTools.Method(typeof(GameTutMgr),"IsSeen"),prefix:new HarmonyMethod(typeof(CoopTutorial),nameof(Seen)));
  h.Patch(AccessTools.Method(typeof(GameTutMgr),"Show"),prefix:new HarmonyMethod(typeof(CoopTutorial),nameof(Show)));
 }
 static bool Skip(GameTutType type){return CoopMode.Enabled;}
 static bool Seen(GameTutType __0,ref bool __result){if(!Skip(__0))return true;__result=true;return false;}
 static bool Show(GameTutType __0){return !Skip(__0);}
}


