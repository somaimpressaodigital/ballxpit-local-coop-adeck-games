public sealed class PauseInputGate {
 readonly bool[] Previous=new bool[3];bool Primed,HadFocus;int LastFrame=-1;float BlockedUntil;
 public void Reset(){Primed=HadFocus=false;LastFrame=-1;BlockedUntil=0;for(int i=0;i<Previous.Length;i++)Previous[i]=false;}
 public void Transition(int frame,float now){LastFrame=frame;BlockedUntil=now+0.18f;}
 public bool Read(int frame,float now,bool focused,bool allowed,bool first,bool second,bool escape){
  if(frame==LastFrame)return false;
  LastFrame=frame;
  bool edge=(first&&!Previous[0])||(second&&!Previous[1])||(escape&&!Previous[2]);
  Previous[0]=first;Previous[1]=second;Previous[2]=escape;
  bool priming=!Primed||!HadFocus;Primed=true;HadFocus=focused;
  if(!focused||priming||!allowed||now<BlockedUntil||!edge)return false;
  BlockedUntil=now+0.18f;return true;
 }
}
