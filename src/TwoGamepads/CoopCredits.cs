using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppInterop.Runtime;
public static class CoopCredits {
 public static bool Active;
 static GameObject Root;static TextMeshProUGUI Label;static Button Button;static CanvasGroup MenuGroup;static float MenuAlpha;static bool MenuInteractable,MenuRaycasts;static float Opened;
 public static void Tick(){
  if(Root==null&&MainMenuUI.I!=null){
   NativeChoiceOverlay.Create(MainMenuUI.I);if(NativeChoiceOverlay.ReadableFont==null)return;
   Root=new GameObject("AdeckGamesModCredits");var canvas=Root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30020;
   Root.AddComponent<GraphicRaycaster>();
   var scaler=Root.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=1;
   var item=new GameObject("CreditsButton");var rect=item.AddComponent<RectTransform>();rect.SetParent(Root.transform,false);rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-20,3);rect.sizeDelta=new Vector2(420,24);
   var background=item.AddComponent<Image>();background.color=new Color(0.08f,0.055f,0.1f,0.85f);
   Button=item.AddComponent<Button>();Button.targetGraphic=background;Button.onClick.AddListener((UnityEngine.Events.UnityAction)(Action)(()=>{if(Active)Close();else Show();}));
   var textObj=new GameObject("Author");var textRect=textObj.AddComponent<RectTransform>();textRect.SetParent(rect,false);textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(8,0);textRect.offsetMax=new Vector2(-8,0);
   Label=textObj.AddComponent<TextMeshProUGUI>();Label.font=NativeChoiceOverlay.ReadableFont;Label.fontSize=15;Label.color=new Color(0.9f,0.8f,0.65f);Label.alignment=TextAlignmentOptions.MidlineRight;Label.raycastTarget=false;Label.enableWordWrapping=false;
   UnityEngine.Object.DontDestroyOnLoad(Root);
  }
  if(Root==null)return;
  Label.text=Active?CoopLocalization.Text("Voltar · Extermineg - Adeck Games"):CoopLocalization.Text("Créditos [Y] · Extermineg - Adeck Games");
  Button.interactable=Active||(SceneMgr.GetCurScene()==GameScene.kMainMenu&&!CoopMode.Enabled);
  if(!Active&&Button.interactable&&MainMenuUI.I!=null&&MainMenuUI.I.gameObject.activeInHierarchy&&!MainMenuUI.I.IsRunningSplashScreen&&Application.isFocused&&!CoopTestHarness.Active){for(int slot=0;slot<2;slot++){var pad=TwoGamepads.GetPad(slot);if(pad!=null&&pad.y.justPressed){Show();break;}}}
  if(Active&&Time.unscaledTime>Opened+0.3f&&Application.isFocused&&!CoopTestHarness.Active){
   bool close=Input.GetKeyDown(KeyCode.Escape);for(int slot=0;slot<2;slot++){var pad=TwoGamepads.GetPad(slot);if(pad!=null&&(pad.a.justPressed||pad.b.justPressed))close=true;}if(close)Close();
  }
 }
 public static bool PointerCanReachButton(){
  if(Button==null||UnityEngine.EventSystems.EventSystem.current==null)return false;
  var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);var rect=Button.GetComponent<RectTransform>();data.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
  var hits=new Il2CppSystem.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);return hits.Count>0&&hits[0].gameObject.transform.IsChildOf(Button.transform);
 }
 public static void Show(){
  if(MainMenuUI.I==null||CoopMode.Enabled)return;NativeChoiceOverlay.Create(MainMenuUI.I);Active=true;Opened=Time.unscaledTime;
  MenuGroup=MainMenuUI.I.GetComponent<CanvasGroup>()??MainMenuUI.I.gameObject.AddComponent<CanvasGroup>();MenuAlpha=MenuGroup.alpha;MenuInteractable=MenuGroup.interactable;MenuRaycasts=MenuGroup.blocksRaycasts;MenuGroup.alpha=0;MenuGroup.interactable=false;MenuGroup.blocksRaycasts=false;
  NativeChoiceOverlay.Show(CoopLocalization.Text("Créditos do mod"),CoopLocalization.Text("Clique em Voltar no canto inferior direito • A / X, B / círculo ou Esc: voltar"));
  NativeChoiceOverlay.Information(0,CoopLocalization.Text("CRIAÇÃO DO MOD"),CoopLocalization.Text("Extermineg - Adeck Games\n\nCriação e direção deste modo coop\nMenus, progressão independente, HUD\nResgate, desafios e balanceamento"));
  NativeChoiceOverlay.Information(1,CoopLocalization.Text("DEPENDÊNCIAS"),CoopLocalization.Text("BALLxPIT Local Coop — sparrowsj\nBepInEx e Il2CppInterop\n\nPersonagens, arte e conteúdo:\nBALL x PIT — Kenny Sun"));
 }
 public static void RefreshLocale(){if(!Active)return;
  NativeChoiceOverlay.Show(CoopLocalization.Text("Créditos do mod"),CoopLocalization.Text("Clique em Voltar no canto inferior direito • A / X, B / círculo ou Esc: voltar"));
  NativeChoiceOverlay.Information(0,CoopLocalization.Text("CRIAÇÃO DO MOD"),CoopLocalization.Text("Extermineg - Adeck Games\n\nCriação e direção deste modo coop\nMenus, progressão independente, HUD\nResgate, desafios e balanceamento"));
  NativeChoiceOverlay.Information(1,CoopLocalization.Text("DEPENDÊNCIAS"),CoopLocalization.Text("BALLxPIT Local Coop — sparrowsj\nBepInEx e Il2CppInterop\n\nPersonagens, arte e conteúdo:\nBALL x PIT — Kenny Sun"));
 }

 public static void Close(){Active=false;NativeChoiceOverlay.Hide();if(MenuGroup!=null){MenuGroup.alpha=MenuAlpha;MenuGroup.interactable=MenuInteractable;MenuGroup.blocksRaycasts=MenuRaycasts;}}
}

