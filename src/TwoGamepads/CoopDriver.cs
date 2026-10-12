using System;
using UnityEngine;
public sealed class CoopDriver : MonoBehaviour {
 public CoopDriver(IntPtr ptr):base(ptr){}
 public void Update(){CoopLocalization.Tick();CoopTestHarness.Heartbeat();CoopMode.Update();CoopVitals.Tick();CoopCharacters.UpdateStandalone();CoopAmmoReload.Tick();CoopShootingRecovery.Tick();CoopCombat.Tick();CoopBossMechanics.Tick();CoopComfort.Tick();CoopRoundStats.Tick();CoopPresentation.Tick();CoopCredits.Tick();CoopTestHarness.Step();CoopVolleyAudit.Tick();}
 public void OnGUI(){CoopCharacters.Draw();CoopDecisions.Draw();CoopMode.Draw();CoopPresentation.Draw();}
}




