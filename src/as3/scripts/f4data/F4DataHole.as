package f4data
{
   import f4data.sdata.ConfigAndStory;
   import flash.geom.Point;
   
   public class F4DataHole
   {
      
      public var xy:Point;
      
      public var lr:int;
      
      public var type:Array;
      
      public var partf4:F4DataLinks;
      
      public var parentOrg:F4DataOrg;
      
      public function F4DataHole()
      {
         super();
         this.xy = new Point();
         this.type = [];
      }
      
      public function canWing() : Boolean
      {
         return true;
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
         if(!param1 || !param1.f4dataorg)
         {
            return 2;
         }
         return this.acceptableOrg(param1.f4dataorg);
      }
      
      public function acceptableOrg(param1:F4DataOrg) : int
      {
         if(!param1)
         {
            return 2;
         }
         if(param1.type == F4DataOrg.TYPE_WING)
         {
            if(this.isMountedWing(param1))
            {
               return 0;
            }
            if(this.getCurrentWings(param1) >= F4DataOrg.maxWingsForHull(this.getHullOrg()))
            {
               return 2;
            }
            return 0;
         }
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
      
      public function getCurrentWings(excludeOrg:F4DataOrg = null) : int
      {
         return F4DataHole.countMountedWings(excludeOrg);
      }
      
      public static function countMountedWings(excludeOrg:F4DataOrg = null) : int
      {
         var _cnt:int = 0;
         if(ConfigAndStory.ALLPART && ConfigAndStory.ALLPART.plMc && ConfigAndStory.ALLPART.plMc.length > 0)
         {
            for each(var _mc:* in ConfigAndStory.ALLPART.plMc)
            {
               if(_mc && _mc.f4DataLink && _mc.f4DataLink.f4dataorg)
               {
                  if(excludeOrg && _mc.f4DataLink.f4dataorg == excludeOrg)
                  {
                     continue;
                  }
                  if(_mc.f4DataLink.f4dataorg.type == F4DataOrg.TYPE_WING)
                  {
                     _cnt++;
                  }
               }
            }
            return _cnt;
         }
         if(ConfigAndStory.ALLPART && ConfigAndStory.ALLPART.plFly)
         {
            var _pl:F4DataLinks = ConfigAndStory.ALLPART.plFly[ConfigAndStory.PLAYER] as F4DataLinks;
            if(_pl)
            {
               return _pl.countWings();
            }
         }
         return 0;
      }

      private function isMountedWing(param1:F4DataOrg) : Boolean
      {
         if(ConfigAndStory.ALLPART && ConfigAndStory.ALLPART.plMc)
         {
            for each(var _mc:* in ConfigAndStory.ALLPART.plMc)
            {
               if(_mc && _mc.f4DataLink && _mc.f4DataLink.f4dataorg == param1)
               {
                  return true;
               }
            }
         }
         return false;
      }

      private function getHullOrg() : F4DataOrg
      {
         if(this.parentOrg && this.parentOrg.type == F4DataOrg.TYPE_BODY)
         {
            return this.parentOrg;
         }
         if(ConfigAndStory.ALLPART && ConfigAndStory.ALLPART.plMc)
         {
            for each(var _mc:* in ConfigAndStory.ALLPART.plMc)
            {
               if(_mc && _mc.f4DataLink && _mc.f4DataLink.f4dataorg && _mc.f4DataLink.parentPart == null && _mc.f4DataLink.f4dataorg.type == F4DataOrg.TYPE_BODY)
               {
                  return _mc.f4DataLink.f4dataorg;
               }
            }
         }
         if(ConfigAndStory.ALLPART && ConfigAndStory.ALLPART.plFly)
         {
            var _pl:F4DataLinks = ConfigAndStory.ALLPART.plFly[ConfigAndStory.PLAYER] as F4DataLinks;
            if(_pl && _pl.f4dataorg)
            {
               return _pl.f4dataorg;
            }
         }
         return this.parentOrg;
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

