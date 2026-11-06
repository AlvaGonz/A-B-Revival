package f4data
{
   import flash.geom.Point;
   
   public class F4DataHole
   {
      
      public var xy:Point;
      
      public var lr:int;
      
      public var type:Array;
      
      public var partf4:F4DataLinks;
      
      public function F4DataHole()
      {
         super();
         this.xy = new Point();
         this.type = [];
      }
      
      public function canWing() : Boolean
      {
         var _loc1_:int = int(this.type.length);
         var _loc2_:int = 0;
         while(_loc2_ < _loc1_)
         {
            if(this.type[_loc2_] == 1)
            {
               return true;
            }
            _loc2_++;
         }
         return false;
      }
      
      public function readFromObj(param1:Object) : void
      {
         this.type = param1["type"];
         this.lr = param1["lr"];
         this.xy = new Point(param1["xy"]["x"],param1["xy"]["y"]);
      }
      
      public function writeObj() : Object
      {
         var _loc1_:Object = new Object();
         _loc1_["xy"] = this.xy;
         _loc1_["lr"] = this.lr;
         _loc1_["type"] = this.type;
         return _loc1_;
      }
      
      public function acceptable(param1:F4DataLinks) : int
      {
         var _loc2_:* = undefined;
         if(this.lr != param1.f4dataorg.lr && this.lr != 2 && param1.f4dataorg.lr != 2)
         {
            return 1;
         }
         for each(_loc2_ in this.type)
         {
            if(param1.f4dataorg.type == _loc2_)
            {
               return 0;
            }
         }
         return 2;
      }
      
      public function acceptableOrg(param1:F4DataOrg) : int
      {
         var _loc2_:* = undefined;
         if(this.lr != param1.lr && this.lr != 2 && param1.lr != 2)
         {
            return 1;
         }
         for each(_loc2_ in this.type)
         {
            if(param1.type == _loc2_)
            {
               return 0;
            }
         }
         return 2;
      }
      
      public function strToType(param1:String) : void
      {
         var _loc2_:int = param1.length;
         var _loc3_:int = 0;
         while(_loc3_ < _loc2_)
         {
            this.type[_loc3_] = parseInt(param1.charAt(_loc3_));
            _loc3_++;
         }
      }
   }
}

