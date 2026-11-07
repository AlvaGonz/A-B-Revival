package ui.base
{
   import flash.display.Bitmap;
   import flash.display.BitmapData;
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.filters.GlowFilter;
   import flash.text.TextField;
   import flash.text.TextFormat;
   import flash.text.TextFormatAlign;
   import fullshit.AbsMain;
   import ui.UICtrl;
   
   public class UIBase
   {
      // Typography & Styling Tokens (consolidated from UIStyles per ADR-0009)
      public static const FONT_TITLE:String = "SF TransRobotics";
      public static const FONT_HEADER:String = "SF Zero Gravity";
      public static const FONT_BODY:String = "Arial";
      public static const FONT_SANS:String = "_sans";

      // Size hierarchy tokens
      public static const SIZE_SMALL:int = 10;
      public static const SIZE_HUD:int = 11;
      public static const SIZE_ALERT:int = 13;
      public static const SIZE_HEADER:int = 16;
      public static const SIZE_TITLE:int = 20;

      // Color tokens
      public static const COLOR_SYSTEM_CYAN:uint = 0x00CCFF;
      public static const COLOR_SYSTEM_LIGHT:uint = 0x66CCFF;
      public static const COLOR_DAMAGE_CRIMSON:uint = 0xFF5555;
      public static const COLOR_DAMAGE_RED:uint = 0xFF3333;
      public static const COLOR_CAPTAIN_AMBER:uint = 0xFFCC00;
      public static const COLOR_CAPTAIN_GOLD:uint = 0xFFAA00;
      public static const COLOR_SUCCESS_MINT:uint = 0x44FF88;
      public static const COLOR_SUCCESS:uint = 0x44FF88;
      public static const COLOR_ACCENT_CYAN:uint = 0x00CCFF;
      public static const COLOR_WHITE:uint = 0xFFFFFF;
      public static const COLOR_SILVER:uint = 0xBFBFBF;

      public static function createFormat(param1:String = "_sans", param2:int = 11, param3:uint = 0x00CCFF, param4:Boolean = true, param5:String = "center") : TextFormat
      {
         var _loc6_:TextFormat = new TextFormat(param1, param2, param3, param4);
         _loc6_.align = param5;
         return _loc6_;
      }

      public static function applyFormat(param1:TextField, param2:String = "_sans", param3:int = 11, param4:uint = 0x00CCFF, param5:Boolean = true, param6:String = "center") : TextFormat
      {
         var _loc7_:TextFormat = createFormat(param2, param3, param4, param5, param6);
         param1.defaultTextFormat = _loc7_;
         param1.setTextFormat(_loc7_);
         return _loc7_;
      }

      public static function applyGlow(param1:TextField, param2:uint = 0x000000, param3:Number = 0.9, param4:Number = 3, param5:Number = 3) : void
      {
         param1.filters = [new GlowFilter(param2, param3, param4, param4, param5, 1)];
      }

      public static function applyHoloGlow(param1:TextField, param2:uint, param3:Number = 3) : void
      {
         var _loc4_:GlowFilter = new GlowFilter(0x000000, 0.9, param3, param3, 3, 1);
         var _loc5_:GlowFilter = new GlowFilter(param2, 0.45, param3 * 2, param3 * 2, 1.5, 1);
         param1.filters = [_loc4_, _loc5_];
      }
      
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

