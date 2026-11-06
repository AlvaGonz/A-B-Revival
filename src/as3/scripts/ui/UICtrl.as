package ui
{
   import f4.data.InitData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.system.ApplicationDomain;
   import fullshit.AbsMain;
   import fullshit.ds.Array1;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.base.UIBase;
   import ui.pages.UIMain;
   
   public class UICtrl
   {
      
      public static var DOMAIN:ApplicationDomain;
      
      public var nowRun:UIBase;
      
      public var disA1:Array1;
      
      public function UICtrl()
      {
         super();
         this.disA1 = new Array1();
      }
      
      public function init(param1:DisplayObjectContainer) : void
      {
         this.nowRun = new UIMain(param1,Attach.attach("UI_Main") as MovieClip,"UI_Main",this);
         MochiEvents.trackEvent("enter_main");
         MochiEvents.startPlay();
      }
      
      public function run() : void
      {
         this.disA1.run1();
         if(Fly4UI.DEBUG && AbsMain.keyCtrl.isKeyDownOnce("a"))
         {
            ConfigAndStory.MONEY += 1000000;
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[17]);
         }
      }
   }
}

