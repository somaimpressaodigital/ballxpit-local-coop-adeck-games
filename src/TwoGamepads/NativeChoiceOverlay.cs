using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppInterop.Runtime;
public static class NativeChoiceOverlay {
 static GameObject Root; static string CurrentTitle;
 static TextMeshProUGUI Title,Footer;
 static readonly TextMeshProUGUI[] Names=new TextMeshProUGUI[2],Status=new TextMeshProUGUI[2];
 static readonly Image[] Icons=new Image[2];
 static TMP_FontAsset Font;
 public static TMP_FontAsset ReadableFont {get{return Font;}}
 static readonly Transform[] Cards=new Transform[2];
 static readonly Image[,] OptionBacks=new Image[2,3],OptionIcons=new Image[2,3];
 static readonly Image[,] FusionIcons=new Image[2,3];static readonly TextMeshProUGUI[] PageLabels=new TextMeshProUGUI[2];
 static readonly TextMeshProUGUI[] Descriptions=new TextMeshProUGUI[2];
 static readonly TextMeshProUGUI[,] OptionNames=new TextMeshProUGUI[2,3];
 static readonly System.Collections.Generic.List<Image>[] GridBacks={new System.Collections.Generic.List<Image>(),new System.Collections.Generic.List<Image>()},GridIcons={new System.Collections.Generic.List<Image>(),new System.Collections.Generic.List<Image>()};
 static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max){
  var obj=new GameObject(name);var r=obj.AddComponent(Il2CppType.Of<RectTransform>()).Cast<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;return r;
 }
 static TextMeshProUGUI Text(string name,Transform parent,Vector2 min,Vector2 max,int size,Color color){
  var rect=Rect(name,parent,min,max);var text=rect.gameObject.AddComponent(Il2CppType.Of<TextMeshProUGUI>()).Cast<TextMeshProUGUI>();
  text.font=Font;text.fontSize=size*Mathf.Max(0.6f,Screen.height/1080f);text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;text.enableWordWrapping=true;return text;
 }
  public static void RefreshLocale(){if(Root==null)return;CurrentTitle=null;for(int slot=0;slot<2;slot++)Cards[slot].Find("Controls").GetComponent<TextMeshProUGUI>().text=CoopLocalization.Text("Analógico esquerdo: trocar\nA / X: confirmar • B / círculo: alterar");}
 public static void Create(Component source){
  if(Root!=null)return;
  var sample=source.GetComponentInChildren(Il2CppType.Of<TextMeshProUGUI>(),true)?.Cast<TextMeshProUGUI>();
  if(sample==null||sample.font==null)return;
  Font=sample.font;
  try{var readable=TMP_FontAsset.CreateFontAsset(@"C:\Windows\Fonts\segoeui.ttf",0,90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);if(readable!=null){Font=readable;UnityEngine.Object.DontDestroyOnLoad(readable);UnityEngine.Debug.Log("Coop readable font loaded: "+readable.name);}}catch(System.Exception e){UnityEngine.Debug.LogWarning("Coop readable font unavailable: "+e.Message);}
  Root=new GameObject("LocalCoopChoiceOverlay");var root=Root.AddComponent(Il2CppType.Of<RectTransform>()).Cast<RectTransform>();
  var canvas=Root.AddComponent(Il2CppType.Of<Canvas>()).Cast<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
  UnityEngine.Object.DontDestroyOnLoad(Root);
  var back=Rect("Backdrop",root,Vector2.zero,Vector2.one).gameObject.AddComponent(Il2CppType.Of<Image>()).Cast<Image>();back.color=new Color(0.04f,0.03f,0.07f,1);back.raycastTarget=false;
  Title=Text("Heading",root,new Vector2(0.05f,0.88f),new Vector2(0.95f,0.98f),34,Color.white);
  Footer=Text("Footer",root,new Vector2(0.05f,0.02f),new Vector2(0.95f,0.11f),20,new Color(0.85f,0.85f,0.9f));
  for(int slot=0;slot<2;slot++){
   float x=slot==0?0.055f:0.525f;
   var card=Rect("Player"+(slot+1),root,new Vector2(x,0.15f),new Vector2(x+0.42f,0.84f));
   Cards[slot]=card;
   var bg=card.gameObject.AddComponent(Il2CppType.Of<Image>()).Cast<Image>();bg.color=new Color(0.13f,0.1f,0.17f);bg.raycastTarget=false;
   Color color=slot==0?new Color(1,0.8f,0.9f):new Color(0.1f,0.9f,1);
   var player=Text("Player",card,new Vector2(0.05f,0.85f),new Vector2(0.95f,0.98f),28,color);player.text=CoopLocalization.Text("JOGADOR ")+(slot+1);
   var icon=Rect("Icon",card,new Vector2(0.27f,0.48f),new Vector2(0.73f,0.83f)).gameObject.AddComponent(Il2CppType.Of<Image>()).Cast<Image>();icon.preserveAspect=true;icon.raycastTarget=false;Icons[slot]=icon;
   Names[slot]=Text("Name",card,new Vector2(0.04f,0.3f),new Vector2(0.96f,0.48f),30,Color.white);
   Status[slot]=Text("Status",card,new Vector2(0.04f,0.2f),new Vector2(0.96f,0.29f),26,color);
   var hint=Text("Controls",card,new Vector2(0.04f,0.015f),new Vector2(0.96f,0.18f),22,new Color(0.8f,0.78f,0.85f));hint.text=CoopLocalization.Text("Analógico esquerdo: trocar\nA / X: confirmar • B / círculo: alterar");
  }
  Root.SetActive(false);
 }
 public static void Show(string title,string footer){if(Root==null)return;if(CurrentTitle!=title){ResetLayout();CurrentTitle=title;}Root.SetActive(true);Title.text=title;Footer.text=footer;}
 public static void Card(int slot,string name,bool ready,Sprite icon){if(Root==null)return;Names[slot].enabled=true;Names[slot].text=name;Status[slot].text=ready?CoopLocalization.Text("PRONTO"):CoopLocalization.Text("Escolhendo...");Icons[slot].sprite=icon;Icons[slot].color=Color.white;Icons[slot].enabled=icon!=null;}
 public static void Details(int slot,string text,bool ready){Card(slot,text,ready,null);if(Root==null)return;Names[slot].fontSize=24*Mathf.Max(0.6f,Screen.height/1080f);Names[slot].rectTransform.anchorMin=new Vector2(0.04f,0.31f);Names[slot].rectTransform.anchorMax=new Vector2(0.96f,0.81f);}
 public static void Information(int slot,string heading,string text){Details(slot,text,false);if(Root==null)return;Cards[slot].Find("Player").GetComponent<TextMeshProUGUI>().text=heading;Status[slot].gameObject.SetActive(false);Cards[slot].Find("Controls").gameObject.SetActive(false);Names[slot].rectTransform.anchorMin=new Vector2(0.06f,0.12f);Names[slot].rectTransform.anchorMax=new Vector2(0.94f,0.82f);}
 public static void CharacterGrid(int slot,Sprite[] sprites,bool[] unlocked,int selected,bool level=false,string profile=null){
  if(Root==null)return;
  for(int i=0;i<3;i++)if(OptionBacks[slot,i]!=null)OptionBacks[slot,i].gameObject.SetActive(false);
  Icons[slot].rectTransform.anchorMin=new Vector2(0.35f,0.61f);Icons[slot].rectTransform.anchorMax=new Vector2(0.65f,0.84f);
  Names[slot].rectTransform.anchorMin=new Vector2(0.04f,0.52f);Names[slot].rectTransform.anchorMax=new Vector2(0.96f,0.62f);
  Status[slot].rectTransform.anchorMin=new Vector2(0.04f,0.46f);Status[slot].rectTransform.anchorMax=new Vector2(0.96f,0.53f);
  Icons[slot].color=unlocked[selected]?Color.white:new Color(0,0,0,1);
  if(!level){
   Icons[slot].rectTransform.anchorMin=new Vector2(0.35f,0.7f);Icons[slot].rectTransform.anchorMax=new Vector2(0.65f,0.85f);
   Names[slot].rectTransform.anchorMin=new Vector2(0.04f,0.59f);Names[slot].rectTransform.anchorMax=new Vector2(0.96f,0.7f);
   Names[slot].enableAutoSizing=true;Names[slot].fontSizeMin=16;Names[slot].fontSizeMax=28*Mathf.Max(0.6f,Screen.height/1080f);
   Status[slot].rectTransform.anchorMin=new Vector2(0.04f,0.53f);Status[slot].rectTransform.anchorMax=new Vector2(0.96f,0.59f);
   if(Descriptions[slot]==null)Descriptions[slot]=Text("Description",Cards[slot],Vector2.zero,Vector2.one,21,new Color(0.92f,0.89f,0.83f));
   Descriptions[slot].rectTransform.anchorMin=new Vector2(0.055f,0.32f);Descriptions[slot].rectTransform.anchorMax=new Vector2(0.945f,0.53f);
   Descriptions[slot].text=profile??"";Descriptions[slot].gameObject.SetActive(!string.IsNullOrEmpty(profile));Descriptions[slot].enableAutoSizing=true;Descriptions[slot].fontSizeMin=13;Descriptions[slot].fontSizeMax=20*Mathf.Max(0.6f,Screen.height/1080f);
   var hint=Cards[slot].Find("Controls").Cast<RectTransform>();hint.anchorMin=new Vector2(0.04f,0.005f);hint.anchorMax=new Vector2(0.96f,0.08f);
  }
  int columns=level?4:8,rows=(sprites.Length+columns-1)/columns;
  float size=Mathf.Min(Screen.width*(level?0.8f:0.42f)*0.88f/columns,Screen.height*0.69f*0.32f/Mathf.Max(1,rows));
  for(int i=0;i<sprites.Length;i++){
   if(GridBacks[slot].Count<=i){
    var r=Rect("Character"+i,Cards[slot],new Vector2(0.5f,0.29f),new Vector2(0.5f,0.29f));
    var bg=r.gameObject.AddComponent(Il2CppType.Of<Image>()).Cast<Image>();bg.raycastTarget=false;GridBacks[slot].Add(bg);
    var icon=Rect("Portrait",r,new Vector2(0.07f,0.07f),new Vector2(0.93f,0.93f)).gameObject.AddComponent(Il2CppType.Of<Image>()).Cast<Image>();icon.preserveAspect=true;icon.raycastTarget=false;GridIcons[slot].Add(icon);
   }
   var rect=GridBacks[slot][i].rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0.5f,level?0.29f:0.2f);rect.sizeDelta=new Vector2(size-5,size-5);rect.anchoredPosition=new Vector2((i%columns-(columns-1)/2f)*size,((rows-1)/2f-i/columns)*size);
   GridBacks[slot][i].gameObject.SetActive(true);GridBacks[slot][i].color=i==selected?(slot==0?new Color(0.65f,0.36f,0.5f):new Color(0.12f,0.52f,0.58f)):new Color(0.25f,0.22f,0.3f);
   GridIcons[slot][i].sprite=sprites[i];GridIcons[slot][i].color=unlocked[i]?Color.white:Color.black;
  }
  for(int i=sprites.Length;i<GridBacks[slot].Count;i++)GridBacks[slot][i].gameObject.SetActive(false);
 }
 public static void Options(int slot,string[] names,Sprite[] icons,int selected,Sprite[] fusionIcons=null,string description=null){
  if(Root==null)return;
  Icons[slot].enabled=false;Names[slot].enabled=false;
  int page=selected/3,start=page*3,count=Mathf.Min(3,names.Length-start);
  if(Descriptions[slot]==null)Descriptions[slot]=Text("Description",Cards[slot],new Vector2(0.05f,0.335f),new Vector2(0.95f,0.46f),21,new Color(0.92f,0.89f,0.83f));Descriptions[slot].gameObject.SetActive(!string.IsNullOrEmpty(description));Descriptions[slot].text=description??"";Descriptions[slot].enableAutoSizing=true;Descriptions[slot].fontSizeMin=14;Descriptions[slot].fontSizeMax=21*Mathf.Max(0.6f,Screen.height/1080f);
  Descriptions[slot].rectTransform.anchorMin=new Vector2(0.05f,0.335f);Descriptions[slot].rectTransform.anchorMax=new Vector2(0.95f,0.46f);
  if(PageLabels[slot]==null)PageLabels[slot]=Text("PageIndicator",Cards[slot],new Vector2(0.05f,0.29f),new Vector2(0.95f,0.325f),18,new Color(0.85f,0.82f,0.75f));PageLabels[slot].gameObject.SetActive(true);PageLabels[slot].text=CoopLocalization.Text("Página ")+(page+1)+" / "+((names.Length+2)/3)+CoopLocalization.Text("  •  Opção ")+(selected+1)+" / "+names.Length+(names.Length>3?"  •  ← / →":"");
  for(int i=0;i<3;i++){
   if(OptionBacks[slot,i]==null){
    var r=Rect("Option"+i,Cards[slot],Vector2.zero,Vector2.one);
    var bg=r.gameObject.AddComponent(Il2CppType.Of<Image>()).Cast<Image>();bg.raycastTarget=false;OptionBacks[slot,i]=bg;
    var image=Rect("Image",r,new Vector2(0.12f,0.42f),new Vector2(0.88f,0.92f)).gameObject.AddComponent(Il2CppType.Of<Image>()).Cast<Image>();image.preserveAspect=true;image.raycastTarget=false;OptionIcons[slot,i]=image;
    OptionNames[slot,i]=Text("Label",r,new Vector2(0.025f,0.025f),new Vector2(0.975f,0.43f),24,Color.white);
    OptionNames[slot,i].enableAutoSizing=true;OptionNames[slot,i].fontSizeMin=14;OptionNames[slot,i].fontSizeMax=24*Mathf.Max(0.6f,Screen.height/1080f);
    FusionIcons[slot,i]=Rect("SecondOrb",r,new Vector2(0.54f,0.48f),new Vector2(0.94f,0.9f)).gameObject.AddComponent(Il2CppType.Of<Image>()).Cast<Image>();FusionIcons[slot,i].preserveAspect=true;FusionIcons[slot,i].raycastTarget=false;
   }
   bool active=i<count;OptionBacks[slot,i].gameObject.SetActive(active);if(!active)continue;
   var rect=OptionBacks[slot,i].rectTransform;float width=0.9f/count;
   rect.anchorMin=new Vector2(0.05f+i*width+0.007f,string.IsNullOrEmpty(description)?0.33f:0.48f);rect.anchorMax=new Vector2(0.05f+(i+1)*width-0.007f,0.83f);rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
   int index=start+i;OptionBacks[slot,i].color=index==selected?new Color(0.13f,0.4f,0.48f):new Color(0.2f,0.16f,0.25f);
   OptionNames[slot,i].text=names[index];OptionIcons[slot,i].sprite=icons[index];OptionIcons[slot,i].enabled=icons[index]!=null;
   bool fusion=fusionIcons!=null&&index<fusionIcons.Length&&fusionIcons[index]!=null;FusionIcons[slot,i].enabled=fusion;FusionIcons[slot,i].sprite=fusion?fusionIcons[index]:null;OptionIcons[slot,i].rectTransform.anchorMin=new Vector2(fusion?0.06f:0.12f,0.48f);OptionIcons[slot,i].rectTransform.anchorMax=new Vector2(fusion?0.46f:0.88f,0.9f);
  }
 }
 public static void Loading(string title,string message){Show(title,message);for(int i=0;i<2;i++)Cards[i].gameObject.SetActive(false);Footer.rectTransform.anchorMin=new Vector2(0.1f,0.38f);Footer.rectTransform.anchorMax=new Vector2(0.9f,0.62f);}
 public static void SinglePanel(){Cards[0].Find("Player").GetComponent(Il2CppType.Of<TextMeshProUGUI>()).Cast<TextMeshProUGUI>().text=CoopLocalization.Text("DUPLA");Cards[1].gameObject.SetActive(false);var r=Cards[0].Cast<RectTransform>();r.anchorMin=new Vector2(0.1f,0.15f);r.anchorMax=new Vector2(0.9f,0.84f);}
 static void ResetLayout(){for(int restoreSlot=0;restoreSlot<2;restoreSlot++){Status[restoreSlot].gameObject.SetActive(true);Cards[restoreSlot].Find("Controls").gameObject.SetActive(true);}Footer.rectTransform.anchorMin=new Vector2(0.05f,0.02f);Footer.rectTransform.anchorMax=new Vector2(0.95f,0.11f);for(int slot=0;slot<2;slot++){Cards[slot].gameObject.SetActive(true);Cards[slot].Find("Player").GetComponent(Il2CppType.Of<TextMeshProUGUI>()).Cast<TextMeshProUGUI>().text=CoopLocalization.Text("JOGADOR ")+(slot+1);if(PageLabels[slot]!=null)PageLabels[slot].gameObject.SetActive(false);if(Descriptions[slot]!=null)Descriptions[slot].gameObject.SetActive(false);var cr=Cards[slot].Cast<RectTransform>();float x=slot==0?0.055f:0.525f;cr.anchorMin=new Vector2(x,0.15f);cr.anchorMax=new Vector2(x+0.42f,0.84f);foreach(var bg in GridBacks[slot])bg.gameObject.SetActive(false);for(int i=0;i<3;i++)if(OptionBacks[slot,i]!=null)OptionBacks[slot,i].gameObject.SetActive(false);Names[slot].enabled=true;Names[slot].fontSize=30*Mathf.Max(0.6f,Screen.height/1080f);Icons[slot].rectTransform.anchorMin=new Vector2(0.27f,0.48f);Icons[slot].rectTransform.anchorMax=new Vector2(0.73f,0.83f);Names[slot].rectTransform.anchorMin=new Vector2(0.04f,0.3f);Names[slot].rectTransform.anchorMax=new Vector2(0.96f,0.48f);Status[slot].rectTransform.anchorMin=new Vector2(0.04f,0.2f);Status[slot].rectTransform.anchorMax=new Vector2(0.96f,0.29f);}}
 public static void ClearChoices(){if(Root==null)return;ResetLayout();for(int s=0;s<2;s++){Names[s].text=CoopLocalization.Text("Preparando opções...");Status[s].text="";Icons[s].enabled=false;}}
 public static void Hide(){if(Root!=null)Root.SetActive(false);}
}









