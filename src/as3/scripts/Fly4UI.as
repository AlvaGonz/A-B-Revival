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
   import flash.system.LoaderContext;
   import flash.text.TextField;
   import flash.utils.ByteArray;
   import flash.utils.getTimer;
   import fullshit.AbsMain;
   import fullshit.ctrl.KeyCtrl;
   import fullshit.ctrl2.SoundCtrl2;
   import flash.system.Capabilities;
   import flash.system.System;
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import f4.data.Targets;
   import mochi.as3.MochiAd;
   import ui.UICtrl;
   
   public dynamic class Fly4UI extends AbsMain
   {
      
      public static var MAINFLY4:Fly4UI;
      
      public static var DOMAIN:ApplicationDomain;
      
      public static var LVDATA:Object;
      
      public static var DEFFLYDATA:Object;
      
      public static var STARTDATA:ByteArray;
      
      public static var DEBUG:Boolean = false;
      
      public static const BUILD:String = "BUILD 111214_2";
      
      public var key:KeyCtrl;
      
      public var ui_out:Sprite;
      
      public var uictrl:UICtrl;
      
      public var core:OpOneCore;
      
      public var soundc2:SoundCtrl2;
      
      public var soundc32:SoundCtrl32;
      
      public var loadNum:int = 0;
      
      public var alldata:AllData;
      
      private var _lastFrameTimer:uint = 0;
      
      private var devOverlay:DevMetricsOverlay;
      
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
         var _ctx1:LoaderContext = new LoaderContext();
         _ctx1.allowCodeImport = true;
         _loc1_.loadBytes(new this.alldata.data(),_ctx1);
      }
      
      public function soundLoad() : void
      {
         var _loc1_:Loader = new Loader();
         _loc1_.contentLoaderInfo.addEventListener(Event.COMPLETE,this.soundCop);
         var _ctx2:LoaderContext = new LoaderContext();
         _ctx2.allowCodeImport = true;
         _loc1_.loadBytes(new this.alldata.sound(),_ctx2);
      }
      
      public function uiLoad() : void
      {
         var _loc1_:Loader = new Loader();
         _loc1_.contentLoaderInfo.addEventListener(Event.COMPLETE,this.uiCop);
         var _ctx3:LoaderContext = new LoaderContext();
         _ctx3.allowCodeImport = true;
         _loc1_.loadBytes(new this.alldata.ui(),_ctx3);
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
         this.devOverlay = new DevMetricsOverlay(stage);
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
         if(flash.system.Capabilities.isDebugger)
         {
            if(!Fly4UI.DEBUG && this.devOverlay)
            {
               this.devOverlay.toggle(stage);
            }
         }
      }
      
      private function loop(param1:Event) : void
      {
         if(this.key)
         {
            if(this.key.isKeyDownOnce("I") || this.key.isKeyDownOnce("i") || this.key.isKeyCodeDownOnce(192) || this.key.isKeyCodeDownOnce(112))
            {
               if(this.devOverlay)
               {
                  this.devOverlay.toggle(stage);
               }
            }
            if(this.key.isKeyCodeDownOnce(122))
            {
               ConfigAndStory.toggleFullscreen();
            }
         }

         var _t0:uint = uint(getTimer());
         var _frameDelta:uint = this._lastFrameTimer > 0 ? uint(_t0 - this._lastFrameTimer) : 16;
         this._lastFrameTimer = _t0;

         var _t1:uint = uint(getTimer());
         this.uictrl.run();
         this.core.runGame();
         this.soundc2.runSound2();
         this.soundc32.run();
         var _logicMs:uint = uint(getTimer() - _t1);

         var _t2:uint = uint(getTimer());
         this.core.runRender();
         var _renderMs:uint = uint(getTimer() - _t2);

         if(this.devOverlay)
         {
            this.devOverlay.update(stage, _frameDelta, _logicMs, _renderMs);
         }
      }
      
      public function showAds(callback:Function) : void
      {
         if(callback is Function)
         {
            callback();
         }
      }
   }
}

import f4.data.Targets;
import f4.data.InitData;
import f4data.sdata.ConfigAndStory;
import flash.display.DisplayObject;
import flash.display.DisplayObjectContainer;
import flash.display.Stage;
import flash.system.System;
import flash.text.TextField;

class DevMetricsOverlay
{
   public var text:TextField;
   private var _logCounter:int = 0;
   private var _cachedDObjCount:int = 0;
   
   public function DevMetricsOverlay(stage:Stage)
   {
      this.text = new TextField();
      this.text.y = 442;
      this.text.x = 8;
      this.text.textColor = 0x00FF66;
      this.text.selectable = false;
      this.text.width = 624;
      this.text.height = 34;
      this.text.multiline = true;
      this.text.wordWrap = true;
      this.text.background = true;
      this.text.backgroundColor = 0x000000;
      this.text.mouseWheelEnabled = false;
      this.text.mouseEnabled = false;
      this.text.alpha = 0.85;
      this.text.visible = false;
      if(stage)
      {
         stage.addChild(this.text);
      }
   }
   
