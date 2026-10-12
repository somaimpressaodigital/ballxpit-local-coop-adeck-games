public static class CoopSessionRules {
 public static bool AllowSecondPlayer(bool coop,bool inBattle){return coop&&inBattle;}
 public static int InputSlot(bool coop,bool independent,int simulationOwner,bool secondContext){if(!coop||!independent)return -1;return simulationOwner==0||simulationOwner==1?simulationOwner:secondContext?1:0;}
}
