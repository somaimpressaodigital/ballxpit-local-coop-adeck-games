using System;
class ControllerIdentityTests {
 static int Checks;
 static void Check(ControllerRoles roles,int[] ids,string[] keys,int first,int second){roles.Update(ids,keys,true);if(roles.First!=first||roles.Second!=second)throw new Exception("Expected "+first+"/"+second+", got "+roles.First+"/"+roles.Second);Checks++;}
 public static void Main(){
  var r=new ControllerRoles();Check(r,new[]{0,1},new[]{"Xbox A","PS5 A"},0,1);
  Check(r,new[]{1},new[]{"PS5 A"},-1,1);
  Check(r,new[]{1,4},new[]{"PS5 A","Xbox A"},4,1);
  Check(r,new int[0],new string[0],-1,-1);
  Check(r,new[]{5},new[]{"PS5 A"},-1,5);
  Check(r,new[]{5,6},new[]{"PS5 A","Xbox A"},6,5);
  Check(r,new[]{0,1},new[]{"PS5 A","Xbox A"},1,0);
  Check(r,new[]{1},new[]{"Xbox A"},1,-1);
  Check(r,new[]{1,7},new[]{"Xbox A","Xbox B"},1,7);
  Check(r,new[]{1,7,8},new[]{"Xbox A","Xbox B","PS5 A"},1,7);
  Check(r,new[]{0,1},new[]{"Xbox A","Xbox A"},0,-1);
  var ps=new ControllerRoles();Check(ps,new[]{10,11},new[]{"PS5 A","PS5 B"},10,11);
  Check(ps,new int[0],new string[0],-1,-1);
  Check(ps,new[]{12,13},new[]{"PS5 B","PS5 A"},13,12);
  Console.WriteLine("PASS: "+Checks+" device identity, reconnect, ID reuse, reverse order, replacement and duplicate identity scenarios.");
 }
}
