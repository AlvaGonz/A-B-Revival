package f4data
{
   import f4data.sdata.ConfigAndStory;
   import f4ui.f4partmc.F4PartMC;
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import fullshit.tool.Attach;
   
   public class F4ShowData
   {
      
      public var link:F4DataLinks;
      
      public var sp:Sprite;
      
      public var spBody:Sprite;
      
      public var spWing:Sprite;
      
      public var spOth:Sprite;
      
      public var plMc:Array;
      
      public var mc2f4:MC2F4Data;
      
      public var tname:String;
      
      public var cls:String;
      
      public var value:int;
      
      public var cpx:int;
      
      public var speed:Number;
      
      public var turn:Number;
      
      public var hp:int;
      
      public var fire:Number;
      
      public var mass:Number;
      
      public var costMut:Number;
      
      public function F4ShowData(param1:F4DataLinks, param2:Number = 1)
      {
         super();
         this.costMut = param2;
         this.link = param1;
         this.sp = new Sprite();
         this.spWing = new Sprite();
         this.spBody = new Sprite();
         this.spOth = new Sprite();
         this.sp.addChild(this.spWing);
         this.sp.addChild(this.spBody);
         this.sp.addChild(this.spOth);
         this.sp.mouseEnabled = false;
         this.sp.mouseChildren = false;
         this.plMc = [];
         this.mc2f4 = new MC2F4Data();
      }
      
      public function makeFlyClone() : void
      {
         var _loc1_:F4DataLinks = this.link.clone(ConfigAndStory.ALLPART.plPart,ConfigAndStory.ALLPART.defFire,ConfigAndStory.ALLPART.defBullet);
         this.makeFly_A(_loc1_);
         this.hidePoints();
      }
      
      public function makeFly_A(param1:F4DataLinks) : void
      {
         var _loc2_:Sprite = null;
         var _loc3_:MovieClip = null;
         var _loc4_:F4PartMC = null;
         var _loc5_:F4DataHole = null;
         if(param1.f4dataorg.type == 0)
         {
            _loc2_ = this.spBody;
         }
         else if(param1.f4dataorg.type == 1)
         {
            _loc2_ = this.spWing;
         }
         else
         {
            _loc2_ = this.spOth;
         }
         _loc3_ = Attach.attach(param1.f4dataorg.mcName) as MovieClip;
         _loc4_ = this.mc2f4.mc2Data(_loc3_,_loc2_,param1.f4dataorg,param1.f4dataorg.useAs == 2,param1.f4dataorg.lr);
         _loc4_.f4DataLink = param1;
         _loc4_.f4DataLink.f4mc = _loc4_;
         _loc4_.allf4 = this.plMc;
         _loc4_.allf4.push(_loc4_);
         for each(_loc5_ in param1.f4dataorg.hole)
         {
            if(_loc5_.partf4)
            {
               this.makeFly_A(_loc5_.partf4);
            }
         }
         if(param1.f4dataorg.useAs == 2)
         {
            _loc4_.reLinkParent();
         }
      }
      
      public function cleanMC() : void
      {
         var _loc1_:F4PartMC = null;
         for each(_loc1_ in this.plMc)
         {
            if(_loc1_.f4DataLink.f4dataorg.useAs == 2)
            {
               _loc1_.removeAllNotBack(_loc1_);
               break;
            }
         }
         while(this.plMc.length > 0)
         {
            (this.plMc[0] as F4PartMC).removeAllNotBack(this.plMc[0] as F4PartMC,false);
         }
      }
      
      public function initData() : void
      {
         this.tname = this.link.name;
         this.mass = 0;
         this.speed = 0;
         this.mass = 0;
         this.turn = 0;
         this.hp = 0;
         this.value = 0;
         this.fire = 0;
         this.cls = "Z";
         this.initDataRec(this.link);
         this.speed /= this.mass;
         this.turn /= this.mass;
         this.turn *= 180 / Math.PI * 10;
         this.speed *= 100;
         this.value *= this.costMut;
         this.fire /= 300000;
         var _loc1_:Number = (this.hp / 1000 + this.turn + this.speed + this.fire) / 100;
         if(_loc1_ < 0.5)
         {
            this.cls = "F";
         }
         else if(_loc1_ < 1)
         {
            this.cls = "E";
         }
         else if(_loc1_ < 1.5)
         {
            this.cls = "D";
         }
         else if(_loc1_ < 2.3)
         {
            this.cls = "C";
         }
         else if(_loc1_ < 4)
         {
            this.cls = "B";
         }
         else if(_loc1_ < 5.8)
         {
            this.cls = "A";
         }
         else
         {
            this.cls = "S";
         }
      }
      
      public function initDataRec(param1:F4DataLinks) : void
      {
         var _loc2_:F4DataHole = null;
         ++this.cpx;
         this.mass += param1.f4dataorg.mass;
         this.speed += param1.f4dataorg.powFly;
         this.turn += param1.f4dataorg.turnFly;
         this.hp += param1.f4dataorg.maxhp;
         this.value += param1.f4dataorg.cost;
         if(param1.f4dataorg.firePoints.length > 0)
         {
            this.fire += param1.f4dataorg.firePoints.length * param1.f4dataorg.fire.firePowRate * param1.f4dataorg.bullet.bulletPow;
         }
         for each(_loc2_ in param1.f4dataorg.hole)
         {
            if(_loc2_.partf4)
            {
               this.initDataRec(_loc2_.partf4);
            }
         }
      }
      
      public function hidePoints() : void
      {
         var _loc1_:MovieClip = null;
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         var _loc4_:DisplayObject = null;
         var _loc5_:String = null;
         var _loc6_:F4PartMC = null;
         for each(_loc6_ in this.plMc)
         {
            _loc1_ = _loc6_.mc;
            _loc3_ = _loc1_.numChildren;
            _loc2_ = 0;
            while(_loc2_ < _loc3_)
            {
               _loc4_ = _loc1_.getChildAt(_loc2_);
               _loc5_ = _loc4_.name;
               if(_loc5_.charAt(0) == "L")
               {
                  _loc4_.visible = false;
               }
               else if(_loc5_.charAt(0) == "H")
               {
                  _loc4_.visible = false;
               }
               else if(_loc5_.charAt(0) == "F")
               {
                  _loc4_.visible = false;
               }
               else if(_loc5_.charAt(0) == "P")
               {
                  _loc4_.visible = false;
               }
               _loc2_++;
            }
         }
      }
   }
}

