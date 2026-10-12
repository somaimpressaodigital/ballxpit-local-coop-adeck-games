using System;
public static class CoopProgressRules {
 public const int MaxPermanentStat=10;
 public const int MaxCharacterLevel=30;
 public static int RequiredProofs(int rosterIndex){return rosterIndex<2?0:1+3*(rosterIndex-2);}
 public static int RequiredStage(int rosterIndex,int stageCount){return Math.Min(Math.Max(0,stageCount-1),Math.Max(0,(rosterIndex-1)/2));}
 public static bool CanUnlockCharacter(int rosterIndex,int proofs,int highestClearedStage,int stageCount){return rosterIndex<2||(proofs>=RequiredProofs(rosterIndex)&&highestClearedStage>=RequiredStage(rosterIndex,stageCount));}
 public static bool CanUnlockStage(int stageIndex,bool previousCleared){return stageIndex==0||previousCleared;}
 public static int ExperienceForLevel(int level){int n=Math.Max(1,Math.Min(MaxCharacterLevel,level));return 3*n*(n-1)/2;}
 public static int CharacterLevel(int experience){int level=1;while(level<MaxCharacterLevel&&Math.Max(0,experience)>=ExperienceForLevel(level+1))level++;return level;}
 public static int UpgradePrice(int currentRank){return 2*(Math.Max(0,currentRank)+1);}
 public static int RequiredUpgradeStage(int currentRank,int stageCount){return Math.Min(Math.Max(0,stageCount-1),Math.Max(0,currentRank));}
 public static bool CanBuy(int currentRank,int points){return currentRank>=0&&currentRank<MaxPermanentStat&&points>=UpgradePrice(currentRank);}
 public static int VictoryPoints(bool firstClear,bool newProof=false){return firstClear?3:newProof?2:1;}
 public static int VictoryExperience(int kills){return 3+Math.Min(27,Math.Max(0,kills)/30);}
 public static int DefeatExperience(int kills){return Math.Min(10,Math.Max(0,kills)/50);}
 public static int DefeatPoints(int kills){return 0;}
 public static int[] CharacterBonuses(int character,int level,int[] baseStats){
  var result=new int[6];var weights=new int[6];int total=0;for(int i=0;i<6;i++){weights[i]=baseStats!=null&&baseStats.Length>i?Math.Max(0,baseStats[i]):0;total+=weights[i];}
  if(total==0){for(int i=0;i<6;i++)weights[i]=1;total=6;}
  var random=new Random(unchecked(104729*(character+1)));for(int rank=1;rank<Math.Min(MaxCharacterLevel,Math.Max(1,level));rank++){int choice=random.Next(total);for(int stat=0;stat<6;stat++){choice-=weights[stat];if(choice<0){result[stat]++;break;}}}return result;
 }
}