   public function toggle(stage:Stage) : void
   {
      Fly4UI.DEBUG = !Fly4UI.DEBUG;
      if(this.text)
      {
         this.text.visible = Fly4UI.DEBUG;
         if(Fly4UI.DEBUG && stage && this.text.parent != stage)
         {
            stage.addChild(this.text);
         }
      }
      var _resStr:String = ConfigAndStory.RESOLUTION == ConfigAndStory.RES_1080 ? "1080p" : (ConfigAndStory.RESOLUTION == ConfigAndStory.RES_720 ? "720p" : "640x480");
      var _qStr:String = ConfigAndStory.QUALITY == ConfigAndStory.QUALITY_HIGH ? "High" : (ConfigAndStory.QUALITY == ConfigAndStory.QUALITY_LOW ? "Low" : "Med");
      var _fsStr:String = ConfigAndStory.FULLSCREEN ? "ON" : "OFF";
      if(Fly4UI.DEBUG)
      {
         trace("[DEV_SESSION_START] time=" + ConfigAndStory.TIMEALL + "s res=" + _resStr + " quality=" + _qStr + " fullscreen=" + _fsStr);
      }
      else
      {
         trace("[DEV_SESSION_END] time=" + ConfigAndStory.TIMEALL + "s");
      }
      trace(Fly4UI.DEBUG ? "[DEV_MODE] Enabled (FPS, frame time, memory, object counters active)" : "[DEV_MODE] Disabled");
   }
   
   public function update(stage:Stage, frameDelta:uint, logicMs:uint, renderMs:uint) : void
   {
      if(!Fly4UI.DEBUG || !this.text)
      {
         return;
      }
      if(stage && this.text.parent == stage)
      {
         stage.setChildIndex(this.text, stage.numChildren - 1);
      }
      if(++this._logCounter % 15 == 0)
      {
         this._cachedDObjCount = countDisplayObjects(stage);
      }
      var _targets:Targets = InitData.TARGETS;
      var _bullets:int = _targets ? ((_targets.pba1 ? _targets.pba1.num : 0) + (_targets.eba1 ? _targets.eba1.num : 0)) : 0;
      var _particles:int = _targets && _targets.pa1 ? _targets.pa1.num : 0;
      var _ships:int = _targets ? ((_targets.pfa1 ? _targets.pfa1.num : 0) + (_targets.efa1 ? _targets.efa1.num : 0)) : 0;
      var _drops:int = _targets && _targets.ia1 ? _targets.ia1.num : 0;
      var _debris:int = _targets && _targets.deadParta1 ? _targets.deadParta1.num : 0;
      var _arrays:int = _bullets + _particles + _ships + _drops + _debris + (_targets && _targets.swa1 ? _targets.swa1.num : 0) + (_targets && _targets.swa2 ? _targets.swa2.num : 0);
      var _memMb:String = (System.totalMemory / 1048576).toFixed(2);
      var _currentFps:Number = frameDelta > 0 ? (1000 / frameDelta) : (stage ? stage.frameRate : 60);

      var _resStr:String = ConfigAndStory.RESOLUTION == ConfigAndStory.RES_1080 ? "1080p" : (ConfigAndStory.RESOLUTION == ConfigAndStory.RES_720 ? "720p" : "640x480");
      var _qStr:String = ConfigAndStory.QUALITY == ConfigAndStory.QUALITY_HIGH ? "High" : (ConfigAndStory.QUALITY == ConfigAndStory.QUALITY_LOW ? "Low" : "Med");
      var _fsStr:String = ConfigAndStory.FULLSCREEN ? "ON" : "OFF";
      this.text.text = "FPS: " + _currentFps.toFixed(1) + " (" + frameDelta + "ms) [Logic: " + logicMs + "ms, Render: " + renderMs + "ms] | Mem: " + _memMb + " MB | DObjs: " + this._cachedDObjCount + " | " + _resStr + " | Q:" + _qStr + " | FS:" + _fsStr;
      this.text.appendText("\nBullets: " + _bullets + " | Particles: " + _particles + " | Ships: " + _ships + " | Drops: " + _drops + " | Debris: " + _debris + " | Arrays: " + _arrays);

      if(this._logCounter % 30 == 0)
      {
         trace("[PERF_METRICS] time=" + ConfigAndStory.TIMEALL + "s fps=" + _currentFps.toFixed(1) + " frameDelta=" + frameDelta + "ms logic=" + logicMs + "ms render=" + renderMs + "ms mem=" + _memMb + "MB bullets=" + _bullets + " particles=" + _particles + " ships=" + _ships + " drops=" + _drops + " debris=" + _debris + " dObjs=" + this._cachedDObjCount + " arrays=" + _arrays);
      }
   }
   
   private static function countDisplayObjects(container:DisplayObjectContainer) : int
   {
      if(!container)
      {
         return 0;
      }
      var count:int = 1;
      var len:int = container.numChildren;
      for(var i:int = 0; i < len; i++)
      {
         try
         {
            var child:DisplayObject = container.getChildAt(i);
            if(child is DisplayObjectContainer)
            {
               count += countDisplayObjects(child as DisplayObjectContainer);
            }
            else if(child != null)
            {
               count++;
            }
         }
         catch(e:Error)
         {
         }
      }
      return count;
   }
}

