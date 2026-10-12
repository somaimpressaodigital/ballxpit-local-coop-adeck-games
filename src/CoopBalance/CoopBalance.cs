using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

[BepInPlugin("local.ballxpit.coopbalance","Local Coop Balance","1.0.1")]
[BepInDependency("sparrow.ballxpit.localcoop",BepInDependency.DependencyFlags.HardDependency)]
public class CoopBalance : BasePlugin {
 static ConfigEntry<bool> Enabled;
 static ConfigEntry<float> EnemyResistance,BossResistance;
 static FieldInfo ControllerInstance,SecondPlayer;
 public override void Load(){
 Enabled=Config.Bind("Balance","Enabled",true,"Enable resistance scaling only while player two exists.");
 EnemyResistance=Config.Bind("Balance","EnemyResistance",1.8f,new ConfigDescription("Effective resistance multiplier for regular enemies. Incoming damage is divided by this number. Minimum damage stays 1.",new AcceptableValueRange<float>(1f,4f)));
 BossResistance=Config.Bind("Balance","BossResistance",2f,new ConfigDescription("Effective resistance multiplier for bosses. Does not change their health values or rewards.",new AcceptableValueRange<float>(1f,4f)));
 var type=typeof(BALLxPITLocalCoop.Plugin).Assembly.GetType("BALLxPITLocalCoop.PlayerTwoController",true);
 ControllerInstance=type.GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic);
 SecondPlayer=type.GetField("_playerTwo",BindingFlags.Instance|BindingFlags.NonPublic);
 if(ControllerInstance==null||SecondPlayer==null)throw new InvalidOperationException("Coop controller fields unavailable. Balance was not activated.");
 new Harmony("local.ballxpit.coopbalance").Patch(typeof(GridPieceInst).GetMethod("Damage",new[]{typeof(int),typeof(DamageType),typeof(HitType)}),prefix:new HarmonyMethod(typeof(CoopBalance).GetMethod("BeforeDamage",BindingFlags.NonPublic|BindingFlags.Static)));
 Log.LogInfo("Coop Balance loaded. Regular resistance="+EnemyResistance.Value+", boss resistance="+BossResistance.Value+". Damage received, rewards, speed and saves are unchanged by this plugin.");
 }
 static bool HasSecondPlayer(){try{object controller=ControllerInstance.GetValue(null);return controller!=null&&(SecondPlayer.GetValue(controller) as UnityEngine.Object)!=null;}catch{return false;}}
 static void BeforeDamage(GridPieceInst __instance,ref int __0){
 if(!Enabled.Value||__instance==null||__0<=0||!HasSecondPlayer())return;
 string type=__instance.Type.ToString();
 if(type.StartsWith("kAlly",StringComparison.Ordinal)||type=="kGemspring"||type=="kNum")return;
 float multiplier=StatUtl.IsBoss(__instance.Type)?BossResistance.Value:EnemyResistance.Value;
 __0=ScaleDamage(__0,multiplier);
 }
 public static int ScaleDamage(int amount,float multiplier){if(amount<=0)return amount;if(float.IsNaN(multiplier)||float.IsInfinity(multiplier)||multiplier<=1f)return amount;return Math.Max(1,(int)Math.Round(amount/Math.Min(multiplier,4f),MidpointRounding.AwayFromZero));}
}
