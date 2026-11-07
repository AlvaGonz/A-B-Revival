package fullshit.ctrl
{
   import flash.display.Stage;
   import flash.events.KeyboardEvent;
   import flash.utils.Dictionary;
   
   public class KeyCtrl
   {
      
      public static const UP:String = "&";
      
      public static const DOWN:String = "(";
      
      public static const LEFT:String = "%";
      
      public static const RIGHT:String = "\'";
      
      public static const ENTER:String = "\r";
      
      public static const ESC:String = "\x1b";
      
      private var _keyL:Stage;
      
      protected var _keyArr:Dictionary;
      
      protected var _keyArrN:Dictionary;
      
      public function KeyCtrl(param1:Stage)
      {
         super();
         this._keyL = param1;
         this._keyArr = new Dictionary();
         this._keyArrN = new Dictionary();
         this._keyL.addEventListener(KeyboardEvent.KEY_UP,this.keyUpE);
         this._keyL.addEventListener(KeyboardEvent.KEY_DOWN,this.keyDownE);
      }
      
      public function removeAll() : void
      {
         this._keyL.removeEventListener(KeyboardEvent.KEY_UP,this.keyUpE);
         this._keyL.removeEventListener(KeyboardEvent.KEY_DOWN,this.keyDownE);
      }
      
      private function keyUpE(param1:KeyboardEvent) : void
      {
         var _loc2_:String = String.fromCharCode(param1.keyCode);
         delete this._keyArr[_loc2_];
         delete this._keyArrN[_loc2_];
         delete this._keyArr["#" + param1.keyCode];
         delete this._keyArrN["#" + param1.keyCode];
      }
      
      private function keyDownE(param1:KeyboardEvent) : void
      {
         this._keyArr[String.fromCharCode(param1.keyCode)] = true;
         this._keyArr["#" + param1.keyCode] = true;
      }
      
      public function isKeyDown(param1:String) : Boolean
      {
         return this._keyArr[param1];
      }
      
      public function isKeyDownOnce(param1:String) : Boolean
      {
         if(Boolean(this._keyArr[param1]) && !this._keyArrN[param1])
         {
            this._keyArrN[param1] = true;
            return true;
         }
         return false;
      }

      public function isKeyCodeDownOnce(param1:uint) : Boolean
      {
         var _key:String = "#" + param1;
         if(Boolean(this._keyArr[_key]) && !this._keyArrN[_key])
         {
            this._keyArrN[_key] = true;
            return true;
         }
         return false;
      }
   }
}

