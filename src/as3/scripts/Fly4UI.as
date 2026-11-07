package
{
   import all.AllData;
   import f4.core.OpOneCore;
   import f4.data.InitData;
   import f4.data.SoundData;
   import f4.data.sound32.SoundCtrl32;
   import f4data.cpx.F4DataCPX;
   import f4data.sdata.ConfigAndStory;
   import flash.display.Loader;
   import flash.display.LoaderInfo;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.system.ApplicationDomain;
   import flash.text.TextField;
   import flash.utils.ByteArray;
   import flash.utils.getTimer;
   import fullshit.AbsMain;
   import fullshit.ctrl.KeyCtrl;
   import fullshit.ctrl2.SoundCtrl2;
   import mochi.as3.MochiAd;
   import ui.UICtrl;
   
   public dynamic class Fly4UI extends AbsMain
   {
      
      public static var MAINFLY4:Fly4UI;
      
      public static var DOMAIN:ApplicationDomain;
      
      public static var LVDATA:Object;
      
      public static var DEFFLYDATA:Object;
      
      public static var STARTDATA:ByteArray;
      
      public static const DEBUG:Boolean = false;
      
      public static const BUILD:String = "BUILD 111214_2";
      
      public var key:KeyCtrl;
      
      public var ui_out:Sprite;
      
      public var uictrl:UICtrl;
      
      public var core:OpOneCore;
      
      public var soundc2:SoundCtrl2;
      
      public var soundc32:SoundCtrl32;
      
      public var text:TextField;
      
      public var loadNum:int = 0;
      
      public var alldata:AllData;
      
      public function Fly4UI()
      {
         super();
         MAINFLY4 = this;
         this.alldata = new AllData();
         this.dataLoad();
         this.soundLoad();
         this.uiLoad();
      }
      
      public static function addRandomData() : void
      {
         if(ConfigAndStory.ALLPART.plPart.length == 0)
         {
            ConfigAndStory.ALLPART.addPartFromDef(7);
            ConfigAndStory.ALLPART.addPartFromDef(174);
            ConfigAndStory.ALLPART.addPartFromDef(223);
            ConfigAndStory.ALLPART.addPartFromDef(137);
            ConfigAndStory.ALLPART.addPartFromDef(140);
            if(VeryOutShitABC.VGPLAY)
            {
               ConfigAndStory.ALLPART.addPartFromDef(107);
               ConfigAndStory.ALLPART.addPartFromDef(107);
               ConfigAndStory.ALLPART.addPartFromDef(113);
            }
            else
            {
               ConfigAndStory.ALLPART.addPartFromDef(72);
               ConfigAndStory.ALLPART.addPartFromDef(72);
               ConfigAndStory.ALLPART.addPartFromDef(78);
            }
         }
      }
      
      public function allLoaded() : void
      {
         this.stCop();
         this.lvCop();
         this.defCop();
         this.startLoadXML();
         this.xmlLoaded();
      }
      
      public function stCop() : void
      {
         var _loc1_:ByteArray = new ByteArray();
         _loc1_.writeBytes(new this.alldata.startFly());
         STARTDATA = _loc1_;
      }
      
      public function lvCop() : void
      {
         var _loc1_:ByteArray = new ByteArray();
         _loc1_.writeBytes(new this.alldata.levels());
         _loc1_.position = 0;
         _loc1_.uncompress();
         _loc1_.position = 0;
         LVDATA = _loc1_.readObject();
      }
      
      public function defCop() : void
      {
         var _loc1_:ByteArray = new ByteArray();
         _loc1_.writeBytes(new this.alldata.defFly());
         _loc1_.position = 0;
         _loc1_.uncompress();
         _loc1_.position = 0;
         DEFFLYDATA = _loc1_.readObject();
      }
      
      public function dataLoad() : void
      {
         var _loc1_:Loader = new Loader();
         _loc1_.contentLoaderInfo.addEventListener(Event.COMPLETE,this.dataCop);
         _loc1_.loadBytes(new this.alldata.data());
      }
      
      public function soundLoad() : void
      {
         var _loc1_:Loader = new Loader();
         _loc1_.contentLoaderInfo.addEventListener(Event.COMPLETE,this.soundCop);
         _loc1_.loadBytes(new this.alldata.sound());
      }
      
      public function uiLoad() : void
      {
         var _loc1_:Loader = new Loader();
         _loc1_.contentLoaderInfo.addEventListener(Event.COMPLETE,this.uiCop);
         _loc1_.loadBytes(new this.alldata.ui());
      }
      
      private function dataCop(param1:Event) : void
      {
         trace("dataLoaded");
         var _loc2_:LoaderInfo = param1.currentTarget as LoaderInfo;
         _loc2_.removeEventListener(Event.COMPLETE,this.dataCop);
         DOMAIN = _loc2_.applicationDomain;
         ++this.loadNum;
         if(this.loadNum == 3)
         {
            this.allLoaded();
         }
      }
      
      private function soundCop(param1:Event) : void
      {
         var _loc2_:LoaderInfo = param1.currentTarget as LoaderInfo;
         _loc2_.removeEventListener(Event.COMPLETE,this.soundCop);
         InitData.SOUNDDATA = new SoundData();
         InitData.SOUNDDATA.DOMAIN = _loc2_.applicationDomain;
         InitData.SOUNDDATA.initArray();
         ++this.loadNum;
         if(this.loadNum == 3)
         {
            this.allLoaded();
         }
      }
      
      private function uiCop(param1:Event) : void
      {
         var _loc2_:LoaderInfo = param1.currentTarget as LoaderInfo;
         _loc2_.removeEventListener(Event.COMPLETE,this.uiCop);
         UICtrl.DOMAIN = _loc2_.applicationDomain;
         ++this.loadNum;
         if(this.loadNum == 3)
         {
            this.allLoaded();
         }
      }
      
      public function startLoadXML() : void
      {
         ConfigAndStory.ALLPART = new F4DataCPX(this.alldata);
         this.ui_out = new Sprite();
         this.key = new KeyCtrl(stage);
         this.text = new TextField();
         this.text.y = 460;
         this.text.textColor = 16777215;
         this.text.selectable = false;
         this.text.width = 640;
         this.text.mouseWheelEnabled = false;
         this.text.mouseEnabled = false;
         this.text.alpha = 0.25;
         if(DEBUG)
         {
            stage.addChild(this.text);
         }
      }
      
      public function xmlLoaded() : void
      {
         this.uictrl = new UICtrl();
         this.addChild(this.ui_out);
         this.addEventListener(Event.ENTER_FRAME,this.loop);
         ConfigAndStory.init();
         if(ConfigAndStory.STORY == 0)
         {
            addRandomData();
         }
         InitData.UICTRL = this.uictrl;
         InitData.initAll(stage,this);
         this.uictrl.init(this.ui_out);
         this.core = InitData.CORE;
         this.soundc2 = InitData.SOUNDC2;
         this.soundc32 = InitData.SOUNDC32;
         ConfigAndStory.CKTIME = getTimer();
         ConfigAndStory.makeChange();
      }
      
      private function loop(param1:Event) : void
      {
         this.uictrl.run();
         var _loc2_:uint = uint(getTimer());
         var _loc3_:uint = uint(getTimer());
         this.core.runGame();
         this.soundc2.runSound2();
         this.soundc32.run();
         this.text.text = "A&B_BETA 3.00  LogicFps: " + (1000 / (getTimer() - _loc3_)).toFixed(2) + " Sound:" + this.soundc32.sfxa1.num;
         var _loc4_:uint = uint(getTimer());
         this.core.runRender();
         this.text.appendText("  RenderFps: " + (1000 / (getTimer() - _loc4_)).toFixed(2));
         this.text.appendText("  Fps: " + (1000 / (getTimer() - _loc2_)).toFixed(2));
         this.text.appendText("  Time: " + ConfigAndStory.TIMEALL + "s");
         if(DEBUG)
         {
            this.text.appendText("  -Dev");
         }
      }
      
      public function showAds(param1:Function) : void
      {
         MochiAd.showInterLevelAd({
            "clip":root,
            "id":"0fbe1356f7457e01",
            "res":"640x480",
            "ad_finished":param1
         });
      }
   }
}

