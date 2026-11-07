package ui.pages
{
   import f4data.sdata.ConfigAndStory;
   import flash.display.Bitmap;
   import flash.display.BitmapData;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.events.Event;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   
   public class UIMovie extends UIBase
   {
      
      public var endFunc:Function;
      
      public var movs:Array;
      
      public var index:int;
      
      public var movMC:MovieClip;
      
      public var movBmp:Bitmap;
      
      public var timer:uint;
      
      public var alphaAdd:int = -1;
      
      public function UIMovie(param1:Function, param2:Array, param3:DisplayObjectContainer, param4:MovieClip, param5:String, param6:UICtrl, param7:UIBase = null)
      {
         this.initMovs(param2);
         this.timer = 0;
         this.endFunc = param1;
         super(param3,param4,param5,param6,param7);
      }
      
      override public function afterFadeIn() : void
      {
         this.movBmp = new Bitmap(new BitmapData(640,480));
         this.movBmp.x = 0;
         this.movBmp.y = 0;
         this.movBmp.alpha = 0;
         _mov.addChildAt(this.movBmp,0);
         _mov.addEventListener(Event.ENTER_FRAME,this.alwaysLoop);
         this.nextMovie();
         super.afterFadeIn();
      }
      
      public function initMovs(param1:Array) : void
      {
         this.movs = [];
         var _loc2_:int = 0;
         while(_loc2_ < param1.length)
         {
            this.movs.push(ConfigAndStory.ALLPART.movie.movData[param1[_loc2_]]);
            _loc2_++;
         }
         this.index = 0;
      }
      
      private function alwaysLoop(param1:Event) : void
      {
         if(this.movMC)
         {
            if(this.movMC.currentFrame == this.movMC.totalFrames)
            {
               this.movMC.stop();
               ++this.timer;
               if(this.timer == 2.5 * 60)
               {
                  this.alphaAdd = 1;
                  this.timer = 0;
                  this.drawAndEnd();
               }
            }
         }
         if(this.alphaAdd == 0)
         {
            this.movBmp.alpha += 0.1;
            if(this.movBmp.alpha >= 1)
            {
               this.movBmp.visible = false;
               this.alphaAdd = -1;
               this.movMC.visible = true;
               this.movMC.play();
            }
         }
         else if(this.alphaAdd == 1)
         {
            this.movBmp.alpha -= 0.1;
            if(this.movBmp.alpha <= 0)
            {
               this.alphaAdd = -1;
               this.nextMovie();
            }
         }
      }
      
      public function drawAndEnd() : void
      {
         this.movBmp.bitmapData.fillRect(this.movBmp.bitmapData.rect,0);
         this.movBmp.bitmapData.draw(this.movMC);
         this.movBmp.visible = true;
         this.movBmp.alpha = 1;
         this.movMC.stop();
         _mov.removeChild(this.movMC);
         this.movMC = null;
      }
      
      public function startAddMC(param1:String) : void
      {
         this.movMC = Attach.attach(param1) as MovieClip;
         this.movMC.gotoAndStop(1);
         this.movBmp.bitmapData.draw(this.movMC);
         this.movMC.visible = false;
         this.movBmp.visible = true;
         this.movBmp.alpha = 0;
         _mov.addChildAt(this.movMC,0);
         this.alphaAdd = 0;
      }
      
      override public function removeBtnL() : void
      {
         this.movBmp.bitmapData.dispose();
         _mov.removeEventListener(Event.ENTER_FRAME,this.alwaysLoop);
         super.removeBtnL();
      }
      
      public function nextMovie() : void
      {
         var _loc1_:Object = null;
         if(this.index < this.movs.length)
         {
            _loc1_ = this.movs[this.index];
            this.startAddMC(_loc1_["mcName"]);
            ++this.index;
         }
         else
         {
            removeSelf();
            if(this.endFunc != null)
            {
               this.endFunc();
            }
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc2_:String = param1.name;
         if(_loc2_ == "skip")
         {
            if(this.alphaAdd == -1)
            {
               this.timer = 0;
               this.alphaAdd = 1;
               this.drawAndEnd();
               MochiEvents.trackEvent("skip_mov",ConfigAndStory.STORY);
            }
         }
      }
   }
}

