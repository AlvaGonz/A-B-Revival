package ui.base
{
   import flash.display.Bitmap;
   import flash.display.BitmapData;
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.events.Event;
   import fullshit.AbsMain;
   import ui.UICtrl;
   
   public class UIBase
   {
      
      protected var _sp:DisplayObjectContainer;
      
      protected var _mov:MovieClip;
      
      protected var _btnArr:Array;
      
      protected var _last:UIBase;
      
      protected var _uiName:String;
      
      protected var _ui:UICtrl;
      
      protected var _bmp:Bitmap;
      
      public var allRemoved:Function;
      
      public function UIBase(param1:DisplayObjectContainer, param2:MovieClip, param3:String, param4:UICtrl, param5:UIBase = null)
      {
         super();
         this._uiName = param3;
         this._last = param5;
         this._sp = param1;
         this._mov = param2;
         this._btnArr = [];
         this._bmp = new Bitmap(new BitmapData(AbsMain.w,AbsMain.h,true,0));
         this._bmp.alpha = 0;
         this._ui = param4;
         if(this._last)
         {
            this._last.removeBtnL();
            this.addToView(false);
         }
         else
         {
            this.addToView();
         }
         this.drawOneStart();
      }
      
      public function get sp() : DisplayObjectContainer
      {
         return this._sp;
      }
      
      public function get uictrl() : UICtrl
      {
         return this._ui;
      }
      
      protected function drawOneStart() : void
      {
         this._bmp.bitmapData.draw(this._mov);
         this._sp.addChildAt(this._bmp,this._sp.getChildIndex(this._mov));
         this._mov.visible = false;
         this._sp.addEventListener(Event.ENTER_FRAME,this.alphaRunStart);
      }
      
      private function alphaRunStart(param1:Event) : void
      {
         this._bmp.alpha += 0.1;
         if(this._bmp.alpha >= 1)
         {
            this._sp.removeEventListener(Event.ENTER_FRAME,this.alphaRunStart);
            this._sp.removeChild(this._bmp);
            this._mov.visible = true;
            this.initArr();
            this.afterFadeIn();
         }
      }
      
      public function afterFadeIn() : void
      {
      }
      
      public function get mov() : MovieClip
      {
         return this._mov;
      }
      
      public function get uiName() : String
      {
         return this._uiName;
      }
      
      public function removeSelf() : UIBase
      {
         this.removeBtnL();
         this.startToRemove();
         if(this._last)
         {
            this._last.addBtnL();
         }
         return this._last;
      }
      
      public function addToView(param1:Boolean = true) : void
      {
         if(param1)
         {
            this._sp.addChildAt(this._mov,0);
         }
         else
         {
            this._sp.addChild(this._mov);
         }
      }
      
      protected function startToRemove() : void
      {
         this._bmp.bitmapData.fillRect(this._bmp.bitmapData.rect,0);
         this._bmp.bitmapData.draw(this._mov);
         this._sp.addChildAt(this._bmp,this._sp.getChildIndex(this._mov));
         this._sp.removeChild(this._mov);
         this._sp.addEventListener(Event.ENTER_FRAME,this.alphaRun);
      }
      
      private function alphaRun(param1:Event) : void
      {
         this._bmp.alpha -= 0.1;
         if(this._bmp.alpha <= 0)
         {
            this._sp.removeEventListener(Event.ENTER_FRAME,this.alphaRun);
            this._bmp.bitmapData.dispose();
            this._sp.removeChild(this._bmp);
            if(this.allRemoved != null)
            {
               this.allRemoved();
            }
            trace("bmpd dispose");
         }
      }
      
      protected function removeView() : void
      {
         this._sp.removeChild(this._mov);
      }
      
      public function removeAll() : void
      {
         this.removeSelf();
         if(this._last)
         {
            this._last.removeAll();
         }
      }
      
      public function removeBtnL() : void
      {
         var _loc1_:BtnBase = null;
         for each(_loc1_ in this._btnArr)
         {
            _loc1_.removeL();
         }
      }
      
      public function addBtnL() : void
      {
         var _loc1_:BtnBase = null;
         for each(_loc1_ in this._btnArr)
         {
            _loc1_.initL();
         }
      }
      
      public function act(param1:BtnBase) : void
      {
      }
      
      public function downAct(param1:String) : void
      {
      }
      
      public function overAct(param1:String) : void
      {
      }
      
      public function outAct(param1:String) : void
      {
      }
      
      protected function initArr() : void
      {
         var _loc1_:DisplayObject = null;
         var _loc2_:DisplayObjectContainer = null;
         var _loc3_:int = 0;
         var _loc4_:int = 0;
         this._mov.mouseEnabled = false;
         var _loc5_:Array = [this._mov];
         while(_loc5_.length > 0)
         {
            _loc1_ = _loc5_[_loc5_.length - 1];
            --_loc5_.length;
            if(_loc1_ is DisplayObjectContainer)
            {
               _loc2_ = _loc1_ as DisplayObjectContainer;
               _loc3_ = _loc2_.numChildren;
               _loc4_ = 0;
               while(_loc4_ < _loc3_)
               {
                  _loc1_ = _loc2_.getChildAt(_loc4_);
                  if(_loc1_ is DisplayObjectContainer)
                  {
                     _loc5_.push(_loc1_);
                  }
                  if(Boolean(_loc1_) && _loc1_.name.charAt(0) == "$")
                  {
                     this._btnArr.push(new BtnBase(_loc1_,_loc1_.name.substr(1),this));
                  }
                  _loc4_++;
               }
            }
         }
      }
      
      public function disableBtn(param1:String) : void
      {
         var _loc2_:BtnBase = null;
         for each(_loc2_ in this._btnArr)
         {
            if(_loc2_.name == param1)
            {
               _loc2_.disable();
            }
         }
      }
      
      public function enableBtn(param1:String) : void
      {
         var _loc2_:BtnBase = null;
         for each(_loc2_ in this._btnArr)
         {
            if(_loc2_.name == param1)
            {
               _loc2_.enable();
            }
         }
      }
   }
}

