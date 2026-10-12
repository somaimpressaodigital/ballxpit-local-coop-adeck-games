using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using TMPro;
using Il2CppInterop.Runtime;
public static class CoopVisuals {
 static FieldInfo Second,SecondController,Aim,Intro;
 static MethodInfo Position;
 static ManualLogSource Log;
 static bool AimLogged,Warned;
 public static bool TwoControllers;
 static FieldInfo Instance;
 static bool MouseHidden,MouseWasVisible;
 static UnityEngine.UI.Image HiddenImage;
 static bool ImageWasEnabled;
 static UnityEngine.UI.Image FirstAim;
 static Type ContextType;static readonly System.Collections.Generic.Dictionary<IntPtr,bool> PreviewStates=new System.Collections.Generic.Dictionary<IntPtr,bool>();
  public static void RefreshLocale(){if(MainMenuUI.I==null||MainMenuUI.I.WrapperTitle==null)return;var heading=MainMenuUI.I.WrapperTitle.transform.Find("CasaDaNathaliaSubtitle");if(heading!=null)heading.GetComponent<TextMeshProUGUI>().text=CoopLocalization.Text("Casa da Nathália");}
 public static void Install(Harmony h,Type controller,ManualLogSource log){
  Log=log;
  Second=AccessTools.Field(controller,"_playerTwo");
  SecondController=AccessTools.Field(controller,"_playerTwoController");
  Aim=AccessTools.Field(controller,"_playerTwoAimDir");
  Intro=AccessTools.Field(controller,"_isFollowingIntro");
  Instance=AccessTools.Field(controller,"_instance");
  var context=controller.Assembly.GetType("BALLxPITLocalCoop.NativePlayerContext",true);
  ContextType=context;
  Position=AccessTools.Method(context,"GetPlayerPosition");
  h.Patch(AccessTools.Method(controller,"Update"),postfix:new HarmonyMethod(typeof(CoopVisuals),nameof(SyncMouse)));
  h.Patch(AccessTools.Method(controller,"LateUpdate"),postfix:new HarmonyMethod(typeof(CoopVisuals),nameof(SyncFirstAim)));
  h.Patch(AccessTools.Method(typeof(MainMenuUI),"Start"),postfix:new HarmonyMethod(typeof(CoopVisuals),nameof(MenuSubtitle)));
  h.Patch(AccessTools.PropertySetter(typeof(Cursor),"visible"),prefix:new HarmonyMethod(typeof(CoopVisuals),nameof(CursorVisibility)));
  h.Patch(AccessTools.Method(typeof(ControllerCursorUI),"MyUpdate"),postfix:new HarmonyMethod(typeof(CoopVisuals),nameof(ControllerCursor)));
 }
 static bool CoopBattle(){
  if(!TwoControllers)return false;
  var controller=Instance.GetValue(null);if(controller==null)return false;
  var player=Second.GetValue(controller) as Player;
  return player!=null&&player.gameObject.activeInHierarchy&&Time.timeScale>0;
 }
 static void CursorVisibility(ref bool __0){if(CoopBattle())__0=false;}
 static void ControllerCursor(ControllerCursorUI __instance){
  if(CoopBattle()&&CoopVitals.Down(0)&&__instance.Img!=null){
   if(HiddenImage!=__instance.Img){HiddenImage=__instance.Img;ImageWasEnabled=HiddenImage.enabled;}
   HiddenImage.enabled=false;
  }
 }
 static void SyncMouse(){
  SyncFirstAim();
  if(CoopMode.Enabled&&CoopCharacters.IndependentReady){for(int slot=0;slot<2;slot++)foreach(string name in slot==0?new[]{"OriginalGameBallPreview","OriginalBallPreview"}:new[]{"PlayerTwoGameBallPreview","PlayerTwoBallPreview"}){var preview=AccessTools.Property(ContextType,name)?.GetValue(null) as Component;if(preview==null)continue;var obj=preview.gameObject;if(CoopVitals.Down(slot)){if(!PreviewStates.ContainsKey(obj.Pointer))PreviewStates[obj.Pointer]=obj.activeSelf;obj.SetActive(false);}else{bool active;if(PreviewStates.TryGetValue(obj.Pointer,out active)){obj.SetActive(active);PreviewStates.Remove(obj.Pointer);}}}if(InGameUI.I!=null&&InGameUI.I.ImgAimCursor!=null)InGameUI.I.ImgAimCursor.enabled=!CoopVitals.Down(0)&&(FirstAim==null||!FirstAim.gameObject.activeSelf);}
  bool hide=CoopBattle();
  if(hide){if(!MouseHidden){MouseWasVisible=Cursor.visible;MouseHidden=true;}Cursor.visible=false;}
  else if(MouseHidden){MouseHidden=false;Cursor.visible=MouseWasVisible;if(HiddenImage!=null){HiddenImage.enabled=ImageWasEnabled;HiddenImage=null;}}
  if(!CoopVitals.Down(0)&&HiddenImage!=null){HiddenImage.enabled=ImageWasEnabled;HiddenImage=null;}
 }
 static void SyncFirstAim(){
  bool show=CoopMode.Enabled&&CoopCharacters.IndependentReady&&SceneMgr.GetCurScene()==GameScene.kGame&&!CoopVitals.Down(0)&&!CoopRoundStats.Complete;
  var preview=AccessTools.Property(ContextType,"OriginalBallPreview")?.GetValue(null) as BallPreview;
  var source=InGameUI.I==null?null:InGameUI.I.ImgAimCursor;
  var sourceRect=InGameUI.I==null?null:InGameUI.I.XfmAimCursor;
  if(!show||preview==null||preview.StartLine==null||source==null||source.sprite==null||sourceRect==null||sourceRect.parent==null||Camera.main==null){if(FirstAim!=null)FirstAim.gameObject.SetActive(false);return;}
  if(FirstAim==null){
   var obj=new GameObject("CoopP1AimCursor");obj.layer=source.gameObject.layer;
   var rect=obj.AddComponent(Il2CppType.Of<RectTransform>()).Cast<RectTransform>();rect.SetParent(sourceRect.parent,false);
   FirstAim=obj.AddComponent(Il2CppType.Of<UnityEngine.UI.Image>()).Cast<UnityEngine.UI.Image>();
   FirstAim.sprite=source.sprite;FirstAim.overrideSprite=source.overrideSprite;FirstAim.type=source.type;FirstAim.preserveAspect=source.preserveAspect;FirstAim.fillCenter=source.fillCenter;FirstAim.fillMethod=source.fillMethod;FirstAim.fillAmount=source.fillAmount;FirstAim.fillClockwise=source.fillClockwise;FirstAim.fillOrigin=source.fillOrigin;FirstAim.useSpriteMesh=source.useSpriteMesh;FirstAim.pixelsPerUnitMultiplier=source.pixelsPerUnitMultiplier;FirstAim.material=source.material;FirstAim.maskable=source.maskable;FirstAim.raycastTarget=false;FirstAim.color=new Color(1,0.5f,0.72f,1);
   rect.anchorMin=sourceRect.anchorMin;rect.anchorMax=sourceRect.anchorMax;rect.pivot=sourceRect.pivot;rect.sizeDelta=sourceRect.sizeDelta;rect.localScale=sourceRect.localScale;rect.localRotation=sourceRect.localRotation;rect.SetSiblingIndex(sourceRect.GetSiblingIndex()+1);
  }
  var line=preview.StartLine;var world=line.transform.TransformPoint(line.End);
  var screen=Camera.main.WorldToScreenPoint(world);if(screen.z<=0){FirstAim.gameObject.SetActive(false);return;}
  var target=FirstAim.rectTransform;var parent=target.parent.Cast<RectTransform>();var canvas=FirstAim.canvas;
  var uiCamera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
  Vector3 position;if(!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent,new Vector2(screen.x,screen.y),uiCamera,out position)){FirstAim.gameObject.SetActive(false);return;}
  target.sizeDelta=sourceRect.sizeDelta;target.localScale=sourceRect.localScale;target.localRotation=sourceRect.localRotation;target.position=position;FirstAim.gameObject.SetActive(true);
 }
 static void MenuSubtitle(MainMenuUI __instance){
  try{
   var title=__instance.WrapperTitle;
   if(title==null||title.transform.Find("CasaDaNathaliaSubtitle")!=null)return;
   var source=__instance.GetComponentInChildren(Il2CppType.Of<TextMeshProUGUI>(),true)?.Cast<TextMeshProUGUI>();
   if(source==null||source.font==null){Log.LogWarning("Subtitle font unavailable.");return;}
   var obj=new GameObject("CasaDaNathaliaSubtitle");
   var rect=obj.AddComponent(Il2CppType.Of<RectTransform>()).Cast<RectTransform>();
   rect.SetParent(title.transform,false);
   rect.anchorMin=new Vector2(0.5f,0.5f);rect.anchorMax=rect.anchorMin;
   rect.pivot=new Vector2(0.5f,0);
   float left=float.MaxValue,right=float.MinValue,top=float.MinValue;
   foreach(var logo in new[]{__instance.LogoBall,__instance.LogoX,__instance.LogoPit}){
    if(logo==null)continue;
    var corners=new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
    logo.rectTransform.GetWorldCorners(corners);
    for(int i=0;i<4;i++){var p=title.transform.InverseTransformPoint(corners[i]);left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);top=Mathf.Max(top,p.y);}
   }
   rect.localPosition=top==float.MinValue?new Vector3(0,150,0):new Vector3((left+right)/2,top+18,0);
   rect.sizeDelta=new Vector2(800,65);
   var text=obj.AddComponent(Il2CppType.Of<TextMeshProUGUI>()).Cast<TextMeshProUGUI>();
   text.font=source.font;text.fontSize=36;text.text=CoopLocalization.Text("Casa da Nathália");
   text.alignment=TextAlignmentOptions.Center;text.color=new Color(1,0.83f,0.89f,1);
   text.raycastTarget=false;text.enableWordWrapping=false;
   Log.LogInfo("Main menu heading added above logo: Casa da Nathália (size 36).");
  }catch(Exception e){Log.LogWarning("Subtitle failed: "+e);}
 }
 static void DrawAim(object __instance){
  if(Event.current.type!=EventType.Repaint)return;
  try{
   var player=Second.GetValue(__instance) as Player;
   if(player==null||!player.gameObject.activeInHierarchy||(bool)Intro.GetValue(__instance))return;
   if(TwoControllers)Cursor.visible=false;
   var camera=Camera.main;if(camera==null)return;
   Vector2 aim=(Vector2)Aim.GetValue(__instance);if(aim.sqrMagnitude<0.001f)return;
   Vector3 origin=(Vector3)Position.Invoke(null,new object[]{player});
   var pc=SecondController.GetValue(__instance) as PlayerCharController;
   if(pc!=null&&pc.ShootXfm!=null)origin=pc.ShootXfm.position;
   var p=camera.WorldToScreenPoint(origin);
   var q=camera.WorldToScreenPoint(origin+new Vector3(aim.x,aim.y,0));
   if(p.z<=0||q.z<=0)return;
   Vector2 start=new Vector2(p.x,Screen.height-p.y);
   Vector2 dir=new Vector2(q.x-p.x,p.y-q.y).normalized;
   if(dir.sqrMagnitude<0.001f)return;
   float scale=Mathf.Max(0.6f,Screen.height/1080f);
   Vector2 end=start+dir*(190*scale);
   Color old=GUI.color;Matrix4x4 matrix=GUI.matrix;
   try{
    GUI.color=new Color(0.1f,0.9f,1,0.85f);
    float angle=(float)(Math.Atan2(dir.y,dir.x)*180/Math.PI);
    GUIUtility.RotateAroundPivot(angle,start);
    for(float d=24*scale;d<180*scale;d+=14*scale)
     GUI.DrawTexture(new Rect(start.x+d,start.y-1.2f*scale,6*scale,2.4f*scale),Texture2D.whiteTexture);
    GUI.matrix=matrix;
    float r=7*scale,t=2*scale;
    GUI.DrawTexture(new Rect(end.x-r,end.y-t/2,r*2,t),Texture2D.whiteTexture);
    GUI.DrawTexture(new Rect(end.x-t/2,end.y-r,t,r*2),Texture2D.whiteTexture);
   }finally{GUI.matrix=matrix;GUI.color=old;}
   if(!AimLogged){Log.LogInfo("P2 cyan aim indicator rendered.");AimLogged=true;}
  }catch(Exception e){if(!Warned){Log.LogWarning("P2 aim indicator failed: "+e.Message);Warned=true;}}
 }
}


