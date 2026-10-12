using System;
public static class CoopAimRules {
 public static float TargetDegrees(float x,float y,float min,float max){
  // The legal aim arc is above the character. Lower-half stick inputs
  // project to the nearest horizontal edge, including negative zero Y.
  double forwardY=y>0?y:0d;
  return Math.Max(min,Math.Min(max,(float)(Math.Atan2(forwardY,x)*180/Math.PI)));
 }
 public static float TurnDegrees(float current,float target,float maxStep){
  float delta=target-current;return current+Math.Sign(delta)*Math.Min(Math.Abs(delta),Math.Max(0,maxStep));
 }
}

