using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
public static class CoopComfort {
 sealed class Pulse{public GridPieceObj Piece;public float Until,Next;}
 static readonly Dictionary<IntPtr,Pulse> Pulses=new Dictionary<IntPtr,Pulse>();
 public static void Install(Harmony h){h.Patch(AccessTools.Method(typeof(GridPieceObj),"HitFlash"),prefix:new HarmonyMethod(typeof(CoopComfort),nameof(Hit)));h.Patch(AccessTools.Method(typeof(UpgradeMgr),"CalculateTgtXP"),postfix:new HarmonyMethod(typeof(CoopComfort),nameof(XpThreshold)));}
 static bool Hit(GridPieceObj __instance){if(!CoopMode.Enabled)return true;Pulse pulse;if(!Pulses.TryGetValue(__instance.Pointer,out pulse)){pulse=new Pulse{Piece=__instance};Pulses[__instance.Pointer]=pulse;}if(Time.unscaledTime>=pulse.Next){pulse.Next=Time.unscaledTime+0.18f;pulse.Until=Time.unscaledTime+0.07f;__instance.SetColor(new Color(0.82f,0.78f,0.74f,1));}return false;}
 static void XpThreshold(){if(CoopMode.Enabled&&UpgradeMgr.I!=null)UpgradeMgr.I.TgtXP=Mathf.Max(1,Mathf.CeilToInt(UpgradeMgr.I.TgtXP*1.8f));}
 public static void Tick(){if(!CoopMode.Enabled){Pulses.Clear();return;}foreach(var item in Pulses){var p=item.Value;if(p.Until>0&&Time.unscaledTime>=p.Until){p.Until=0;if(p.Piece!=null&&p.Piece.gameObject.activeInHierarchy)p.Piece.SetColor(Color.white);}}if(SceneMgr.GetCurScene()!=GameScene.kGame)Pulses.Clear();}
}

