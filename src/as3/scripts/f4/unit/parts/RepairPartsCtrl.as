package f4.unit.parts
{
   import f4data.F4DataHole;
   import f4data.F4DataLinks;
   import f4data.F4DataOrg;
   import f4data.sdata.ConfigAndStory;
   
   public class RepairPartsCtrl
   {
      
      public var flys:Array;
      
      public function RepairPartsCtrl()
      {
         super();
         this.flys = [];
      }
      
      public function shopNewFalse() : void
      {
         var _loc1_:F4DataOrg = null;
         for each(_loc1_ in ConfigAndStory.ALLPART.shopPart)
         {
            _loc1_.snew = false;
         }
      }
      
      public function removeParts() : void
      {
         var _loc1_:F4DataLinks = null;
         for each(_loc1_ in this.flys)
         {
            this.ckOneLink(_loc1_);
         }
         for each(_loc1_ in this.flys)
         {
            this.ckBodyAndDel(_loc1_);
         }
         this.allItemPartToShop();
         if(ConfigAndStory.STORY < 35)
         {
            this.allGiftToShop();
         }
      }
      
      public function deadFalse() : void
      {
         var _loc1_:F4DataOrg = null;
         for each(_loc1_ in ConfigAndStory.ALLPART.repairPart)
         {
            _loc1_.dead = false;
         }
      }
      
      public function ckOneLinkFalse(param1:F4DataLinks) : void
      {
         var _loc2_:F4DataHole = null;
         var _loc3_:int = int(param1.f4dataorg.hole.length);
         var _loc4_:int = 0;
         while(_loc4_ < _loc3_)
         {
            _loc2_ = param1.f4dataorg.hole[_loc4_];
            if(_loc2_.partf4)
            {
               this.ckOneLink(_loc2_.partf4);
               _loc2_.partf4.f4dataorg.dead = false;
            }
            _loc4_++;
         }
      }
      
      public function allGiftToShop() : void
      {
         var _loc1_:Array = ConfigAndStory.GIFT[ConfigAndStory.STORY];
         var _loc2_:int = int(_loc1_.length);
         var _loc3_:int = 0;
         while(_loc3_ < _loc2_)
         {
            ConfigAndStory.ALLPART.addPartToShop(_loc1_[_loc3_]);
            ConfigAndStory.SHOPNEW = true;
            _loc3_++;
         }
      }
      
      public function allItemPartToShop() : void
      {
         var _loc1_:F4DataOrg = null;
         for each(_loc1_ in ConfigAndStory.ALLPART.itemPart)
         {
            ConfigAndStory.ALLPART.addPartToShop(_loc1_.index);
            ConfigAndStory.SHOPNEW = true;
         }
      }
      
      public function ckBodyAndDel(param1:F4DataLinks) : void
      {
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         if(param1.f4dataorg.useAs == 2 && param1.f4dataorg.dead)
         {
            param1.f4dataorg.useAs = 0;
            _loc2_ = param1.f4dataorg.index;
            ConfigAndStory.ALLPART.addPartToShopFromPlayer(_loc2_);
            delete ConfigAndStory.ALLPART.plPart[_loc2_];
            if(param1.atHole)
            {
               param1.atHole = null;
            }
            if(param1.parentPart)
            {
               param1.parentPart = null;
            }
            _loc3_ = 0;
            while(_loc3_ < 20)
            {
               if(ConfigAndStory.COMPANIONS && _loc3_ < ConfigAndStory.COMPANIONS.length && ConfigAndStory.COMPANIONS[_loc3_] != null)
               {
                  var _cAssign:Object = ConfigAndStory.COMPANIONS[_loc3_];
                  if(_cAssign && _cAssign["craftIndex"] == param1.index)
                  {
                     ConfigAndStory.COMPANIONS[_loc3_] = null;
                     delete ConfigAndStory.ALLPART.plFly[param1.index];
                  }
               }
               _loc3_++;
            }
         }
      }
      
      public function ckOneLink(param1:F4DataLinks) : void
      {
         var _loc2_:F4DataHole = null;
         var _loc4_:int = 0;
         var _loc3_:int = int(param1.f4dataorg.hole.length);
         var _loc5_:int = 0;
         while(_loc5_ < _loc3_)
         {
            _loc2_ = param1.f4dataorg.hole[_loc5_];
            if(_loc2_.partf4)
            {
               this.ckOneLink(_loc2_.partf4);
               if(_loc2_.partf4.f4dataorg.dead)
               {
                  _loc2_.partf4.f4dataorg.useAs = 0;
                  _loc4_ = _loc2_.partf4.f4dataorg.index;
                  ConfigAndStory.ALLPART.addPartToShopFromPlayer(_loc4_);
                  delete ConfigAndStory.ALLPART.plPart[_loc4_];
                  if(_loc2_.partf4.atHole)
                  {
                     _loc2_.partf4.atHole = null;
                  }
                  if(_loc2_.partf4.parentPart)
                  {
                     _loc2_.partf4.parentPart = null;
                  }
                  _loc2_.partf4 = null;
               }
               else if(Boolean(_loc2_.partf4.parentPart) && _loc2_.partf4.parentPart.f4dataorg.dead)
               {
                  _loc2_.partf4.f4dataorg.useAs = 0;
                  if(_loc2_.partf4.atHole)
                  {
                     _loc2_.partf4.atHole = null;
                  }
                  _loc2_.partf4.parentPart = null;
                  _loc2_.partf4.f4dataorg.useAs = 0;
                  _loc2_.partf4 = null;
               }
            }
            _loc5_++;
         }
      }
   }
}

