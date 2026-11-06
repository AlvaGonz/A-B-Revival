package f4data
{
   import f4ui.f4partmc.F4PartMC;
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.geom.Point;
   
   public class MC2F4Data
   {
      
      public function MC2F4Data()
      {
         super();
      }
      
      public function readMcData(param1:MovieClip, param2:F4DataOrg) : void
      {
         var _loc6_:String = null;
         var _loc7_:String = null;
         var _loc8_:int = 0;
         var _loc10_:int = 0;
         var _loc11_:DisplayObject = null;
         var _loc12_:F4DataHole = null;
         var _loc3_:MovieClip = param1.getChildByName("SHOW") as MovieClip;
         var _loc4_:MovieClip = param1.getChildByName("HIT") as MovieClip;
         var _loc5_:int = 1;
         if(param2.lr == 1)
         {
            _loc5_ = -1;
         }
         var _loc9_:int = _loc3_.numChildren;
         param2.hit.x = _loc4_.width;
         param2.hit.y = _loc4_.height;
         _loc10_ = 0;
         while(_loc10_ < _loc9_)
         {
            _loc11_ = _loc3_.getChildAt(_loc10_);
            _loc6_ = _loc11_.name;
            if(_loc6_.charAt(0) == "L")
            {
               param2.linkOff.x = _loc11_.x * _loc5_;
               param2.linkOff.y = _loc11_.y;
            }
            else if(_loc6_.charAt(0) == "H")
            {
               _loc6_ = _loc6_.substr(2);
               _loc8_ = parseInt(_loc6_.substring(0,_loc6_.indexOf("_")));
               _loc7_ = _loc6_.substring(_loc6_.indexOf("_") + 1);
               _loc12_ = new F4DataHole();
               _loc12_.parentOrg = param2;
               _loc12_.strToType(_loc7_);
               _loc12_.lr = _loc8_;
               if(param2.lr == 1 && _loc8_ == 0)
               {
                  _loc12_.lr = 1;
               }
               _loc12_.xy.x = _loc11_.x * _loc5_;
               _loc12_.xy.y = _loc11_.y;
               param2.hole.push(_loc12_);
            }
            else if(_loc6_.charAt(0) == "F")
            {
               param2.firePoints.push(_loc11_.x * _loc5_);
               param2.firePoints.push(_loc11_.y);
            }
            else if(_loc6_.charAt(0) == "P")
            {
               param2.partPoint.x = _loc11_.x * _loc5_;
               param2.partPoint.y = _loc11_.y;
            }
            _loc10_++;
         }
      }
      
      public function readMcDataBlt(param1:MovieClip, param2:F4DataBullet) : void
      {
         var _loc6_:int = 0;
         var _loc7_:DisplayObject = null;
         var _loc8_:String = null;
         var _loc3_:MovieClip = param1.getChildByName("HIT") as MovieClip;
         var _loc4_:MovieClip = param1.getChildByName("SHOW") as MovieClip;
         param2.hit = new Point();
         param2.hit.x = _loc3_.width;
         param2.hit.y = _loc3_.height;
         var _loc5_:int = _loc4_.numChildren;
         _loc6_ = 0;
         while(_loc6_ < _loc5_)
         {
            _loc7_ = _loc4_.getChildAt(_loc6_);
            _loc8_ = _loc7_.name;
            if(_loc8_.charAt(0) == "P")
            {
               param2.partPoint = new Point();
               param2.partPoint.x = Math.sqrt(_loc7_.x * _loc7_.x + _loc7_.y * _loc7_.y);
               param2.partPoint.y = Math.atan2(_loc7_.x,-_loc7_.y);
            }
            _loc6_++;
         }
      }
      
      public function mc2Data(param1:MovieClip, param2:DisplayObjectContainer, param3:F4DataOrg, param4:Boolean, param5:int) : F4PartMC
      {
         var _loc6_:MovieClip = param1.getChildByName("SHOW") as MovieClip;
         _loc6_.x = _loc6_.y = 0;
         _loc6_.scaleX = _loc6_.scaleY = 1;
         _loc6_.rotation = 0;
         var _loc7_:int = 1;
         if(param5 == 1)
         {
            _loc6_.scaleX = -1;
            _loc7_ = -1;
         }
         param2.addChild(_loc6_);
         var _loc8_:F4PartMC = new F4PartMC(_loc6_,param4);
         _loc8_.f4DataLink.f4dataorg = param3;
         if(param4)
         {
            param3.useAs = 2;
         }
         else
         {
            param3.useAs = 1;
         }
         return _loc8_;
      }
   }
}

