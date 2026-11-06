package
{
   import flash.display.DisplayObject;
   import flash.display.Loader;
   import flash.display.LoaderInfo;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.net.URLRequest;
   import flash.net.navigateToURL;
   import flash.text.TextField;
   import flash.utils.getDefinitionByName;
   import mochi.as3.MochiAd;
   import mochi.as3.MochiServices;
   
   [SWF(width="640", height="480", backgroundColor="#000000", frameRate="36")]
   public dynamic class VeryOutShitABC extends MovieClip
   {
      
      public static var VGPLAY:Boolean = false;
      
      public static var NOADS:Boolean = false;
      
      public const loading:Class = VeryOutShitABC_loading;
      
      public var loadMC:MovieClip;
      
      public var loader:Loader;
      
      public const noAdsUrls:Array = ["kongregate.com","flashgamelicense.com"];
      
      public function VeryOutShitABC()
      {
         super();
         stop();
         if(stage)
         {
            this.ckURL();
         }
         else
         {
            addEventListener(Event.ADDED_TO_STAGE,this.addToStage);
         }
      }
      
      private function ckURL() : void
      {
         var _loc2_:String = null;
         MochiServices.connect("0fbe1356f7457e01",this);
         var _loc1_:String = this.loaderInfo.loaderURL;
         for each(_loc2_ in this.noAdsUrls)
         {
            if(_loc1_.indexOf(_loc2_) != -1)
            {
               NOADS = true;
               break;
            }
         }
         VGPLAY = _loc1_.indexOf("vargames.com") != -1;
         if(NOADS || VGPLAY)
         {
            this.readyToLoading();
         }
         else
         {
            this.readyToAds();
         }
      }
      
      private function readyToAds() : void
      {
         MochiAd.showPreGameAd({
            "clip":root,
            "id":"0fbe1356f7457e01",
            "res":"640x480",
            "ad_finished":this.readyToLoading
         });
      }
      
      private function addToStage(param1:Event) : void
      {
         removeEventListener(Event.ADDED_TO_STAGE,this.addToStage);
         this.ckURL();
      }
      
      private function readyToLoading() : void
      {
         this.loader = new Loader();
         this.loader.contentLoaderInfo.addEventListener(Event.COMPLETE,this.uiCop);
         this.loader.loadBytes(new this.loading());
      }
      
      private function uiCop(param1:Event) : void
      {
         var _loc2_:LoaderInfo = param1.currentTarget as LoaderInfo;
         this.loader.contentLoaderInfo.removeEventListener(Event.COMPLETE,this.uiCop);
         var _loc3_:Class = _loc2_.applicationDomain.getDefinition("UI_Loading") as Class;
         this.loadMC = new _loc3_();
         stage.addChild(this.loadMC);
         stage.addEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         stage.addEventListener(MouseEvent.MOUSE_DOWN,this.toVargames);
      }
      
      private function toVargames(param1:MouseEvent) : void
      {
         navigateToURL(new URLRequest("http://www.vargames.com"),"_blank");
      }
      
      public function onEnterFrame(param1:Event) : void
      {
         var _loc2_:Number = stage.loaderInfo.bytesLoaded / stage.loaderInfo.bytesTotal;
         (this.loadMC.getChildByName("txtLoad") as TextField).text = int(_loc2_ * 100) + "%";
         if(framesLoaded == totalFrames)
         {
            stage.removeChild(this.loadMC);
            this.loader.contentLoaderInfo.removeEventListener(Event.COMPLETE,this.uiCop);
            stage.removeEventListener(Event.ENTER_FRAME,this.onEnterFrame);
            stage.removeEventListener(MouseEvent.MOUSE_DOWN,this.toVargames);
            nextFrame();
            this.create();
         }
      }
      
      private function create() : void
      {
         var _loc1_:Class = Class(getDefinitionByName("Fly4UI"));
         var _loc2_:Object = new _loc1_();
         addChildAt(_loc2_ as DisplayObject,0);
         _loc2_.initSup();
      }
   }
}

