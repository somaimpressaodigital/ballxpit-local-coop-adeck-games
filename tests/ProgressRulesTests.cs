using System;
public static class ProgressRulesTests {
 static int Count;
 static void Check(bool value,string name){if(!value)throw new Exception(name);Count++;}
 public static void Main(){
  Check(CoopProgressRules.CanUnlockCharacter(0,0,-1,8),"first character available");
  Check(CoopProgressRules.CanUnlockCharacter(1,0,-1,8),"second character available");
  Check(!CoopProgressRules.CanUnlockCharacter(2,0,0,8),"no third character before victory");
  Check(CoopProgressRules.CanUnlockCharacter(2,1,0,8),"third character after first victory");
  Check(!CoopProgressRules.CanUnlockCharacter(4,20,0,8),"farming first stage cannot bypass stage requirement");
  Check(CoopProgressRules.CanUnlockCharacter(4,7,1,8),"proofs and stage requirement met");
  Check(!CoopProgressRules.CanUnlockCharacter(3,999,0,8),"fourth character requires second phase regardless of farming");
  Check(CoopProgressRules.CanUnlockCharacter(15,40,7,8),"entire roster can eventually unlock");
  Check(!CoopProgressRules.CanUnlockCharacter(15,39,7,8),"last character still requires proofs");
  Check(!CoopProgressRules.CanUnlockCharacter(15,99,6,8),"last character requires final phase");
  Check(CoopProgressRules.CanUnlockStage(0,false),"first stage available");
  Check(!CoopProgressRules.CanUnlockStage(1,false),"second stage requires first");
  Check(CoopProgressRules.CanUnlockStage(1,true),"second stage unlocks after first");
  Check(CoopProgressRules.CharacterLevel(0)==1,"starting level");
  Check(CoopProgressRules.CharacterLevel(2)==1,"partial experience");
  Check(CoopProgressRules.CharacterLevel(3)==2,"level threshold");
  Check(CoopProgressRules.CharacterLevel(9999)==30,"level cap");
  Check(!CoopProgressRules.CanBuy(0,1),"first permanent upgrade needs earned currency");
  Check(CoopProgressRules.CanBuy(0,2),"first permanent upgrade");
  Check(!CoopProgressRules.CanBuy(1,1),"insufficient points");
  Check(CoopProgressRules.CanBuy(9,20),"last permanent upgrade");
  Check(!CoopProgressRules.CanBuy(10,100),"permanent upgrade cap");
  Check(CoopProgressRules.VictoryPoints(true)==3&&CoopProgressRules.VictoryPoints(false,true)==2&&CoopProgressRules.VictoryPoints(false)==1,"fresh and repeated victory reward scaling");
  Check(CoopProgressRules.DefeatPoints(10)==0&&CoopProgressRules.DefeatPoints(10000)==0,"suicide farming grants no permanent points");
  Check(CoopProgressRules.CharacterLevel(8)==2&&CoopProgressRules.CharacterLevel(9)==3,"later levels require more experience");
  Check(CoopProgressRules.DefeatExperience(49)==0&&CoopProgressRules.DefeatExperience(50)==1,"qualified losses retain earned experience");
  var proofs=new System.Collections.Generic.HashSet<string>();var available=new System.Collections.Generic.HashSet<int>{0,1};int highest=-1,runs=0;
  while(available.Count<16&&runs<100){int stage=-1,a=-1,b=-1;for(int s=0;s<8&&stage<0;s++){if(s>highest+1)continue;foreach(int ch in available)if(!proofs.Contains(s+":"+ch)){if(a<0)a=ch;else{b=ch;break;}}if(a>=0)stage=s;}Check(stage>=0,"progression has an attainable next challenge");proofs.Add(stage+":"+a);if(b>=0)proofs.Add(stage+":"+b);highest=Math.Max(highest,stage);runs++;for(int i=0;i<16;i++)if(CoopProgressRules.CanUnlockCharacter(i,proofs.Count,highest,8))available.Add(i);}
  Check(available.Count==16&&highest==7,"all characters reachable through unique victories and campaign completion");
  int unchanged=proofs.Count;proofs.Add("0:0");proofs.Add("0:1");Check(proofs.Count==unchanged,"repeating an existing party and phase grants no new proof");
  for(int rank=0;rank<10;rank++){Check(CoopProgressRules.RequiredUpgradeStage(rank,8)==Math.Min(rank,7),"permanent upgrade requires matching campaign progress");}
  var bonuses=CoopProgressRules.CharacterBonuses(3,10,new[]{0,2,0,0,0,0});Check(bonuses[1]==9,"character levels provide actual native-stat bonuses");var capped=CoopProgressRules.CharacterBonuses(0,99,null);int sum=0;foreach(int item in capped)sum+=item;Check(sum==29,"level bonus cap");
  Console.WriteLine("PASS: "+Count+" progression rule checks");
 }
}
