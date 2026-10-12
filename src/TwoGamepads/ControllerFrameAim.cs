using System;
public sealed class ControllerFrameAim {
 bool Ready;int Frame=-1;float Angle;
 public void Reset(){Ready=false;Frame=-1;}
 public float Update(int frame,float seconds,float initial,float target,float speed,float min,float max){
  if(!Ready){Ready=true;Angle=initial;}
  Angle=Math.Max(min,Math.Min(max,Angle));
  if(Frame!=frame){Frame=frame;Angle=CoopAimRules.TurnDegrees(Angle,target,speed*Math.Max(0,seconds));}
  return Angle;
 }
}

