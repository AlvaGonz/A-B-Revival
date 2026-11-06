package f4data
{
   public class F4DataFire
   {
      
      public var fireAcc:Number = 0;
      
      public var fireEx:int = 1;
      
      public var fireStop:int = 0;
      
      public var fireFrame:int = 0;
      
      public var fireNum:int = 0;
      
      public var angS:Number = 0;
      
      public var angAdd:Number = 0;
      
      public var accS:Number = 0;
      
      public var accAdd:Number = 0;
      
      public var accAngS:Number = 0;
      
      public var accAngAdd:Number = 0;
      
      public var spdS:Number = 0;
      
      public var spdAdd:Number = 0;
      
      public var index:int = 0;
      
      public function F4DataFire()
      {
         super();
      }
      
      public function readFromObj(param1:Object) : void
      {
         this.fireAcc = param1["fireAcc"];
         this.fireEx = param1["fireEx"];
         this.fireStop = param1["fireStop"];
         this.fireFrame = param1["fireFrame"];
         this.fireNum = param1["fireNum"];
         this.angS = param1["angS"];
         this.angAdd = param1["angAdd"];
         this.accS = param1["accS"];
         this.accAdd = param1["accAdd"];
         this.spdS = param1["spdS"];
         this.spdAdd = param1["spdAdd"];
         this.accAngS = param1["accAngS"];
         this.accAngAdd = param1["accAngAdd"];
         this.index = param1["index"];
      }
      
      public function writeObj() : Object
      {
         var _loc1_:Object = new Object();
         _loc1_["fireAcc"] = this.fireAcc;
         _loc1_["fireEx"] = this.fireEx;
         _loc1_["fireStop"] = this.fireStop;
         _loc1_["fireFrame"] = this.fireFrame;
         _loc1_["fireNum"] = this.fireNum;
         _loc1_["angS"] = this.angS;
         _loc1_["angAdd"] = this.angAdd;
         _loc1_["accS"] = this.accS;
         _loc1_["accAdd"] = this.accAdd;
         _loc1_["spdS"] = this.spdS;
         _loc1_["spdAdd"] = this.spdAdd;
         _loc1_["accAngS"] = this.accAngS;
         _loc1_["accAngAdd"] = this.accAngAdd;
         _loc1_["index"] = this.index;
         return _loc1_;
      }
      
      public function get firePowRate() : Number
      {
         return (0.5 * this.accAngS + this.spdS * 15) * this.fireNum / (this.fireEx / this.fireFrame) + 1000 / (this.fireFrame + this.fireStop);
      }
   }
}

