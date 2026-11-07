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
      
      public var text:TextField;
      
      public var loadNum:int = 0;
      
      public var alldata:AllData;
      
      private var _lastFrameTimer:uint = 0;
      
      private var _logCounter:int = 0;
      
      private var _cachedDObjCount:int = 0;
      
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
         if(DEBUG)
         {
            stage.addChild(this.text);
         }
      }
      
      public static function countDisplayObjects(param1:DisplayObjectContainer) : int
      {
         if(param1 == null)
         {
            return 0;
         }
         var _count:int = 1;
         var _len:int = param1.numChildren;
         var _i:int = 0;
         while(_i < _len)
         {
            try
            {
               var _child:DisplayObject = param1.getChildAt(_i);
               if(_child is DisplayObjectContainer)
               {
                  _count += countDisplayObjects(_child as DisplayObjectContainer);
               }
               else if(_child != null)
               {
                  _count++;
               }
            }
            catch(e:Error)
            {
            }
            _i++;
         }
         return _count;
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
         if(this.key)
         {
            if(this.key.isKeyDownOnce("I") || this.key.isKeyDownOnce("i") || this.key.isKeyCodeDownOnce(192) || this.key.isKeyCodeDownOnce(112))
            {
               DEBUG = !DEBUG;
               if(DEBUG)
               {
                  if(!this.text.parent && stage)
                  {
                     stage.addChild(this.text);
                  }
                  this.text.visible = true;
                  trace("[DEV_MODE] Enabled (FPS, frame time, memory, object counters active)");
               }
               else
               {
                  this.text.visible = false;
                  trace("[DEV_MODE] Disabled");
               }
            }
         }

         var _t0:uint = uint(getTimer());
         var _frameDelta:uint = this._lastFrameTimer > 0 ? uint(_t0 - this._lastFrameTimer) : 28;
         this._lastFrameTimer = _t0;

         this.uictrl.run();
         var _t1:uint = uint(getTimer());
         this.core.runGame();
         this.soundc2.runSound2();
         this.soundc32.run();
         var _logicMs:uint = uint(getTimer() - _t1);

         var _t2:uint = uint(getTimer());
         this.core.runRender();
         var _renderMs:uint = uint(getTimer() - _t2);

         if(DEBUG)
         {
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
            var _currentFps:Number = _frameDelta > 0 ? (1000 / _frameDelta) : (stage ? stage.frameRate : 60);

            this.text.text = "FPS: " + _currentFps.toFixed(1) + " (" + _frameDelta + "ms) [Logic: " + _logicMs + "ms, Render: " + _renderMs + "ms] | Mem: " + _memMb + " MB | DObjs: " + this._cachedDObjCount;
            this.text.appendText("\nBullets: " + _bullets + " | Particles: " + _particles + " | Ships: " + _ships + " | Drops: " + _drops + " | Debris: " + _debris + " | Arrays: " + _arrays);

            if(this._logCounter % 180 == 0)
            {
               trace("[PERF_METRICS] time=" + ConfigAndStory.TIMEALL + "s fps=" + _currentFps.toFixed(1) + " frameDelta=" + _frameDelta + "ms logic=" + _logicMs + "ms render=" + _renderMs + "ms mem=" + _memMb + "MB bullets=" + _bullets + " particles=" + _particles + " dObjs=" + this._cachedDObjCount + " arrays=" + _arrays);
            }
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

