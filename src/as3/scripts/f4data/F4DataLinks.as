package f4data
{
   import f4ui.f4partmc.F4PartMC;
   import flash.geom.Point;
   
   public class F4DataLinks
   {
      
      public var f4dataorg:F4DataOrg;
      
      public var parentPart:F4DataLinks;
      
      public var atHole:F4DataHole;
      
      public var point:Point;
      
      public var f4mc:F4PartMC;
      
      public var index:int = -1;
      
      public var name:String = "name";
      
      public var colorHue:int;
      
      public function F4DataLinks()
      {
         super();
         this.point = new Point();
         this.f4dataorg = new F4DataOrg();
      }
      
      public function getMass(param1:F4DataLinks) : Number
      {
         var _loc3_:F4DataHole = null;
         var _loc2_:Number = 0;
         for each(_loc3_ in param1.f4dataorg.hole)
         {
            if(_loc3_.partf4)
            {
               _loc2_ += _loc3_.partf4.f4dataorg.mass;
               _loc2_ += this.getMass(_loc3_.partf4);
            }
         }
         return _loc2_;
      }
      
      public function readFromObj(param1:Object, param2:Array, param3:Array = null, param4:Array = null) : void
      {
         this.f4dataorg = param2[param1["self"]];
         this.index = param1["index"];
         this.name = param1["name"];
         if(Boolean(param3) && Boolean(param4))
         {
            this.f4dataorg = this.f4dataorg.clone(param3,param4);
         }
         this.f4dataorg.useAs = param1["useAs"];
         this.f4LinkLA(this,param1,param2,param3,param4);
      }
      
      public function writeObj() : Object
      {
         var _loc1_:Object = this.f4LinkSA(this);
         _loc1_["index"] = this.index;
         _loc1_["name"] = this.name;
         _loc1_["self"] = this.f4dataorg.index;
         _loc1_["useAs"] = this.f4dataorg.useAs;
         return _loc1_;
      }
      
      public function f4LinkSA(param1:F4DataLinks) : Object
      {
         var _loc3_:F4DataHole = null;
         var _loc2_:Object = new Object();
         _loc2_["thole"] = new Object();
         _loc2_["use"] = new Object();
         var _loc4_:int = int(param1.f4dataorg.hole.length);
         var _loc5_:int = 0;
         while(_loc5_ < _loc4_)
         {
            _loc3_ = param1.f4dataorg.hole[_loc5_];
            if(_loc3_.partf4)
            {
               _loc2_["thole"]["" + _loc5_] = _loc3_.partf4.f4dataorg.index;
               _loc2_["use"]["" + _loc5_] = _loc3_.partf4.f4dataorg.useAs;
               _loc2_["hole" + _loc5_] = this.f4LinkSA(_loc3_.partf4);
            }
            _loc5_++;
         }
         return _loc2_;
      }
      
      public function f4LinkLA(param1:F4DataLinks, param2:Object, param3:Array, param4:Array = null, param5:Array = null) : void
      {
         var _loc6_:F4DataLinks = null;
         var _loc7_:* = undefined;
         for(_loc7_ in param2["thole"])
         {
            _loc6_ = new F4DataLinks();
            _loc6_.f4dataorg = param3[param2["thole"][_loc7_]];
            if(Boolean(param4) && Boolean(param5))
            {
               _loc6_.f4dataorg = _loc6_.f4dataorg.clone(param4,param5);
            }
            _loc6_.f4dataorg.useAs = param2["use"][_loc7_];
            (param1.f4dataorg.hole[_loc7_] as F4DataHole).partf4 = _loc6_;
            _loc6_.atHole = param1.f4dataorg.hole[_loc7_] as F4DataHole;
            if(param2["hole" + _loc7_])
            {
               this.f4LinkLA(_loc6_,param2["hole" + _loc7_],param3,param4,param5);
            }
         }
      }
      
      public function clone(param1:Array, param2:Array, param3:Array) : F4DataLinks
      {
         var _loc4_:F4DataLinks = new F4DataLinks();
         _loc4_.readFromObj(this.writeObj(),param1,param2,param3);
         return _loc4_;
      }
      
      public function getRoot() : F4DataLinks
      {
         var _cur:F4DataLinks = this;
         while(_cur.parentPart)
         {
            _cur = _cur.parentPart;
         }
         return _cur;
      }
      
      public function countWings() : int
      {
         var _cnt:int = 0;
         if(this.f4dataorg && this.f4dataorg.type == 1)
         {
            _cnt++;
         }
         if(this.f4dataorg && this.f4dataorg.hole)
         {
            for each(var _h:F4DataHole in this.f4dataorg.hole)
            {
               if(_h && _h.partf4)
               {
                  _cnt += _h.partf4.countWings();
               }
            }
         }
         return _cnt;
      }
   }
}

