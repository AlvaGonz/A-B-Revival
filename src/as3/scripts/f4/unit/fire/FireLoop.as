package f4.unit.fire
{
   public class FireLoop
   {
      
      public var loopNum:int;
      
      public var angS:Number;
      
      public var angAdd:Number;
      
      public var angn:Number;
      
      public var spdS:Number;
      
      public var spdAdd:Number;
      
      public var spdn:Number;
      
      public var accS:Number;
      
      public var accAdd:Number;
      
      public var accn:Number;
      
      public var accAngS:Number;
      
      public var accAngAdd:Number;
      
      public var accAngn:Number;
      
      public var yoyo:Boolean;
      
      private var _mutNum:int;
      
      private var _timer:uint;
      
      public function FireLoop(param1:int, param2:Number, param3:Number, param4:Number, param5:Number, param6:Number, param7:Number, param8:Number, param9:Number, param10:Boolean, param11:int = 0)
      {
         super();
         this.loopNum = param1;
         this.angn = this.angS = param2;
         this.angAdd = param3;
         this.spdn = this.spdS = param4;
         this.spdAdd = param5;
         this.accn = this.accS = param6;
         this.accAdd = param7;
         this.accAngn = this.accAngS = param8;
         this.accAngAdd = param9;
         this.yoyo = param10;
         this._mutNum = 1;
         this._timer = param11;
      }
      
      public function runFl() : void
      {
         ++this._timer;
         this.angn += this.angAdd * this._mutNum;
         this.spdn += this.spdAdd * this._mutNum;
         this.accn += this.accAdd * this._mutNum;
         this.accAngn += this.accAngAdd;
         if(this._timer % this.loopNum == 0)
         {
            if(this.yoyo)
            {
               this._mutNum = -this._mutNum;
            }
            else
            {
               this.angn = this.angS;
               this.spdn = this.spdS;
               this.accn = this.accS;
               this.accAngn = this.accAngS;
            }
         }
      }
      
      public function reset() : void
      {
         this._timer = 0;
         this._mutNum = 1;
         this.angn = this.angS;
         this.spdn = this.spdS;
         this.accn = this.accS;
         this.accAngn = this.accAngS;
      }
   }
}

