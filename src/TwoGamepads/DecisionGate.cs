using System;
public sealed class DecisionGate {
 public readonly int[] Index={0,0};
 public readonly bool[] Ready={false,false};
 public bool Complete {get{return Ready[0]&&Ready[1];}}
 public void Reset(){Index[0]=Index[1]=0;Ready[0]=Ready[1]=false;}
 public void Move(int slot,int direction,int count){if(!Ready[slot]&&count>0)Index[slot]=(Index[slot]+direction+count)%count;}
 public void Confirm(int slot,bool connected){Ready[slot]=connected;}
 public void Cancel(int slot){Ready[slot]=false;}
}

