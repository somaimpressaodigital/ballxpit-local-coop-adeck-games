// Production package: no test save redirection, automation or photo capture.
using System;
using BepInEx.Logging;
using HarmonyLib;
public static class CoopTestHarness {
 public static bool Active {get{return false;}}
 public static bool PrimaryFireTestEnabled {get{return false;}}
 public static void Install(Harmony h,Type controller,ManualLogSource log){}
 public static void Heartbeat(){}
 public static void Step(){}
}
public static class CoopVolleyAudit {
 public static void Install(Harmony h,ManualLogSource log){}
 public static void Tick(){}
}
public static class CoopSpeedAudit {public static bool Comparing {get{return false;}}}

