using System;
class ControllerFrameAimTests {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 public static void Main(){
  foreach(int fps in new[]{30,60,144}){
   var first=new ControllerFrameAim();var second=new ControllerFrameAim();float a=90,b=90;
   for(int frame=0;frame<fps*2;frame++){
    a=first.Update(frame,1f/fps,20,160,75,20,160);
    for(int calls=0;calls<8;calls++)Check(first.Update(frame,1f/fps,20,20,75,20,160)==a,"native rewrites must not accelerate or reset aim in one frame");
    b=second.Update(frame,1f/fps,20,160,75,20,160);
    Check(Math.Abs(a-b)<0.001f,"P1 and P2 must turn at the same rate");if(frame==fps-1)Check(Math.Abs(a-95)<0.001f,"one second advances exactly seventy-five degrees");
   }
   Check(a==160,"aim stays inside the legal native arc");
   first.Reset();float initial=first.Update(10000,1f/fps,90,20,75,20,160);Check(Math.Abs(initial-(90-75f/fps))<0.001f,"a new actor resets cached direction");
  }
  var slow=new ControllerFrameAim();Check(slow.Update(0,0,90,160,75,20,160)==90,"zero time does not turn");Check(slow.Update(1,1,90,160,75,20,160)==160,"one second turns at most seventy-five degrees");
  Console.WriteLine("PASS: equal P1/P2 aim at 30/60/144 FPS, repeated native calls, native angle rewrites, arc limits and actor reset.");
 }
}

