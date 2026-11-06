package ui.base
{
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   import flash.filters.ColorMatrixFilter;
   import fullshit.render.ColorMatrixAdd;
   
   public class BtnBase
   {
      
      public var name:String;
      
      private var _btnDis:DisplayObject;
      
      private var _ui:UIBase;
      
      private var _sound:Boolean;
      
      public function BtnBase(param1:DisplayObject, param2:String, param3:UIBase)
      {
         super();
         this._btnDis = param1;
         if(this._btnDis is MovieClip)
         {
            (this._btnDis as MovieClip).mouseChildren = true;
            (this._btnDis as MovieClip).gotoAndStop(1);
         }
         var _loc4_:int = param2.indexOf("MAKESOUND");
         if(_loc4_ == -1)
         {
            this._sound = false;
            this.name = param2;
         }
         else
         {
            this._sound = true;
            this.name = param2.substr(0,_loc4_);
         }
         this._ui = param3;
         this.initL();
      }
      
      public function get parentName() : String
      {
         if(this._btnDis.parent)
         {
            return this._btnDis.parent.name;
         }
         return null;
      }
      
      public function disable() : void
      {
         if(this._btnDis is Sprite)
         {
            (this._btnDis as Sprite).mouseEnabled = false;
            (this._btnDis as Sprite).mouseChildren = false;
         }
         var _loc1_:ColorMatrixFilter = new ColorMatrixFilter();
         var _loc2_:ColorMatrixAdd = new ColorMatrixAdd(_loc1_);
         _loc2_.setSaturation(-1);
         _loc2_.setColor();
         this._btnDis.filters = [_loc1_];
      }
      
      public function enable() : void
      {
         if(this._btnDis is Sprite)
         {
            (this._btnDis as Sprite).mouseEnabled = true;
            (this._btnDis as Sprite).mouseChildren = true;
         }
         this._btnDis.filters = [];
      }
      
      public function initL() : void
      {
         this._btnDis.addEventListener(MouseEvent.MOUSE_DOWN,this.mouseDown);
         this._btnDis.addEventListener(MouseEvent.MOUSE_UP,this.mouseUp);
         this._btnDis.addEventListener(MouseEvent.ROLL_OUT,this.mouseOut);
         this._btnDis.addEventListener(MouseEvent.MOUSE_OVER,this.mouseOver);
      }
      
      private function mouseOver(param1:MouseEvent) : void
      {
         this._ui.overAct(this.name);
         if(this.name.indexOf("choose") == -1 && this._btnDis is MovieClip)
         {
            (this._btnDis as MovieClip).gotoAndStop(2);
         }
      }
      
      private function mouseOut(param1:MouseEvent) : void
      {
         this._ui.outAct(this.name);
         if(this.name.indexOf("choose") == -1 && this._btnDis is MovieClip)
         {
            (this._btnDis as MovieClip).gotoAndStop(1);
         }
      }
      
      private function mouseUp(param1:MouseEvent) : void
      {
         this._ui.act(this);
         if(this.name.indexOf("choose") == -1 && this._btnDis is MovieClip)
         {
            (this._btnDis as MovieClip).gotoAndStop(1);
         }
      }
      
      private function mouseDown(param1:MouseEvent) : void
      {
         if(this.name.indexOf("choose") == -1 && this._btnDis is MovieClip)
         {
            (this._btnDis as MovieClip).gotoAndStop(2);
         }
      }
      
      public function removeL() : void
      {
         this._btnDis.removeEventListener(MouseEvent.MOUSE_DOWN,this.mouseDown);
         this._btnDis.removeEventListener(MouseEvent.MOUSE_UP,this.mouseUp);
         this._btnDis.removeEventListener(MouseEvent.MOUSE_OUT,this.mouseOut);
         this._btnDis.removeEventListener(MouseEvent.MOUSE_OVER,this.mouseOver);
      }
   }
}

