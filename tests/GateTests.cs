using System;
class GateTests {
 static void Assert(bool ok,string name){if(!ok)throw new Exception(name);}
 public static void Main(){
  var g=new DecisionGate();Assert(!g.Complete,"initially waiting");
  g.Move(0,-1,3);Assert(g.Index[0]==2,"left wraps");
  g.Confirm(0,true);Assert(!g.Complete,"P1 alone never resumes");
  g.Move(0,1,3);Assert(g.Index[0]==2,"confirmed choice remains fixed");
  g.Confirm(1,false);Assert(!g.Complete,"disconnected P2 cannot confirm");
  g.Confirm(1,true);Assert(g.Complete,"both confirmations resume");
  g.Cancel(0);Assert(!g.Complete,"cancel removes confirmation");
  g.Move(0,1,3);Assert(g.Index[0]==0,"change after cancel");
  g.Reset();Assert(!g.Complete&&g.Index[0]==0&&g.Index[1]==0,"new event resets both");
  g.Confirm(1,true);Assert(!g.Complete,"P2 alone never resumes");
  g.Confirm(0,true);Assert(g.Complete,"reverse confirmation order");
  g.Move(1,1,0);Assert(g.Index[1]==0,"empty choice list safe");
  Console.WriteLine("PASS: 12 upgrade/fusion confirmation gate scenarios.");
 }
}
