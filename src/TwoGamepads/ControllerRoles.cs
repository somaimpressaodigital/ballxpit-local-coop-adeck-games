using System;
public sealed class ControllerRoles {
 public int First=-1,Second=-1;
 string FirstIdentity,SecondIdentity;
 public void Update(int[] connected,string[] identities,bool preserveSlots=true){
  if(connected.Length!=identities.Length)throw new ArgumentException("Controller IDs and identities must match.");
  First=Find(connected,identities,FirstIdentity,-1);
  Second=Find(connected,identities,SecondIdentity,First);
  if(Second>=0&&Identity(connected,identities,Second)==Identity(connected,identities,First))Second=-1;
  if(!preserveSlots&&First<0&&Second>=0){First=Second;FirstIdentity=SecondIdentity;Second=-1;SecondIdentity=null;}
  for(int i=0;i<connected.Length;i++)if(connected[i]!=First&&connected[i]!=Second){
   if(First<0&&identities[i]!=SecondIdentity){First=connected[i];FirstIdentity=identities[i];}
   else if(Second<0&&identities[i]!=FirstIdentity){Second=connected[i];SecondIdentity=identities[i];}
  }
 }
 static int Find(int[] ids,string[] keys,string wanted,int excluded){if(wanted==null)return -1;for(int i=0;i<ids.Length;i++)if(ids[i]!=excluded&&keys[i]==wanted)return ids[i];return -1;}
 static string Identity(int[] ids,string[] keys,int id){for(int i=0;i<ids.Length;i++)if(ids[i]==id)return keys[i];return null;}
 public void Update(int[] connected,bool preserveSlots=false){
  if(!Contains(connected,First)){
   First=-1;
   foreach(int id in connected)if(id!=Second){First=id;break;}
   if(!preserveSlots&&First<0&&Contains(connected,Second)){First=Second;Second=-1;}
  }
  if(Second==First||!Contains(connected,Second))Second=-1;
  if(Second<0)foreach(int id in connected)if(id!=First){Second=id;break;}
 }
 static bool Contains(int[] connected,int id){return id>=0&&Array.IndexOf(connected,id)>=0;}
}

