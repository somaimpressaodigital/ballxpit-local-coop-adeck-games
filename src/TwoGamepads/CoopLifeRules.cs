using System;
public sealed class CoopLifeRules {
 public readonly float[] Health={1,1},Maximum={1,1};
 public readonly bool[] Rescued={false,false};
 public bool Down(int slot){return Health[slot]<=0;}
 public bool Defeated {get{return Down(0)&&Down(1);}}
 public void Reset(float first,float second){Maximum[0]=Math.Max(1,first);Maximum[1]=Math.Max(1,second);Health[0]=Maximum[0];Health[1]=Maximum[1];Rescued[0]=Rescued[1]=false;}
 public void SetMaximum(int slot,float value){value=Math.Max(1,value);float extra=Math.Max(0,value-Maximum[slot]);Maximum[slot]=value;if(!Down(slot))Health[slot]=Math.Min(value,Health[slot]+extra);}
 public bool CanRescue(int donor){int target=1-donor;return !Down(donor)&&Down(target)&&!Rescued[target];}
 public bool Rescue(int donor){if(!CanRescue(donor))return false;int target=1-donor;float paid=Health[donor]*0.5f;Health[donor]-=paid;Health[target]=Math.Min(Maximum[target],paid);Rescued[target]=true;return true;}
 public void Heal(int slot,float amount){if(!Down(slot)&&amount>0)Health[slot]=Math.Min(Maximum[slot],Health[slot]+amount);}
}

