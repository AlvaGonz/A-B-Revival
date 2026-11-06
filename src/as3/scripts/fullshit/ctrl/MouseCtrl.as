package fullshit.ctrl
{
   import flash.display.Stage;
   import flash.events.Event;
   import flash.events.MouseEvent;
   
   public class MouseCtrl
   {
      
      private var _mouseL:Stage;
      
      private var _isDown:Boolean;
      
      private var _isClick:Boolean;
      
      private var _isDownOnce:Boolean;
      
      public function MouseCtrl(param1:Stage)
      {
         super();
         this._mouseL = param1;
         this._isDown = false;
         this._isClick = false;
         this._isDownOnce = false;
         this._mouseL.addEventListener(Event.MOUSE_LEAVE,this.mouseLeave);
         this._mouseL.addEventListener(MouseEvent.MOUSE_DOWN,this.mouseDown);
         this._mouseL.addEventListener(MouseEvent.MOUSE_UP,this.mouseUp);
      }
      
      public function removeAll() : void
      {
         this._mouseL.removeEventListener(Event.MOUSE_LEAVE,this.mouseLeave);
         this._mouseL.removeEventListener(MouseEvent.MOUSE_DOWN,this.mouseDown);
         this._mouseL.removeEventListener(MouseEvent.MOUSE_UP,this.mouseUp);
      }
      
      private function mouseLeave(param1:Event) : void
      {
         this._isDown = false;
      }
      
      public function get mouseIsDown() : Boolean
      {
         return this._isDown;
      }
      
      public function get mouseIsDownOnce() : Boolean
      {
         var _loc1_:Boolean = this._isDownOnce && this._isDown;
         this._isDownOnce = false;
         return _loc1_;
      }
      
      private function mouseDown(param1:MouseEvent) : void
      {
         this._isDownOnce = true;
         this._isDown = true;
      }
      
      private function mouseUp(param1:MouseEvent) : void
      {
         this._isDown = false;
         this._isClick = true;
      }
      
      public function get isClick() : Boolean
      {
         var _loc1_:Boolean = this._isClick;
         this._isClick = false;
         return _loc1_;
      }
   }
}

